using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Doario.Data.Models.SaaS;

namespace Doario.Data.Repositories
{
    public interface ITenantMonitoredInboxRepository
    {
        Task<List<TenantMonitoredInbox>> GetActiveForTenantAsync(Guid tenantId);
        Task<List<TenantMonitoredInbox>> GetAllForTenantAsync(Guid tenantId);
        Task<List<TenantMonitoredInbox>> GetAllActiveAsync(); // for background service
        Task<TenantMonitoredInbox> GetByIdAsync(Guid id);
        Task AddAsync(TenantMonitoredInbox inbox);
        Task UpdateLastProcessedAtAsync(Guid inboxId, DateTime processedAt);
        Task DeactivateAsync(Guid id);   // sets EndDate = UtcNow
        Task RestoreAsync(Guid id);      // sets EndDate = MaxValue
        Task SaveAsync();
    }
}