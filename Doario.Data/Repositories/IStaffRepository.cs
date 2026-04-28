using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Doario.Data.Models.Mail;

namespace Doario.Data.Repositories
{
    public interface IStaffRepository
    {
        Task<ImportedStaff> GetByIdAsync(Guid staffId, Guid tenantId);
        Task<ImportedStaff> GetByEmailAsync(string email, Guid tenantId);
        Task<List<ImportedStaff>> GetAllForTenantAsync(Guid tenantId);
        Task AddAsync(ImportedStaff staff);
        Task SaveAsync();
        Task UpsertRangeAsync(List<ImportedStaff> staff);
    }
}