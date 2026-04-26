using Doario.Data.Models.Mail;

namespace Doario.Data.Repositories;

public interface IAssignmentRepository
{
    Task<DocumentAssignment> GetByDocumentAsync(Guid documentId, Guid tenantId);
    Task<DocumentAssignment> GetByIdAsync(Guid assignmentId, Guid tenantId);
    Task<List<DocumentAssignment>> GetAllByDocumentAsync(Guid documentId, Guid tenantId);
    Task AddAsync(DocumentAssignment assignment);
    Task DeleteRangeAsync(List<DocumentAssignment> assignments);
    Task SaveAsync();
}