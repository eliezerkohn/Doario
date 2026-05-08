using System;
using System.Threading.Tasks;
using Doario.Data.Models.SaaS;

namespace Doario.Data.Repositories
{
    public interface ITenantAiSettingsRepository
    {
        Task<TenantAiSettings> GetByTenantAsync(Guid tenantId);
        Task UpsertAsync(Guid tenantId, string mode, int confidenceThreshold = 8);
    }
}