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

    public async Task AddAsync(ImportedStaff staff)
        => await _db.ImportedStaff.AddAsync(staff);

    public async Task SaveAsync()
        => await _db.SaveChangesAsync();

    public async Task UpsertRangeAsync(List<ImportedStaff> staff)
    {
        foreach (var s in staff)
        {
            var existing = await _db.ImportedStaff
                .FirstOrDefaultAsync(x => x.Email == s.Email && x.TenantId == s.TenantId);

            if (existing == null)
                await _db.ImportedStaff.AddAsync(s);
            else
            {
                existing.FirstName = s.FirstName;
                existing.LastName = s.LastName;
                existing.JobTitle = s.JobTitle;
                existing.Department = s.Department;
                existing.Source = s.Source;
                existing.UpdatedAt = DateTime.UtcNow;
            }
        }
        await _db.SaveChangesAsync();
    }
}