using Doario.Data.Models.Mail;
using Microsoft.EntityFrameworkCore;

namespace Doario.Data.Repositories;

public class TenantWhitelistedSenderRepository : ITenantWhitelistedSenderRepository
{
    private readonly DoarioDataContext _db;

    public TenantWhitelistedSenderRepository(DoarioDataContext db) => _db = db;

    public async Task AddAsync(TenantWhitelistedSender sender)
    {
        _db.TenantWhitelistedSenders.Add(sender);
        await _db.SaveChangesAsync();
    }

    public async Task<List<TenantWhitelistedSender>> GetAllForTenantAsync(Guid tenantId)
        => await _db.TenantWhitelistedSenders
            .Where(w => w.TenantId == tenantId && w.EndDate > DateTime.UtcNow)
            .ToListAsync();

    /// <summary>
    /// Returns true if any whitelisted sender identifier appears in the OCR text.
    /// Case-insensitive match — if "medlinesupply.com" is whitelisted and
    /// the OCR text contains "medlinesupply.com", this returns true.
    /// </summary>
    public async Task<bool> IsWhitelistedAsync(Guid tenantId, string ocrText)
    {
        var whitelist = await GetAllForTenantAsync(tenantId);
        var lower = ocrText.ToLowerInvariant();
        return whitelist.Any(w => lower.Contains(w.SenderIdentifier.ToLowerInvariant()));
    }
}