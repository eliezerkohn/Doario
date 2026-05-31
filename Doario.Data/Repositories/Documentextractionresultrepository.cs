using Doario.Data.Models.Mail;
using Microsoft.EntityFrameworkCore;

namespace Doario.Data.Repositories;

public class DocumentExtractionResultRepository : IDocumentExtractionResultRepository
{
    private readonly DoarioDataContext _db;

    public DocumentExtractionResultRepository(DoarioDataContext db) => _db = db;

    public async Task SaveResultsAsync(
        Guid documentId,
        Guid tenantId,
        List<DocumentExtractionResult> results)
    {
        // Replace existing results for this document
        var existing = await _db.DocumentExtractionResults
            .Where(r => r.DocumentId == documentId)
            .ToListAsync();

        if (existing.Any())
            _db.DocumentExtractionResults.RemoveRange(existing);

        if (results.Any())
            await _db.DocumentExtractionResults.AddRangeAsync(results);

        await _db.SaveChangesAsync();
    }

    public async Task<List<DocumentExtractionResult>> GetByDocumentAsync(
        Guid documentId,
        Guid tenantId)
        => await _db.DocumentExtractionResults
            .Where(r => r.DocumentId == documentId && r.TenantId == tenantId)
            .OrderBy(r => r.ExtractedAt)
            .ToListAsync();

    public async Task UpdateConfirmationAsync(
        Guid documentExtractionResultId,
        bool isConfirmed,
        string correctedValue)
        => await _db.DocumentExtractionResults
            .Where(r => r.DocumentExtractionResultId == documentExtractionResultId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.IsConfirmed, isConfirmed)
                .SetProperty(r => r.CorrectedValue, correctedValue));
}