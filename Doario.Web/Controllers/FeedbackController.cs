using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Doario.Data.Models.Mail;
using Doario.Data.Repositories;
using Doario.Web.Services;

namespace Doario.Web.Controllers;

[ApiController]
[Route("api/feedback")]
[Authorize(Roles = "DoarioAdmin")]
public class FeedbackController : ControllerBase
{
    private readonly IDocumentFeedbackRepository _feedback;
    private readonly ITenantWhitelistedSenderRepository _whitelist;
    private readonly IDocumentRepository _documents;
    private readonly TenantContext _tenant;

    public FeedbackController(
        IDocumentFeedbackRepository feedback,
        ITenantWhitelistedSenderRepository whitelist,
        IDocumentRepository documents,
        TenantContext tenant)
    {
        _feedback = feedback;
        _whitelist = whitelist;
        _documents = documents;
        _tenant = tenant;
    }

    /// <summary>
    /// POST /api/feedback/not-spam
    /// Called when admin clicks "Not Spam" on a spam-classified document.
    ///
    /// Spam is SENDER-based — the sender itself is the problem, not the message.
    /// So we:
    ///   1. Move document back to Unassigned
    ///   2. Record correction in DocumentFeedback for AI learning
    ///   3. Whitelist the sender permanently — future messages from this sender
    ///      skip AI classification entirely and always land in Unassigned
    /// </summary>
    [HttpPost("not-spam")]
    public async Task<IActionResult> MarkNotSpam([FromBody] FeedbackRequest request)
    {
        if (!_tenant.IsResolved)
            return Unauthorized();

        var doc = await _documents.GetByIdAsync(request.DocumentId, _tenant.TenantId);
        if (doc is null)
            return NotFound();

        // 1. Move back to Unassigned
        await _documents.UpdateStatusAsync(request.DocumentId, 1);

        // 2. Record correction for AI learning
        var snippet = string.IsNullOrWhiteSpace(doc.OcrText) ? string.Empty
            : doc.OcrText.Length > 500 ? doc.OcrText[..500] : doc.OcrText;

        await _feedback.AddAsync(new DocumentFeedback
        {
            DocumentFeedbackId = Guid.NewGuid(),
            TenantId = _tenant.TenantId,
            DocumentId = request.DocumentId,
            AiClassification = "spam",
            CorrectedClassification = "mail",
            DocumentSnippet = snippet,
            CreatedAt = DateTime.UtcNow
        });

        // 3. Whitelist sender permanently
        // Spam = sender-based problem, so block the sender for all future messages
        var senderIdentifier = !string.IsNullOrWhiteSpace(doc.SenderDisplayName)
            ? doc.SenderDisplayName.Trim()
            : !string.IsNullOrWhiteSpace(request.SenderIdentifier)
                ? request.SenderIdentifier.Trim()
                : null;

        var senderWhitelisted = false;
        if (!string.IsNullOrWhiteSpace(senderIdentifier))
        {
            var existing = await _whitelist.GetAllForTenantAsync(_tenant.TenantId);
            var alreadyExists = existing.Any(w =>
                w.SenderIdentifier.Equals(senderIdentifier, StringComparison.OrdinalIgnoreCase));

            if (!alreadyExists)
            {
                await _whitelist.AddAsync(new TenantWhitelistedSender
                {
                    TenantWhitelistedSenderId = Guid.NewGuid(),
                    TenantId = _tenant.TenantId,
                    SenderIdentifier = senderIdentifier,
                    Source = "AdminOverride",
                    CreatedAt = DateTime.UtcNow
                });
                senderWhitelisted = true;
            }
        }

        return Ok(new
        {
            message = "Document moved to Inbox. Sender whitelisted.",
            senderWhitelisted = senderWhitelisted,
            senderIdentifier = senderIdentifier ?? "unknown"
        });
    }

    /// <summary>
    /// POST /api/feedback/not-promotion
    /// Called when admin clicks "This is real mail" on a promotion-classified document.
    ///
    /// Promotion is CONTENT-based — the same sender may send real mail next time.
    /// So we:
    ///   1. Move document back to Unassigned
    ///   2. Record correction in DocumentFeedback for AI learning
    ///   NO sender whitelisting — the sender is not the problem, this specific
    ///   message was just misclassified. The AI learns from the content correction.
    /// </summary>
    [HttpPost("not-promotion")]
    public async Task<IActionResult> MarkNotPromotion([FromBody] FeedbackRequest request)
    {
        if (!_tenant.IsResolved)
            return Unauthorized();

        var doc = await _documents.GetByIdAsync(request.DocumentId, _tenant.TenantId);
        if (doc is null)
            return NotFound();

        // 1. Move back to Unassigned
        await _documents.UpdateStatusAsync(request.DocumentId, 1);

        // 2. Record correction for AI learning
        // No sender whitelisting — promotion is content-based, not sender-based
        var snippet = string.IsNullOrWhiteSpace(doc.OcrText) ? string.Empty
            : doc.OcrText.Length > 500 ? doc.OcrText[..500] : doc.OcrText;

        await _feedback.AddAsync(new DocumentFeedback
        {
            DocumentFeedbackId = Guid.NewGuid(),
            TenantId = _tenant.TenantId,
            DocumentId = request.DocumentId,
            AiClassification = "promotion",
            CorrectedClassification = "mail",
            DocumentSnippet = snippet,
            CreatedAt = DateTime.UtcNow
        });

        return Ok(new
        {
            message = "Document moved to Inbox. AI will learn from this correction."
        });
    }
}

public class FeedbackRequest
{
    public Guid DocumentId { get; set; }
    public string SenderIdentifier { get; set; } // optional — used if SenderDisplayName is empty
}