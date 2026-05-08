using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Doario.Data.Models.Mail;

namespace Doario.Data.Repositories
{
    public interface IAiSuggestionRepository
    {
        Task<DocumentAiSuggestion> GetByDocumentAsync(Guid documentId, Guid tenantId);
        Task<DocumentAiSuggestion> GetByIdAsync(Guid suggestionId, Guid tenantId);
        Task<List<DocumentAiSuggestion>> GetPendingForTenantAsync(Guid tenantId);
        Task AddAsync(DocumentAiSuggestion suggestion);
        Task UpdateAsync(DocumentAiSuggestion suggestion);
        Task<int> GetPendingCountAsync(Guid tenantId);
    }
}