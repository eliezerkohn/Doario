using Microsoft.Graph;
using Microsoft.Graph.Models;
using Doario.Data.Models.Mail;
using Doario.Data.Models.SaaS;
using Doario.Data.Repositories;

namespace Doario.Web.Services;

public class EmailDeliveryService
{
    private readonly IDocumentRepository _documents;
    private readonly IAssignmentRepository _assignments;
    private readonly IDeliveryRepository _deliveries;
    private readonly ITenantRepository _tenants;
    private readonly GraphServiceClient _graph;
    private readonly SharePointService _sharePoint;
    private readonly ILogger<EmailDeliveryService> _logger;
    private readonly IConfiguration _config;

    public EmailDeliveryService(
        IDocumentRepository documents,
        IAssignmentRepository assignments,
        IDeliveryRepository deliveries,
        ITenantRepository tenants,
        GraphServiceClient graph,
        SharePointService sharePoint,
        ILogger<EmailDeliveryService> logger,
        IConfiguration config)
    {
        _documents = documents;
        _assignments = assignments;
        _deliveries = deliveries;
        _tenants = tenants;
        _graph = graph;
        _sharePoint = sharePoint;
        _logger = logger;
        _config = config;
    }

    // ── Normal delivery ───────────────────────────────────────────────────────

    public async Task<(bool Success, string Error)> SendAsync(
        Guid documentId,
        Guid assignmentId,
        Guid tenantId)
    {
        var document = await _documents.GetByIdWithTenantAsync(documentId, tenantId);
        if (document is null) return (false, "Document not found.");

        var assignment = await _assignments.GetByIdAsync(assignmentId, tenantId);
        if (assignment is null) return (false, "Assignment not found.");

        var tenant = document.Tenant;
        if (string.IsNullOrEmpty(tenant.MailboxAddress))
            return (false, "Tenant has no MailboxAddress configured.");

        var delivery = new DocumentDelivery
        {
            DocumentDeliveryId = Guid.NewGuid(),
            TenantId = tenantId,
            DocumentId = documentId,
            DocumentAssignmentId = assignmentId,
            SystemStatusId = 7, // Pending
            SentToEmail = assignment.AssignedToEmail,
            CreatedAt = DateTime.UtcNow
        };

        await _deliveries.AddAsync(delivery);

        try
        {
            var baseUrl = _config["Doario:BaseUrl"] ?? "https://doario.com";

            List<Attachment> attachments = new();
            try
            {
                var (bytes, contentType) = await _sharePoint.DownloadFileAsync(
                    tenantId, document.SharePointUrl);

                attachments.Add(new FileAttachment
                {
                    Name = document.OriginalFileName,
                    ContentType = contentType,
                    ContentBytes = bytes
                });
            }
            catch (Exception attachEx)
            {
                _logger.LogWarning(attachEx,
                    "Could not attach file for Document {DocumentId}.", documentId);
            }

            await SendGraphEmailAsync(
                tenant.MailboxAddress,
                assignment.AssignedToEmail,
                $"{assignment.AssignedToStaff.FirstName} {assignment.AssignedToStaff.LastName}".Trim(),
                BuildSubject(document),
                BuildDeliveryBody(document, assignment, baseUrl),
                BuildAdaptiveCard(document, assignment, baseUrl),
                attachments);

            delivery.SystemStatusId = 8; // Sent
            delivery.SentAt = DateTime.UtcNow;
            await _deliveries.SaveAsync();

            _logger.LogInformation(
                "Delivered: Document {DocumentId} -> {Email}",
                documentId, assignment.AssignedToEmail);

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            delivery.SystemStatusId = 5; // Failed
            delivery.ErrorMessage = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            await _deliveries.SaveAsync();

            _logger.LogError(ex,
                "Delivery failed: Document {DocumentId} -> {Email}",
                documentId, assignment.AssignedToEmail);

            return (false, ex.Message);
        }
    }

    // ── Retry delivery ────────────────────────────────────────────────────────

    public async Task RetryDeliveryAsync(DocumentDelivery delivery)
    {
        if (delivery.Document == null)
            throw new InvalidOperationException(
                $"Document not loaded on delivery {delivery.DocumentDeliveryId}");

        var document = delivery.Document;

        var tenant = await _tenants.GetByIdAsync(delivery.TenantId);
        if (tenant == null)
            throw new InvalidOperationException($"Tenant {delivery.TenantId} not found.");

        if (string.IsNullOrEmpty(tenant.MailboxAddress))
            throw new InvalidOperationException($"Tenant {delivery.TenantId} has no MailboxAddress.");

        var baseUrl = _config["Doario:BaseUrl"] ?? "https://doario.com";
        var assignment = delivery.DocumentAssignment;

        List<Attachment> attachments = new();
        try
        {
            var (bytes, contentType) = await _sharePoint.DownloadFileAsync(
                delivery.TenantId, document.SharePointUrl);

            attachments.Add(new FileAttachment
            {
                Name = document.OriginalFileName,
                ContentType = contentType,
                ContentBytes = bytes
            });
        }
        catch (Exception attachEx)
        {
            _logger.LogWarning(attachEx,
                "Retry: could not attach file for Document {DocumentId}.", document.DocumentId);
        }

        await SendGraphEmailAsync(
            tenant.MailboxAddress,
            delivery.SentToEmail,
            delivery.SentToEmail,
            BuildSubject(document),
            BuildDeliveryBody(document, assignment, baseUrl),
            BuildAdaptiveCard(document, assignment, baseUrl),
            attachments);

        _logger.LogInformation(
            "Retry succeeded: Document {DocumentId} -> {Email}",
            document.DocumentId, delivery.SentToEmail);
    }

    // ── Reassign notification ─────────────────────────────────────────────────

    public async Task<(bool Success, string Error)> SendReassignNotificationAsync(
        Document document,
        string previousEmail,
        ImportedStaff newStaff)
    {
        var tenant = document.Tenant;
        if (string.IsNullOrEmpty(tenant?.MailboxAddress))
            return (false, "Tenant has no MailboxAddress configured.");

        try
        {
            var message = new Message
            {
                Subject = $"Document Reassigned: {Enc(document.OriginalFileName)}",
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = BuildReassignBody(document,
                                    $"{newStaff.FirstName} {newStaff.LastName}".Trim())
                },
                ToRecipients = new List<Recipient>
                {
                    new Recipient
                    {
                        EmailAddress = new EmailAddress { Address = previousEmail }
                    }
                }
            };

            await _graph.Users[tenant.MailboxAddress]
                .SendMail
                .PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
                {
                    Message = message,
                    SaveToSentItems = false
                });

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Reassign notification failed: Document {DocumentId} -> {Email}",
                document.DocumentId, previousEmail);

            return (false, ex.Message);
        }
    }

    // ── Promo email ───────────────────────────────────────────────────────────

    public async Task<(bool Success, string Error)> SendPromoEmailAsync(
        string toEmail,
        string toName,
        string tenantName,
        string promoCode,
        string promoDescription,
        decimal? discountPercent,
        decimal? flatDiscountPerDoc,
        int? freeDocCount)
    {
        var fromAddress = _config["SystemEmail:FromAddress"]
            ?? throw new InvalidOperationException("SystemEmail:FromAddress not configured.");
        var fromName = _config["SystemEmail:FromName"] ?? "Doario";

        try
        {
            var message = new Message
            {
                Subject = $"A special offer for {Enc(tenantName)} from Doario",
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = BuildPromoEmailBody(
                        toName, tenantName, promoCode, promoDescription,
                        discountPercent, flatDiscountPerDoc, freeDocCount)
                },
                ToRecipients = new List<Recipient>
                {
                    new Recipient
                    {
                        EmailAddress = new EmailAddress
                        {
                            Address = toEmail,
                            Name = toName
                        }
                    }
                },
                From = new Recipient
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = fromAddress,
                        Name = fromName
                    }
                }
            };

            await _graph.Users[fromAddress]
                .SendMail
                .PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
                {
                    Message = message,
                    SaveToSentItems = false
                });

            _logger.LogInformation(
                "Promo email sent to {Email} with code {Code}", toEmail, promoCode);

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send promo email to {Email}", toEmail);
            return (false, ex.Message);
        }
    }

    // ── Shared Graph send ─────────────────────────────────────────────────────

    private async Task SendGraphEmailAsync(
        string fromMailbox,
        string toAddress,
        string toDisplayName,
        string subject,
        string htmlBody,
        string adaptiveCardJson,
        List<Attachment> attachments)
    {
        // Embed Adaptive Card as a hidden script tag in the HTML body.
        // Outlook detects the application/adaptivecard+json script and renders
        // native action buttons. Non-Outlook clients see only the HTML body.
        var fullBody = $@"{htmlBody}
<script type=""application/adaptivecard+json"">
{adaptiveCardJson}
</script>";

        var message = new Message
        {
            Subject = subject,
            Body = new ItemBody
            {
                ContentType = BodyType.Html,
                Content = fullBody
            },
            ToRecipients = new List<Recipient>
            {
                new Recipient
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = toAddress,
                        Name = toDisplayName
                    }
                }
            },
            Attachments = attachments.Any() ? attachments : null,
            ReplyTo = new List<Recipient>
            {
                new Recipient
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = fromMailbox,
                        Name = "Mail Room (Do Not Reply)"
                    }
                }
            }
        };

        await _graph.Users[fromMailbox]
            .SendMail
            .PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
            {
                Message = message,
                SaveToSentItems = false
            });
    }

    // ── Adaptive Card builder ─────────────────────────────────────────────────

    /// <summary>
    /// Builds an Outlook Adaptive Card JSON with native action buttons.
    /// Outlook renders these as interactive buttons directly in the email.
    /// Non-Outlook clients fall back to the HTML body links.
    /// Works within the same M365 tenant without any registration.
    /// </summary>
    private static string BuildAdaptiveCard(
        Document document,
        DocumentAssignment assignment,
        string baseUrl)
    {
        var token = assignment.StaffAccessToken;
        var docId = document.DocumentId;

        var actionUrl = $"{baseUrl}/api/staff-action/action/{docId}/{token}";
        var verifyUrl = $"{baseUrl}/api/staff-action/verify/{docId}/{token}";
        var noteUrl = $"{baseUrl}/api/staff-action/note/{docId}/{token}";
        var forwardUrl = $"{baseUrl}/api/staff-action/forward/{docId}/{token}";
        var viewUrl = document.SharePointUrl;

        var senderName = !string.IsNullOrWhiteSpace(document.Sender?.DisplayName)
            ? document.Sender.DisplayName
            : "Unknown Sender";

        var summaryPlain = string.IsNullOrWhiteSpace(document.AiSummary)
            ? "AI summary not yet available."
            : System.Text.RegularExpressions.Regex
                .Replace(document.AiSummary, "<.*?>", string.Empty).Trim();

        if (summaryPlain.Length > 200)
            summaryPlain = summaryPlain[..200] + "…";

        // Escape for JSON
        var safeFileName = EscJson(document.OriginalFileName);
        var safeSender = EscJson(senderName);
        var safeSummary = EscJson(summaryPlain);
        var safeDate = document.UploadedAt.ToString("dddd, MMMM d, yyyy h:mm tt") + " UTC";

        return $$"""
{
  "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
  "type": "AdaptiveCard",
  "version": "1.4",
  "hideOriginalBody": false,
  "body": [
    {
      "type": "Container",
      "style": "emphasis",
      "items": [
        {
          "type": "TextBlock",
          "text": "📬 New Mail Item",
          "weight": "Bolder",
          "size": "Medium",
          "color": "Accent"
        }
      ]
    },
    {
      "type": "FactSet",
      "facts": [
        { "title": "From",     "value": "{{safeSender}}" },
        { "title": "File",     "value": "{{safeFileName}}" },
        { "title": "Received", "value": "{{safeDate}}" }
      ]
    },
    {
      "type": "TextBlock",
      "text": "Summary",
      "weight": "Bolder",
      "size": "Small",
      "spacing": "Medium"
    },
    {
      "type": "TextBlock",
      "text": "{{safeSummary}}",
      "wrap": true,
      "size": "Small",
      "color": "Default"
    }
  ],
  "actions": [
    {
      "type": "Action.OpenUrl",
      "title": "✅ Mark as Actioned",
      "url": "{{actionUrl}}",
      "style": "positive"
    },
    {
      "type": "Action.OpenUrl",
      "title": "🔍 Verify Extraction",
      "url": "{{verifyUrl}}"
    },
    {
      "type": "Action.OpenUrl",
      "title": "💬 Add Note",
      "url": "{{noteUrl}}"
    },
    {
      "type": "Action.OpenUrl",
      "title": "↗ Forward",
      "url": "{{forwardUrl}}"
    },
    {
      "type": "Action.OpenUrl",
      "title": "📄 View in SharePoint",
      "url": "{{viewUrl}}"
    }
  ]
}
""";
    }

    // ── Subject ───────────────────────────────────────────────────────────────

    private static string BuildSubject(Document document)
    {
        if (!string.IsNullOrWhiteSpace(document.AiSummary))
        {
            var plain = System.Text.RegularExpressions.Regex
                .Replace(document.AiSummary, "<.*?>", string.Empty).Trim();
            var dot = plain.IndexOf('.');
            var snippet = dot > 0 && dot < 80 ? plain[..(dot + 1)]
                        : plain.Length > 80 ? plain[..80] + "…"
                        : plain;
            return $"New Mail: {snippet}";
        }
        return $"New Mail: {document.Sender?.DisplayName ?? document.OriginalFileName} — {document.OriginalFileName}";
    }

    // ── Delivery email body ───────────────────────────────────────────────────

    private static string BuildDeliveryBody(
        Document document,
        DocumentAssignment assignment,
        string baseUrl)
    {
        var token = assignment.StaffAccessToken;
        var docId = document.DocumentId;
        var actionUrl = $"{baseUrl}/api/staff-action/action/{docId}/{token}";
        var forwardUrl = $"{baseUrl}/api/staff-action/forward/{docId}/{token}";
        var noteUrl = $"{baseUrl}/api/staff-action/note/{docId}/{token}";
        var verifyUrl = $"{baseUrl}/api/staff-action/verify/{docId}/{token}";
        var viewUrl = document.SharePointUrl;

        var senderName = !string.IsNullOrWhiteSpace(document.Sender?.DisplayName)
            ? document.Sender.DisplayName
            : "Unknown Sender";

        var senderEmail = document.Sender?.Email ?? string.Empty;

        var senderLine = string.IsNullOrWhiteSpace(senderEmail)
            ? Enc(senderName)
            : $"{Enc(senderName)} &lt;<a href=\"mailto:{Enc(senderEmail)}\">" +
              $"{Enc(senderEmail)}</a>&gt;";

        var summaryHtml = string.IsNullOrWhiteSpace(document.AiSummary)
            ? "<em style=\"color:#6b7280;\">AI summary not yet available.</em>"
            : document.AiSummary;

        var noteSection = !string.IsNullOrWhiteSpace(assignment.Note)
            ? $"<div style=\"margin:16px 0;padding:12px 16px;background:#fff8e1;" +
              $"border-left:4px solid #f59e0b;border-radius:4px;\">" +
              $"<strong>Note from your admin:</strong><br>{Enc(assignment.Note)}</div>"
            : string.Empty;

        return $@"<!DOCTYPE html>
<html><head><meta charset=""utf-8""></head>
<body style=""font-family:Segoe UI,Arial,sans-serif;color:#1f2937;max-width:640px;margin:0 auto;padding:24px;"">
  <div style=""background:#1d4ed8;padding:20px 24px;border-radius:8px 8px 0 0;"">
    <h1 style=""color:#fff;margin:0;font-size:18px;font-weight:600;"">📬 New Mail Item</h1>
  </div>
  <div style=""background:#f9fafb;padding:24px;border:1px solid #e5e7eb;border-top:none;border-radius:0 0 8px 8px;"">
    <table style=""width:100%;border-collapse:collapse;margin-bottom:20px;"">
      <tr>
        <td style=""color:#6b7280;font-size:13px;padding:4px 12px 4px 0;white-space:nowrap;vertical-align:top;width:80px;"">From</td>
        <td style=""font-size:14px;padding:4px 0;"">{senderLine}</td>
      </tr>
      <tr>
        <td style=""color:#6b7280;font-size:13px;padding:4px 12px 4px 0;white-space:nowrap;vertical-align:top;"">File</td>
        <td style=""font-size:14px;padding:4px 0;"">
          <a href=""{viewUrl}"" style=""color:#1d4ed8;text-decoration:none;"">{Enc(document.OriginalFileName)}</a>
        </td>
      </tr>
      <tr>
        <td style=""color:#6b7280;font-size:13px;padding:4px 12px 4px 0;white-space:nowrap;vertical-align:top;"">Received</td>
        <td style=""font-size:14px;padding:4px 0;"">{document.UploadedAt:dddd, MMMM d, yyyy h:mm tt} UTC</td>
      </tr>
    </table>
    <hr style=""border:none;border-top:1px solid #e5e7eb;margin:0 0 20px;"">
    <h2 style=""font-size:15px;font-weight:600;margin:0 0 10px;"">Summary</h2>
    <div style=""font-size:14px;line-height:1.6;margin-bottom:8px;"">{summaryHtml}</div>
    {noteSection}
    <hr style=""border:none;border-top:1px solid #e5e7eb;margin:20px 0;"">
    <h2 style=""font-size:15px;font-weight:600;margin:0 0 12px;"">Actions</h2>
    <table cellspacing=""0"" cellpadding=""0"">
      <tr>
        <td style=""padding-right:8px;"">
          <a href=""{actionUrl}"" style=""display:inline-block;background:#16a34a;color:#fff;padding:10px 20px;border-radius:6px;text-decoration:none;font-size:14px;font-weight:600;"">✅ Mark as Actioned</a>
        </td>
        <td style=""padding-right:8px;"">
          <a href=""{verifyUrl}"" style=""display:inline-block;background:#0369a1;color:#fff;padding:10px 20px;border-radius:6px;text-decoration:none;font-size:14px;font-weight:600;"">🔍 Verify Extraction</a>
        </td>
        <td style=""padding-right:8px;"">
          <a href=""{forwardUrl}"" style=""display:inline-block;background:#2563eb;color:#fff;padding:10px 20px;border-radius:6px;text-decoration:none;font-size:14px;font-weight:600;"">↗ Forward</a>
        </td>
        <td>
          <a href=""{noteUrl}"" style=""display:inline-block;background:#7c3aed;color:#fff;padding:10px 20px;border-radius:6px;text-decoration:none;font-size:14px;font-weight:600;"">💬 Add Note</a>
        </td>
      </tr>
    </table>
    <p style=""margin-top:20px;font-size:13px;"">
      <a href=""{viewUrl}"" style=""color:#1d4ed8;"">View original document in SharePoint →</a>
    </p>
    <hr style=""border:none;border-top:1px solid #e5e7eb;margin:24px 0 16px;"">
    <p style=""font-size:11px;color:#9ca3af;margin:0;"">
      This message was delivered by your organisation's mail digitisation system.
      Action links expire in 30 days. Do not forward this email — action links are personal to you.
    </p>
  </div>
</body></html>";
    }

    // ── Promo email body ──────────────────────────────────────────────────────

    private static string BuildPromoEmailBody(
        string toName,
        string tenantName,
        string promoCode,
        string promoDescription,
        decimal? discountPercent,
        decimal? flatDiscountPerDoc,
        int? freeDocCount)
    {
        var benefitLines = new List<string>();
        if (discountPercent > 0)
            benefitLines.Add($"<li>{discountPercent}% discount on extra document charges</li>");
        if (flatDiscountPerDoc > 0)
            benefitLines.Add($"<li>${flatDiscountPerDoc:F4} off per extra document</li>");
        if (freeDocCount > 0)
            benefitLines.Add($"<li>{freeDocCount} bonus free documents per month</li>");

        var benefitsHtml = benefitLines.Any()
            ? $"<ul style=\"margin:12px 0;padding-left:20px;font-size:14px;color:#374151;\">{string.Join("", benefitLines)}</ul>"
            : string.Empty;

        var greeting = string.IsNullOrWhiteSpace(toName) ? "Hello" : $"Hello {Enc(toName)}";

        return $@"<!DOCTYPE html>
<html><head><meta charset=""utf-8""></head>
<body style=""font-family:Segoe UI,Arial,sans-serif;color:#1f2937;max-width:600px;margin:0 auto;padding:24px;"">
  <div style=""background:#0f2d4a;padding:24px;border-radius:10px 10px 0 0;text-align:center;"">
    <div style=""font-size:22px;font-weight:800;color:#fff;letter-spacing:-0.5px;"">
      Do<span style=""color:#99e0d9;"">a</span>rio
    </div>
    <div style=""font-size:13px;color:rgba(255,255,255,0.6);margin-top:4px;"">Mail Digitisation</div>
  </div>
  <div style=""background:#f9fafb;padding:32px;border:1px solid #e5e7eb;border-top:none;border-radius:0 0 10px 10px;"">
    <p style=""font-size:15px;margin:0 0 16px;"">{greeting},</p>
    <p style=""font-size:14px;color:#374151;margin:0 0 24px;line-height:1.6;"">
      We have a special offer for <strong>{Enc(tenantName)}</strong>.
      As a valued Doario customer, we are applying an exclusive promo to your account.
    </p>
    {(!string.IsNullOrWhiteSpace(promoDescription) ? $"<p style=\"font-size:14px;color:#374151;margin:0 0 20px;\">{Enc(promoDescription)}</p>" : "")}
    {(benefitsHtml.Length > 0 ? $"<div style=\"margin:0 0 24px;\"><strong style=\"font-size:13px;color:#0f2d4a;\">What you get:</strong>{benefitsHtml}</div>" : "")}
    <div style=""text-align:center;margin:28px 0;"">
      <div style=""font-size:12px;color:#6b7280;margin-bottom:8px;font-weight:600;text-transform:uppercase;letter-spacing:1px;"">Your Promo Code</div>
      <div style=""display:inline-block;background:#0f2d4a;color:#99e0d9;font-size:24px;font-weight:800;
                  padding:16px 32px;border-radius:10px;letter-spacing:4px;font-family:monospace;"">
        {Enc(promoCode)}
      </div>
    </div>
    <p style=""font-size:13px;color:#6b7280;text-align:center;margin:0 0 24px;"">
      Enter this code in your Doario portal under Settings → Billing to apply it to your account.
    </p>
    <hr style=""border:none;border-top:1px solid #e5e7eb;margin:24px 0 16px;"">
    <p style=""font-size:11px;color:#9ca3af;margin:0;text-align:center;"">
      This email was sent by Doario. If you have questions, reply to this email.
    </p>
  </div>
</body></html>";
    }

    // ── Reassign notification body ────────────────────────────────────────────

    private static string BuildReassignBody(Document document, string newStaffName)
        => $@"<!DOCTYPE html>
<html><head><meta charset=""utf-8""></head>
<body style=""font-family:Segoe UI,Arial,sans-serif;color:#1f2937;max-width:640px;margin:0 auto;padding:24px;"">
  <div style=""background:#dc2626;padding:20px 24px;border-radius:8px 8px 0 0;"">
    <h1 style=""color:#fff;margin:0;font-size:18px;font-weight:600;"">↩ Document Reassigned</h1>
  </div>
  <div style=""background:#f9fafb;padding:24px;border:1px solid #e5e7eb;border-top:none;border-radius:0 0 8px 8px;"">
    <p style=""font-size:14px;margin:0 0 16px;"">
      A document previously assigned to you has been reassigned to
      <strong>{Enc(newStaffName)}</strong>. No further action is needed from you.
    </p>
    <table style=""width:100%;border-collapse:collapse;margin-bottom:20px;"">
      <tr>
        <td style=""color:#6b7280;font-size:13px;padding:4px 12px 4px 0;white-space:nowrap;vertical-align:top;width:80px;"">File</td>
        <td style=""font-size:14px;padding:4px 0;"">{Enc(document.OriginalFileName)}</td>
      </tr>
      <tr>
        <td style=""color:#6b7280;font-size:13px;padding:4px 12px 4px 0;white-space:nowrap;vertical-align:top;"">From</td>
        <td style=""font-size:14px;padding:4px 0;"">{Enc(document.Sender?.DisplayName ?? string.Empty)}</td>
      </tr>
      <tr>
        <td style=""color:#6b7280;font-size:13px;padding:4px 12px 4px 0;white-space:nowrap;vertical-align:top;"">Received</td>
        <td style=""font-size:14px;padding:4px 0;"">{document.UploadedAt:dddd, MMMM d, yyyy h:mm tt} UTC</td>
      </tr>
    </table>
    <div style=""padding:12px 16px;background:#fef2f2;border-left:4px solid #dc2626;border-radius:4px;font-size:13px;color:#991b1b;"">
      Any action links in your previous email for this document are no longer valid.
    </div>
    <hr style=""border:none;border-top:1px solid #e5e7eb;margin:24px 0 16px;"">
    <p style=""font-size:11px;color:#9ca3af;margin:0;"">
      This message was sent by your organisation's mail digitisation system.
    </p>
  </div>
</body></html>";

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string Enc(string value) =>
        string.IsNullOrEmpty(value) ? string.Empty : System.Net.WebUtility.HtmlEncode(value);

    private static string EscJson(string value) =>
        string.IsNullOrEmpty(value) ? string.Empty :
        value.Replace("\\", "\\\\").Replace("\"", "\\\"")
             .Replace("\r", "\\r").Replace("\n", "\\n");
}