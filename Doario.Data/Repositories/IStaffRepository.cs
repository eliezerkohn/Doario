using Doario.Data.Models.Mail;

namespace Doario.Data.Repositories;

public interface IStaffRepository
{
    Task<ImportedStaff> GetByIdAsync(Guid staffId, Guid tenantId);
    Task<ImportedStaff> GetByEmailAsync(string email, Guid tenantId);
    Task<List<ImportedStaff>> GetAllForTenantAsync(Guid tenantId);
}