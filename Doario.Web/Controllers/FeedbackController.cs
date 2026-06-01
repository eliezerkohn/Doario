using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Doario.Data.Models.Mail;
using Doario.Data.Repositories;
using Doario.Web.Services;

namespace Doario.Web.Controllers;

[ApiController]
[Route("api/feedback")]
[AllowAnonymous]
public class FeedbackController : ControllerBase
{
    private readonly IDocumentFeedbackRepository _feedback;
    private readonly ITenantWhitelistedSenderRepository _whitelist;
    private readonly IDocumentRepository _documents;
    private readonly IDocumentCheckRepository _checks;
    private readonly TenantContext _tenant;

    public FeedbackController(
        IDocumentFeedbackRepository feedback,
        ITenantWhitelistedSenderRepository whitelist,
        IDocumentRepository documents,
        IDocumentCheckRepository checks,
        TenantContext tenant)
    {
        _feedback = feedback;
        _whitelist = whitelist;
        _documents = documents;
        _checks = checks;
        _tenant = tenant;
    }

    /// <summary>
    /// POST /api/feedback/not-spam
    /// </summary>
    [HttpPost("not-spam")]
    public async Task<IActionResult> MarkNotSpam([FromBody] FeedbackRequest request)
    {
        if (!_tenant.IsResolved) return Unauthorized();

        var doc = await _documents.GetByIdAsync(request.DocumentId, _tenant.TenantId);
        if (doc is null) return NotFound();

        await _documents.UpdateStatusAsync(request.DocumentId, 1);

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

        var senderIdentifier = !string.IsNullOrWhiteSpace(doc.Sender?.DisplayName)
            ? doc.Sender.DisplayName.Trim()
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
            senderWhitelisted,
            senderIdentifier = senderIdentifier ?? "unknown"
        });
    }

    /// <summary>
    /// POST /api/feedback/not-promotion
    /// </summary>
    [HttpPost("not-promotion")]
    public async Task<IActionResult> MarkNotPromotion([FromBody] FeedbackRequest request)
    {
        if (!_tenant.IsResolved) return Unauthorized();

        var doc = await _documents.GetByIdAsync(request.DocumentId, _tenant.TenantId);
        if (doc is null) return NotFound();

        await _documents.UpdateStatusAsync(request.DocumentId, 1);

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

        return Ok(new { message = "Document moved to Inbox. AI will learn from this correction." });
    }

    /// <summary>
    /// POST /api/feedback/not-check
    /// Called when admin clicks "Not a Check" on a document incorrectly flagged as a check.
    /// Removes the check record and saves feedback so the AI learns to be stricter.
    /// </summary>
    [HttpPost("not-check")]
    public async Task<IActionResult> MarkNotCheck([FromBody] FeedbackRequest request)
    {
        if (!_tenant.IsResolved) return Unauthorized();

        var doc = await _documents.GetByIdAsync(request.DocumentId, _tenant.TenantId);
        if (doc is null) return NotFound();

        // Remove the check record
        await _checks.DeleteByDocumentIdAsync(request.DocumentId);

        // Save feedback so AI learns — snippet helps AI recognise this document type
        var snippet = string.IsNullOrWhiteSpace(doc.OcrText) ? string.Empty
            : doc.OcrText.Length > 300 ? doc.OcrText[..300] : doc.OcrText;

        await _feedback.AddAsync(new DocumentFeedback
        {
            DocumentFeedbackId = Guid.NewGuid(),
            TenantId = _tenant.TenantId,
            DocumentId = request.DocumentId,
            AiClassification = "check",
            CorrectedClassification = "not_check",
            DocumentSnippet = snippet,
            CreatedAt = DateTime.UtcNow
        });

        return Ok(new { message = "Check removed. AI will learn from this correction." });
    }
}

public class FeedbackRequest
{
    public Guid DocumentId { get; set; }
    public string SenderIdentifier { get; set; }
}