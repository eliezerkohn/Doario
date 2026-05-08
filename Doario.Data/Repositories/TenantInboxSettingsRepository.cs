using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Doario.Data.Models.SaaS;
using Microsoft.EntityFrameworkCore;

namespace Doario.Data.Repositories
{
    public class TenantInboxSettingsRepository : ITenantInboxSettingsRepository
    {
        private readonly DoarioDataContext _db;

        public TenantInboxSettingsRepository(DoarioDataContext db)
        {
            _db = db;
        }

        public async Task<TenantInboxSettings> GetByTenantAsync(Guid tenantId)
            => await _db.TenantInboxSettings
                .FirstOrDefaultAsync(s => s.TenantId == tenantId);

        public async Task<List<TenantInboxSettings>> GetAllAsync()
            => await _db.TenantInboxSettings.ToListAsync();

        public async Task UpsertAsync(Guid tenantId, int pollingIntervalSeconds)
        {
            var existing = await _db.TenantInboxSettings
                .FirstOrDefaultAsync(s => s.TenantId == tenantId);

            if (existing != null)
            {
                existing.InboxPollingIntervalSeconds = pollingIntervalSeconds;
                existing.UpdatedAt = DateTime.UtcNow;
                _db.TenantInboxSettings.Update(existing);
            }
            else
            {
                _db.TenantInboxSettings.Add(new TenantInboxSettings
                {
                    TenantInboxSettingsId = Guid.NewGuid(),
                    TenantId = tenantId,
                    InboxPollingIntervalSeconds = pollingIntervalSeconds,
                    LastStaffSyncAt = DateTime.UtcNow,
                });
            }

            await _db.SaveChangesAsync();
        }

        public async Task UpdateStaffSyncScheduleAsync(Guid tenantId, int staffSyncIntervalHours)
        {
            var settings = await _db.TenantInboxSettings
                .FirstOrDefaultAsync(s => s.TenantId == tenantId);
            if (settings != null)
            {
                settings.StaffSyncIntervalHours = staffSyncIntervalHours;
                settings.UpdatedAt = DateTime.UtcNow;
                _db.TenantInboxSettings.Update(settings);
                await _db.SaveChangesAsync();
            }
        }

        public async Task UpdateLastStaffSyncAtAsync(Guid tenantId, DateTime syncedAt)
        {
            await _db.TenantInboxSettings
                .Where(s => s.TenantId == tenantId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.LastStaffSyncAt, syncedAt)
                    .SetProperty(x => x.UpdatedAt, DateTime.UtcNow));
        }
    }
}