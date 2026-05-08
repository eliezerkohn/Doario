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

    /// <summary>
    /// Ends the current active subscription and creates a new one from the given plan.
    /// Preserves StripeCustomerId linkage — Stripe subscription switching handled separately.
    /// </summary>
    public async Task<TenantSubscription> SwitchPlanAsync(Guid tenantId, SubscriptionPlan newPlan)
    {
        var now = DateTime.UtcNow;

        // End all currently active subscriptions
        await _db.TenantSubscriptions
            .Where(s => s.TenantId == tenantId && s.EndDate == DateTime.MaxValue)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.EndDate, now));

        // Create new subscription from plan
        var newSub = new TenantSubscription
        {
            TenantSubscriptionId = Guid.NewGuid(),
            TenantId = tenantId,
            SubscriptionPlanId = newPlan.SubscriptionPlanId,
            PlanName = newPlan.Name,
            MonthlyPrice = newPlan.MonthlyPrice,
            IncludedDocuments = newPlan.IncludedDocuments,
            ExtraDocumentPrice = newPlan.ExtraDocumentPrice,
            DiscountPercent = 0,
            StripePlanId = newPlan.StripePriceId ?? string.Empty,
            StripeSubscriptionId = string.Empty,
            StripeSubscriptionItemId = string.Empty,
            StartDate = now,
            EndDate = DateTime.MaxValue,
        };

        _db.TenantSubscriptions.Add(newSub);
        await _db.SaveChangesAsync();

        return newSub;
    }
}