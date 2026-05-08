using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Doario.Data.Models.SaaS;
using Microsoft.EntityFrameworkCore;

namespace Doario.Data.Repositories
{
    public class TenantMonitoredInboxRepository : ITenantMonitoredInboxRepository
    {
        private readonly DoarioDataContext _db;

        public TenantMonitoredInboxRepository(DoarioDataContext db)
        {
            _db = db;
        }

        public async Task<List<TenantMonitoredInbox>> GetActiveForTenantAsync(Guid tenantId)
        {
            var now = DateTime.UtcNow;
            return await _db.TenantMonitoredInboxes
                .Where(x => x.TenantId == tenantId && x.StartDate <= now && x.EndDate >= now)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<TenantMonitoredInbox>> GetAllForTenantAsync(Guid tenantId)
            => await _db.TenantMonitoredInboxes
                .Where(x => x.TenantId == tenantId)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync();

        public async Task<List<TenantMonitoredInbox>> GetAllActiveAsync()
        {
            var now = DateTime.UtcNow;
            return await _db.TenantMonitoredInboxes
                .Where(x => x.StartDate <= now && x.EndDate >= now)
                .ToListAsync();
        }

        public async Task<TenantMonitoredInbox> GetByIdAsync(Guid id)
            => await _db.TenantMonitoredInboxes.FirstOrDefaultAsync(x => x.TenantMonitoredInboxId == id);

        public async Task AddAsync(TenantMonitoredInbox inbox)
        {
            _db.TenantMonitoredInboxes.Add(inbox);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateLastProcessedAtAsync(Guid inboxId, DateTime processedAt)
        {
            await _db.TenantMonitoredInboxes
                .Where(x => x.TenantMonitoredInboxId == inboxId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.LastProcessedAt, processedAt));
        }

        public async Task DeactivateAsync(Guid id)
        {
            var inbox = await _db.TenantMonitoredInboxes
                .FirstOrDefaultAsync(x => x.TenantMonitoredInboxId == id);
            if (inbox != null)
            {
                inbox.EndDate = DateTime.UtcNow;
                _db.TenantMonitoredInboxes.Update(inbox);
                await _db.SaveChangesAsync();
            }
        }

        public async Task RestoreAsync(Guid id)
        {
            var inbox = await _db.TenantMonitoredInboxes
                .FirstOrDefaultAsync(x => x.TenantMonitoredInboxId == id);
            if (inbox != null)
            {
                inbox.EndDate = DateTime.MaxValue;
                _db.TenantMonitoredInboxes.Update(inbox);
                await _db.SaveChangesAsync();
            }
        }

        public async Task SaveAsync()
            => await _db.SaveChangesAsync();
    }
}