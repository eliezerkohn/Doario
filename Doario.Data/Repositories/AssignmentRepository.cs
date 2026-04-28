using Doario.Data.Models.Mail;
using Microsoft.EntityFrameworkCore;

namespace Doario.Data.Repositories;

public class AssignmentRepository : IAssignmentRepository
{
    private readonly DoarioDataContext _db;

    public AssignmentRepository(DoarioDataContext db) => _db = db;

    public async Task<DocumentAssignment> GetByDocumentAsync(Guid documentId, Guid tenantId)
        => await _db.DocumentAssignments
            .Include(a => a.AssignedToStaff)
            .FirstOrDefaultAsync(a => a.DocumentId == documentId
                                   && a.TenantId == tenantId);

    public async Task<DocumentAssignment> GetByIdAsync(Guid assignmentId, Guid tenantId)
        => await _db.DocumentAssignments
            .Include(a => a.AssignedToStaff)
            .FirstOrDefaultAsync(a => a.DocumentAssignmentId == assignmentId
                                   && a.TenantId == tenantId);

    public async Task<List<DocumentAssignment>> GetAllByDocumentAsync(Guid documentId, Guid tenantId)
        => await _db.DocumentAssignments
            .Where(a => a.DocumentId == documentId && a.TenantId == tenantId)
            .ToListAsync();

    public async Task AddAsync(DocumentAssignment assignment)
    {
        _db.DocumentAssignments.Add(assignment);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteRangeAsync(List<DocumentAssignment> assignments)
    {
        _db.DocumentAssignments.RemoveRange(assignments);
        await _db.SaveChangesAsync();
    }

    public async Task SaveAsync()
        => await _db.SaveChangesAsync();

    public async Task<List<DocumentAssignment>> GetByEmailAsync(string email, Guid tenantId)
        => await _db.DocumentAssignments
            .Include(a => a.Document)
                .ThenInclude(d => d.DocumentStatus)
            .Include(a => a.AssignedToStaff)
            .Where(a => a.TenantId == tenantId
                     && a.AssignedToEmail.ToLower() == email.ToLower())
            .OrderByDescending(a => a.AssignedAt)
            .ToListAsync();
}