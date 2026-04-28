using Doario.Data.Models.Mail;

namespace Doario.Data.Repositories;

public interface IDocumentFeedbackRepository
{
    Task AddAsync(DocumentFeedback feedback);

    /// <summary>
    /// Returns the last N corrections for this tenant globally.
    /// Used as a general learning baseline.
    /// </summary>
    Task<List<DocumentFeedback>> GetRecentForTenantAsync(Guid tenantId, int count = 10);

    /// <summary>
    /// Returns ALL corrections for this tenant where the document snippet
    /// contains any of the provided keywords (e.g. sender name words).
    /// Used to prioritise corrections relevant to the current document's sender.
    /// </summary>
    Task<List<DocumentFeedback>> GetRelevantForSenderAsync(Guid tenantId, string ocrText);
}