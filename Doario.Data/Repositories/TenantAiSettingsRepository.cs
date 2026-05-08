using System;
using System.Threading.Tasks;
using Doario.Data.Models.SaaS;
using Microsoft.EntityFrameworkCore;

namespace Doario.Data.Repositories
{
    public class TenantAiSettingsRepository : ITenantAiSettingsRepository
    {
        private readonly DoarioDataContext _db;

        public TenantAiSettingsRepository(DoarioDataContext db)
        {
            _db = db;
        }

        public async Task<TenantAiSettings> GetByTenantAsync(Guid tenantId)
            => await _db.TenantAiSettings
                .FirstOrDefaultAsync(s => s.TenantId == tenantId);

        public async Task UpsertAsync(Guid tenantId, string mode, int confidenceThreshold = 8)
        {
            var existing = await _db.TenantAiSettings
                .FirstOrDefaultAsync(s => s.TenantId == tenantId);

            if (existing != null)
            {
                // Use direct SQL update to avoid EF tracking issues
                await _db.TenantAiSettings
                    .Where(s => s.TenantId == tenantId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.AiAssignmentMode, mode)
                        .SetProperty(x => x.AiConfidenceThreshold, confidenceThreshold)
                        .SetProperty(x => x.UpdatedAt, DateTime.UtcNow));
            }
            else
            {
                _db.TenantAiSettings.Add(new TenantAiSettings
                {
                    TenantAiSettingsId = Guid.NewGuid(),
                    TenantId = tenantId,
                    AiAssignmentMode = mode,
                    AiConfidenceThreshold = confidenceThreshold,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                });
                await _db.SaveChangesAsync();
            }
        }
    }
}