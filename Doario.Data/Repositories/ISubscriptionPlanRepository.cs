using Doario.Data.Models.SaaS;

namespace Doario.Data.Repositories;

public interface ISubscriptionPlanRepository
{
    /// <summary>All currently active public plans (EndDate = MaxValue, IsActive = true).</summary>
    Task<List<SubscriptionPlan>> GetAllActiveAsync();

    /// <summary>All plans including inactive and historical — for admin reporting.</summary>
    Task<List<SubscriptionPlan>> GetAllAsync();

    /// <summary>Get a single plan by ID.</summary>
    Task<SubscriptionPlan> GetByIdAsync(Guid planId);

    /// <summary>Full price history for a plan by name.</summary>
    Task<List<SubscriptionPlan>> GetPlanHistoryAsync(string planName);

    /// <summary>What a plan cost on a specific date.</summary>
    Task<SubscriptionPlan> GetPlanAtDateAsync(string planName, DateTime date);

    /// <summary>Insert a new plan or price row.</summary>
    Task CreateAsync(SubscriptionPlan plan);

    /// <summary>Close a price row by setting EndDate = closedAt. Call before inserting a new price.</summary>
    Task CloseAsync(Guid subscriptionPlanId, DateTime closedAt);
}