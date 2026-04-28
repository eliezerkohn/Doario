using Doario.Data.Models.SaaS;
using Microsoft.EntityFrameworkCore;

namespace Doario.Data.Repositories;

public class SubscriptionPlanRepository : ISubscriptionPlanRepository
{
    private readonly DoarioDataContext _db;

    public SubscriptionPlanRepository(DoarioDataContext db)
    {
        _db = db;
    }

    public async Task<List<SubscriptionPlan>> GetAllActiveAsync()
    {
        return await _db.SubscriptionPlans
            .Where(p => p.IsActive && p.IsPublic)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();
    }

    public async Task<SubscriptionPlan> GetByIdAsync(Guid planId)
    {
        return await _db.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.SubscriptionPlanId == planId);
    }
}