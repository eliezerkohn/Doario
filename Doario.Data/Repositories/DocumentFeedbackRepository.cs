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

    /// <summary>
    /// Returns last 10 corrections for this tenant globally.
    /// General baseline — not sender-specific.
    /// </summary>
    public async Task<List<DocumentFeedback>> GetRecentForTenantAsync(Guid tenantId, int count = 10)
        => await _db.DocumentFeedbacks
            .Where(f => f.TenantId == tenantId)
            .OrderByDescending(f => f.CreatedAt)
            .Take(count)
            .ToListAsync();

    /// <summary>
    /// Returns corrections where the DocumentSnippet contains words
    /// that also appear in the current document's OCR text.
    ///
    /// How it works:
    /// 1. Extract significant words from the first 200 chars of the current OCR text
    ///    (words 4+ characters long, skip common filler words)
    /// 2. Load all corrections for this tenant
    /// 3. Return ones where any keyword appears in their stored snippet
    ///
    /// This means: if MedLine Supply Co. was corrected before, and the current
    /// document contains "MedLine" anywhere, that correction will be included
    /// regardless of how many other corrections exist.
    /// </summary>
    public async Task<List<DocumentFeedback>> GetRelevantForSenderAsync(Guid tenantId, string ocrText)
    {
        // Extract keywords from the first 200 characters of the current document
        // (sender name and company name usually appear near the top)
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
            .Take(10) // use top 10 keywords only
            .ToList();

        if (!keywords.Any()) return new List<DocumentFeedback>();

        // Load all corrections for this tenant (no limit — we want all sender-specific ones)
        var all = await _db.DocumentFeedbacks
            .Where(f => f.TenantId == tenantId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

        // Return corrections where any keyword matches the stored snippet
        return all
            .Where(f => !string.IsNullOrWhiteSpace(f.DocumentSnippet) &&
                        keywords.Any(k => f.DocumentSnippet.ToLowerInvariant().Contains(k)))
            .ToList();
    }
}