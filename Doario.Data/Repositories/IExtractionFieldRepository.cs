using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Doario.Data.Models.Mail;

namespace Doario.Data.Repositories
{
    public interface IExtractionFieldRepository
    {
        Task<List<TenantExtractionField>> GetActiveFieldsAsync(Guid tenantId);
        Task<List<TenantExtractionField>> GetAllFieldsAsync(Guid tenantId);
        Task<TenantExtractionField> GetByIdAsync(Guid tenantExtractionFieldId);
        Task AddFieldAsync(TenantExtractionField field);
        Task UpdateFieldAsync(TenantExtractionField field);
        Task DeleteFieldAsync(Guid tenantExtractionFieldId);
    }
}