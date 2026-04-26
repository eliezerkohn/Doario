using Doario.Data.Models.SaaS;
using Microsoft.EntityFrameworkCore;

namespace Doario.Data.Repositories;

public class TenantRepository : ITenantRepository
{
    private readonly DoarioDataContext _db;

    public TenantRepository(DoarioDataContext db) => _db = db;

    public async Task<Tenant> GetByIdAsync(Guid tenantId)
        => await _db.Tenants.FindAsync(tenantId);

    public async Task<Tenant> GetByDomainAsync(string domain)
        => await _db.Tenants
            .FirstOrDefaultAsync(t => t.Domain == domain);

    public async Task UpdateSiteIdAsync(Guid tenantId, string siteId)
    {
        var tenant = await _db.Tenants.FindAsync(tenantId);
        if (tenant is null) return;
        tenant.SharePointSiteId = siteId;
        await _db.SaveChangesAsync();
    }

    public async Task SaveAsync()
        => await _db.SaveChangesAsync();
}