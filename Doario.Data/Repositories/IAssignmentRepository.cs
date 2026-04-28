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

    /// <summary>
    /// Returns all assignments for a given email address within a tenant.
    /// Includes the Document and DocumentStatus navigation properties
    /// so the portal can show full document details in the search results.
    /// </summary>
    Task<List<DocumentAssignment>> GetByEmailAsync(string email, Guid tenantId);
}