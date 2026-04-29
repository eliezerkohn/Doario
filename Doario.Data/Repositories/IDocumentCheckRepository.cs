using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Doario.Data.Models.Mail;

namespace Doario.Data.Repositories
{
    public interface IDocumentCheckRepository
    {
        Task<DocumentCheck> GetByDocumentIdAsync(Guid documentId);
        Task SaveAsync(DocumentCheck check);
        Task DeleteByDocumentIdAsync(Guid documentId);
        Task<HashSet<Guid>> GetDocumentIdsWithChecksAsync(Guid tenantId);
        Task<List<DocumentCheckQueryResult>> GetAllForTenantAsync(Guid tenantId);
    }
}