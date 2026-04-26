using Doario.Data.Models.Mail;
using Microsoft.EntityFrameworkCore;

namespace Doario.Data.Repositories;

public class StaffRepository : IStaffRepository
{
    private readonly DoarioDataContext _db;

    public StaffRepository(DoarioDataContext db) => _db = db;

    public async Task<ImportedStaff> GetByIdAsync(Guid staffId, Guid tenantId)
        => await _db.ImportedStaff
            .FirstOrDefaultAsync(s => s.ImportedStaffId == staffId
                                   && s.TenantId == tenantId);

    public async Task<ImportedStaff> GetByEmailAsync(string email, Guid tenantId)
        => await _db.ImportedStaff
            .FirstOrDefaultAsync(s => s.Email == email
                                   && s.TenantId == tenantId);

    public async Task<List<ImportedStaff>> GetAllForTenantAsync(Guid tenantId)
        => await _db.ImportedStaff
            .Where(s => s.TenantId == tenantId)
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ToListAsync();
}