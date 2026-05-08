using Doario.Data.Models.Mail;
using Microsoft.EntityFrameworkCore;

namespace Doario.Data.Repositories;

public class DocumentViewedRepository : IDocumentViewedRepository
{
    private readonly DoarioDataContext _db;

    public DocumentViewedRepository(DoarioDataContext db) => _db = db;

    public async Task MarkViewedAsync(Guid documentId, Guid tenantId, Guid viewedByStaffId)
    {
        var exists = await _db.DocumentVieweds
            .AnyAsync(v => v.DocumentId == documentId && v.TenantId == tenantId);

        if (!exists)
        {
            _db.DocumentVieweds.Add(new DocumentViewed
            {
                DocumentViewedId = Guid.NewGuid(),
                TenantId = tenantId,
                DocumentId = documentId,
                ViewedByStaffId = viewedByStaffId,
                ViewedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
    }

    public async Task MarkUnreadAsync(Guid documentId, Guid tenantId)
    {
        var row = await _db.DocumentVieweds
            .FirstOrDefaultAsync(v => v.DocumentId == documentId && v.TenantId == tenantId);

        if (row != null)
        {
            _db.DocumentVieweds.Remove(row);
            await _db.SaveChangesAsync();
        }
    }

    /// <summary>Returns ALL viewed document IDs for a tenant. Avoid for large tenants.</summary>
    public async Task<HashSet<Guid>> GetViewedDocumentIdsAsync(Guid tenantId)
        => (await _db.DocumentVieweds
            .Where(v => v.TenantId == tenantId)
            .Select(v => v.DocumentId)
            .ToListAsync())
            .ToHashSet();

    /// <summary>
    /// Returns viewed document IDs scoped to a specific page of documents.
    /// Only queries the IDs we actually need — much faster than loading all.
    /// </summary>
    public async Task<HashSet<Guid>> GetViewedDocumentIdsAsync(Guid tenantId, IEnumerable<Guid> documentIds)
    {
        var ids = documentIds.ToList();
        if (!ids.Any()) return new HashSet<Guid>();

        return (await _db.DocumentVieweds
            .Where(v => v.TenantId == tenantId && ids.Contains(v.DocumentId))
            .Select(v => v.DocumentId)
            .ToListAsync())
            .ToHashSet();
    }
}