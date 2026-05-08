using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Doario.Data.Models.Mail;
using Microsoft.EntityFrameworkCore;

namespace Doario.Data.Repositories
{
    public class AiSuggestionRepository : IAiSuggestionRepository
    {
        private readonly DoarioDataContext _db;

        public AiSuggestionRepository(DoarioDataContext db)
        {
            _db = db;
        }

        public async Task<DocumentAiSuggestion> GetByIdAsync(Guid suggestionId, Guid tenantId)
            => await _db.DocumentAiSuggestions
                .Include(s => s.SuggestedStaff)
                .Include(s => s.SuggestionStatus)
                .Include(s => s.Document)
                .FirstOrDefaultAsync(s => s.DocumentAiSuggestionId == suggestionId && s.TenantId == tenantId);

        public async Task<DocumentAiSuggestion> GetByDocumentAsync(Guid documentId, Guid tenantId)
            => await _db.DocumentAiSuggestions
                .Include(s => s.SuggestedStaff)
                .Include(s => s.SuggestionStatus)
                .FirstOrDefaultAsync(s => s.DocumentId == documentId && s.TenantId == tenantId);

        public async Task<List<DocumentAiSuggestion>> GetPendingForTenantAsync(Guid tenantId)
            => await _db.DocumentAiSuggestions
                .Include(s => s.SuggestedStaff)
                .Include(s => s.Document)
                    .ThenInclude(d => d.DocumentStatus)
                .Include(s => s.Document)
                    .ThenInclude(d => d.Sender)
                .Where(s => s.TenantId == tenantId && s.SuggestionStatusId == 1) // 1 = Pending
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

        public async Task AddAsync(DocumentAiSuggestion suggestion)
        {
            _db.DocumentAiSuggestions.Add(suggestion);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(DocumentAiSuggestion suggestion)
        {
            _db.DocumentAiSuggestions.Update(suggestion);
            await _db.SaveChangesAsync();
        }

        public async Task<int> GetPendingCountAsync(Guid tenantId)
            => await _db.DocumentAiSuggestions
                .CountAsync(s => s.TenantId == tenantId && s.SuggestionStatusId == 1);
    }
}