using Doario.Data.Models.Mail;
using Microsoft.EntityFrameworkCore;

namespace Doario.Data.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly DoarioDataContext _db;

    public DocumentRepository(DoarioDataContext db) => _db = db;

    public async Task<Document> GetByIdAsync(Guid documentId)
        => await _db.Documents
            .FirstOrDefaultAsync(d => d.DocumentId == documentId);

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
            .Include(d => d.Sender)
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

    public async Task DeleteAsync(Guid documentId)
    {
        var doc = await _db.Documents.FindAsync(documentId);
        if (doc is null) return;
        _db.Documents.Remove(doc);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateSenderIdAsync(Guid documentId, Guid senderId)
    {
        var doc = await _db.Documents.FindAsync(documentId);
        if (doc is null) return;
        doc.SenderId = senderId;
        await _db.SaveChangesAsync();
    }

    public async Task<List<Document>> GetBySenderAsync(Guid tenantId, string query)
    {
        var q = query.Trim();
        return await _db.Documents
            .Include(d => d.DocumentStatus)
            .Include(d => d.Sender)
            .Where(d => d.TenantId == tenantId &&
                        d.Sender != null &&
                        (d.Sender.DisplayName.Contains(q) ||
                         d.Sender.Email.Contains(q)))
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Returns one entry per unique sender seen at this tenant.
    /// Queries the Sender table directly — accurate and deduplicated.
    /// Only includes senders that have at least one document.
    /// </summary>
    public async Task<List<SenderSummary>> GetDistinctSendersAsync(Guid tenantId)
        => await _db.Senders
            .Where(s => s.TenantId == tenantId &&
                        s.DisplayName != "Unknown Sender" &&
                        s.EndDate > DateTime.UtcNow)
            .Select(s => new SenderSummary
            {
                DisplayName = s.DisplayName,
                Email = s.Email,
                DocumentCount = _db.Documents.Count(d =>
                    d.TenantId == tenantId && d.SenderId == s.SenderId)
            })
            .Where(s => s.DocumentCount > 0)
            .OrderBy(s => s.DisplayName == string.Empty ? s.Email : s.DisplayName)
            .ToListAsync();

    public async Task SaveAsync()
        => await _db.SaveChangesAsync();
}