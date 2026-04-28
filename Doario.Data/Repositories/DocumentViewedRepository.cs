using Doario.Data.Models.Mail;
using Microsoft.EntityFrameworkCore;

namespace Doario.Data.Repositories;

public class DocumentViewedRepository : IDocumentViewedRepository
{
    private readonly DoarioDataContext _db;

    public DocumentViewedRepository(DoarioDataContext db) => _db = db;

    public async Task MarkViewedAsync(Guid documentId, Guid tenantId, Guid viewedByStaffId)
    {
        // Only insert if not already viewed — one row per document per tenant
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

    public async Task<HashSet<Guid>> GetViewedDocumentIdsAsync(Guid tenantId)
        => (await _db.DocumentVieweds
            .Where(v => v.TenantId == tenantId)
            .Select(v => v.DocumentId)
            .ToListAsync())
            .ToHashSet();
}