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
    private readonly GraphServiceClient _graph;
    private readonly SharePointService _sharePoint;
    private readonly ILogger<EmailDeliveryService> _logger;
    private readonly IConfiguration _config;

    public EmailDeliveryService(
        IDocumentRepository documents,
        IAssignmentRepository assignments,
        IDeliveryRepository deliveries,
        GraphServiceClient graph,
        SharePointService sharePoint,
        ILogger<EmailDeliveryService> logger,
        IConfiguration config)
    {
        _documents = documents;
        _assignments = assignments;
        _deliveries = deliveries;
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
            var staffName = $"{assignment.AssignedToStaff.FirstName} {assignment.AssignedToStaff.LastName}".Trim();

            // Download from SharePoint into memory — never written to disk
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
                // Non-fatal — email sends without attachment, staff uses SharePoint link
                _logger.LogWarning(attachEx,
                    "Could not attach file for Document {DocumentId}.", documentId);
            }

            var message = new Message
            {
                Subject = BuildSubject(document),
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = BuildDeliveryBody(document, assignment, baseUrl)
                },
                ToRecipients = new List<Recipient>
                {
                    new Recipient
                    {
                        EmailAddress = new EmailAddress
                        {
                            Address = assignment.AssignedToEmail,
                            Name    = staffName
                        }
                    }
                },
                Attachments = attachments.Any() ? attachments : null
            };

            await _graph.Users[tenant.MailboxAddress]
                .SendMail
                .PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
                {
                    Message = message,
                    SaveToSentItems = false
                });

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
        return $"New Mail: {document.SenderDisplayName} — {document.OriginalFileName}";
    }

    // ── Delivery email body ───────────────────────────────────────────────────

    private static string BuildDeliveryBody(
        Document document,
        DocumentAssignment assignment,
        string baseUrl)
    {
        var token = assignment.StaffAccessToken;
        var docId = document.DocumentId;
        var actionUrl = $"{baseUrl}/action/{docId}/{token}";
        var forwardUrl = $"{baseUrl}/forward/{docId}/{token}";
        var noteUrl = $"{baseUrl}/note/{docId}/{token}";
        var viewUrl = document.SharePointUrl;

        var senderName = !string.IsNullOrWhiteSpace(document.SenderDisplayName)
            ? document.SenderDisplayName
            : "Unknown Sender";

        var senderLine = string.IsNullOrWhiteSpace(document.SenderEmail)
            ? Enc(senderName)
            : $"{Enc(senderName)} &lt;<a href=\"mailto:{Enc(document.SenderEmail)}\">" +
              $"{Enc(document.SenderEmail)}</a>&gt;";

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
      The attached document is for your reference — drag it directly into any portal upload field.
      Action links expire in 30 days. Do not forward this email — action links are personal to you.
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
        <td style=""font-size:14px;padding:4px 0;"">{Enc(document.SenderDisplayName)}</td>
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

    private static string Enc(string value) =>
        string.IsNullOrEmpty(value) ? string.Empty : System.Net.WebUtility.HtmlEncode(value);
}