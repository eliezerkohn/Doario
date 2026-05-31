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

    /// <summary>
    /// Active = EndDate year is 9999 (open-ended).
    /// Using year check instead of DateTime.MaxValue to handle seeder vs EF precision differences.
    /// </summary>
    public async Task<List<SubscriptionPlan>> GetAllActiveAsync()
    {
        return await _db.SubscriptionPlans
            .Where(p => p.IsActive && p.IsPublic && p.EndDate.Year == 9999)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();
    }

    public async Task<List<SubscriptionPlan>> GetAllAsync()
    {
        return await _db.SubscriptionPlans
            .OrderBy(p => p.SortOrder)
            .ThenByDescending(p => p.StartDate)
            .ToListAsync();
    }

    public async Task<SubscriptionPlan> GetByIdAsync(Guid planId)
    {
        return await _db.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.SubscriptionPlanId == planId);
    }

    public async Task<List<SubscriptionPlan>> GetPlanHistoryAsync(string planName)
    {
        return await _db.SubscriptionPlans
            .Where(p => p.Name == planName)
            .OrderByDescending(p => p.StartDate)
            .ToListAsync();
    }

    public async Task<SubscriptionPlan> GetPlanAtDateAsync(string planName, DateTime date)
    {
        return await _db.SubscriptionPlans
            .Where(p => p.Name == planName
                     && p.StartDate <= date
                     && p.EndDate > date)
            .FirstOrDefaultAsync();
    }

    public async Task CreateAsync(SubscriptionPlan plan)
    {
        _db.SubscriptionPlans.Add(plan);
        await _db.SaveChangesAsync();
    }

    public async Task CloseAsync(Guid subscriptionPlanId, DateTime closedAt)
    {
        await _db.SubscriptionPlans
            .Where(p => p.SubscriptionPlanId == subscriptionPlanId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.EndDate, closedAt));
    }
}