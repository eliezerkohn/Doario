using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Doario.Data.Models.SaaS;

namespace Doario.Data.Repositories
{
    public interface ITenantInboxSettingsRepository
    {
        Task<TenantInboxSettings> GetByTenantAsync(Guid tenantId);
        Task<List<TenantInboxSettings>> GetAllAsync();
        Task UpsertAsync(Guid tenantId, int pollingIntervalSeconds);
        Task UpdateStaffSyncScheduleAsync(Guid tenantId, int staffSyncIntervalHours);
        Task UpdateLastStaffSyncAtAsync(Guid tenantId, DateTime syncedAt);
    }
}