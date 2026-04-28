using Doario.Data.Models.SaaS;

namespace Doario.Data.Repositories;

public interface ITenantRepository
{
    Task<Tenant> GetByIdAsync(Guid tenantId);
    Task<Tenant> GetByDomainAsync(string domain);
    Task UpdateSiteIdAsync(Guid tenantId, string siteId);
    Task SaveAsync();
    Task<Tenant> GetByApiKeyAsync(string apiKeyHash);
}