using Doario.Data.Models.Mail;
using Microsoft.EntityFrameworkCore;

namespace Doario.Data.Repositories;

public class DocumentFeedbackRepository : IDocumentFeedbackRepository
{
    private readonly DoarioDataContext _db;

    public DocumentFeedbackRepository(DoarioDataContext db) => _db = db;

    public async Task AddAsync(DocumentFeedback feedback)
    {
        _db.DocumentFeedbacks.Add(feedback);
        await _db.SaveChangesAsync();
    }

    public async Task<List<DocumentFeedback>> GetAssignmentCorrectionsAsync(Guid tenantId, string ocrText)
    {
        var topText = ocrText.Length > 200 ? ocrText[..200] : ocrText;
        var keywords = topText
            .Split(new[] { ' ', '\n', '\r', ',', '.', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length >= 4)
            .Select(w => w.ToLowerInvariant())
            .Distinct()
            .Take(10)
            .ToList();

        var all = await _db.DocumentFeedbacks
            .Where(f => f.TenantId == tenantId && f.FeedbackTypeId == 2)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

        if (!keywords.Any()) return all.Take(5).ToList();

        return all
            .Where(f => !string.IsNullOrWhiteSpace(f.DocumentSnippet) &&
                        keywords.Any(k => f.DocumentSnippet.ToLowerInvariant().Contains(k)))
            .ToList();
    }

    public async Task<List<DocumentFeedback>> GetRecentForTenantAsync(Guid tenantId, int count = 10)
        => await _db.DocumentFeedbacks
            .Where(f => f.TenantId == tenantId)
            .OrderByDescending(f => f.CreatedAt)
            .Take(count)
            .ToListAsync();

    public async Task<List<DocumentFeedback>> GetRelevantForSenderAsync(Guid tenantId, string ocrText)
    {
        var topText = ocrText.Length > 200 ? ocrText[..200] : ocrText;
        var keywords = topText
            .Split(new[] { ' ', '\n', '\r', ',', '.', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length >= 4)
            .Where(w => !new[] { "dear", "from", "date", "with", "this", "that",
                                  "your", "have", "will", "been", "they", "their",
                                  "please", "thank", "regards", "sincerely" }
                        .Contains(w.ToLowerInvariant()))
            .Select(w => w.ToLowerInvariant())
            .Distinct()
            .Take(10)
            .ToList();

        if (!keywords.Any()) return new List<DocumentFeedback>();

        var all = await _db.DocumentFeedbacks
            .Where(f => f.TenantId == tenantId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

        return all
            .Where(f => !string.IsNullOrWhiteSpace(f.DocumentSnippet) &&
                        keywords.Any(k => f.DocumentSnippet.ToLowerInvariant().Contains(k)))
            .ToList();
    }

    /// <summary>
    /// Returns the most recent "not a check" corrections for this tenant.
    /// Used to teach AI what NOT to flag as a physical paper check.
    /// </summary>
    public async Task<List<DocumentFeedback>> GetNotCheckCorrectionsAsync(Guid tenantId, int count = 10)
        => await _db.DocumentFeedbacks
            .Where(f => f.TenantId == tenantId
                     && f.AiClassification == "check"
                     && f.CorrectedClassification == "not_check")
            .OrderByDescending(f => f.CreatedAt)
            .Take(count)
            .ToListAsync();
}