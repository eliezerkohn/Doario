using Doario.Data.Models.SaaS;

namespace Doario.Data.Repositories;

public interface ISubscriptionRepository
{
    Task<TenantSubscription> GetActiveForTenantAsync(Guid tenantId);
}
