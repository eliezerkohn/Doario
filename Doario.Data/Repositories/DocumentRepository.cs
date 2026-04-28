using Doario.Data.Models.Mail;
using Microsoft.EntityFrameworkCore;

namespace Doario.Data.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly DoarioDataContext _db;

    public DocumentRepository(DoarioDataContext db) => _db = db;

    // Background services — no tenant filter
    public async Task<Document> GetByIdAsync(Guid documentId)
        => await _db.Documents
            .FirstOrDefaultAsync(d => d.DocumentId == documentId);

    // Controllers — always filter by tenant
    public async Task<Document> GetByIdAsync(Guid documentId, Guid tenantId)
        => await _db.Documents
            .FirstOrDefaultAsync(d => d.DocumentId == documentId
                                   && d.TenantId == tenantId);

    public async Task<Document> GetByIdWithTenantAsync(Guid documentId, Guid tenantId)
        => await _db.Documents
            .Include(d => d.Tenant)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId
                                   && d.TenantId == tenantId);

    public async Task<List<Document>> GetQueueAsync(Guid tenantId, int page, int pageSize)
        => await _db.Documents
            .Where(d => d.TenantId == tenantId)
            .Include(d => d.DocumentStatus)
            .OrderByDescending(d => d.UploadedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

    public async Task<int> GetMonthlyCountAsync(Guid tenantId, int year, int month)
        => await _db.Documents
            .CountAsync(d => d.TenantId == tenantId
                          && d.UploadedAt.Year == year
                          && d.UploadedAt.Month == month);

    public async Task UpdateStatusAsync(Guid documentId, int statusId)
    {
        var doc = await _db.Documents.FindAsync(documentId);
        if (doc is null) return;
        doc.DocumentStatusId = statusId;
        await _db.SaveChangesAsync();
    }

    public async Task UpdateOcrTextAsync(Guid documentId, string ocrText)
    {
        var doc = await _db.Documents.FindAsync(documentId);
        if (doc is null) return;
        doc.OcrText = ocrText;
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAiSummaryAsync(Guid documentId, string aiSummary)
    {
        var doc = await _db.Documents.FindAsync(documentId);
        if (doc is null) return;
        doc.AiSummary = aiSummary;
        await _db.SaveChangesAsync();
    }

    public async Task<Document> CreateAsync(Document document)
    {
        _db.Documents.Add(document);
        await _db.SaveChangesAsync();
        return document;
    }

    public async Task SaveAsync()
        => await _db.SaveChangesAsync();
}