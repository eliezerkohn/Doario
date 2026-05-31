using Doario.Data.Models.Mail;

namespace Doario.Data.Repositories;

public interface IDocumentExtractionResultRepository
{
    /// <summary>
    /// Saves a batch of extraction results for a document.
    /// Replaces any existing results for the same document.
    /// </summary>
    Task SaveResultsAsync(Guid documentId, Guid tenantId, List<DocumentExtractionResult> results);

    /// <summary>
    /// Returns all extraction results for a document.
    /// </summary>
    Task<List<DocumentExtractionResult>> GetByDocumentAsync(Guid documentId, Guid tenantId);

    /// <summary>
    /// Updates a single field — staff confirmed or corrected the value.
    /// </summary>
    Task UpdateConfirmationAsync(
        Guid documentExtractionResultId,
        bool isConfirmed,
        string correctedValue);
}