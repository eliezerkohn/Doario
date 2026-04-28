using Doario.Data.Models.SaaS;
using Microsoft.EntityFrameworkCore;

namespace Doario.Data.Repositories;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly DoarioDataContext _db;

    public SubscriptionRepository(DoarioDataContext db)
    {
        _db = db;
    }

    public async Task<TenantSubscription> GetActiveForTenantAsync(Guid tenantId)
    {
        var now = DateTime.UtcNow;

        return await _db.TenantSubscriptions
            .Where(s => s.TenantId == tenantId
                     && s.StartDate <= now
                     && s.EndDate >= now)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync();
    }
}
