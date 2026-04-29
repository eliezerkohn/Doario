using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Doario.Data.Models.Mail;
using Microsoft.EntityFrameworkCore;

namespace Doario.Data.Repositories
{
    public class DocumentCheckRepository : IDocumentCheckRepository
    {
        private readonly DoarioDataContext _db;

        public DocumentCheckRepository(DoarioDataContext db)
        {
            _db = db;
        }

        public async Task<DocumentCheck> GetByDocumentIdAsync(Guid documentId)
        {
            return await _db.DocumentChecks
                .FirstOrDefaultAsync(c => c.DocumentId == documentId);
        }

        public async Task SaveAsync(DocumentCheck check)
        {
            var existing = await _db.DocumentChecks
                .FirstOrDefaultAsync(c => c.DocumentId == check.DocumentId);

            if (existing != null)
            {
                existing.CheckAmount = check.CheckAmount;
                existing.CheckPayerName = check.CheckPayerName;
                existing.CheckNumber = check.CheckNumber;
                _db.DocumentChecks.Update(existing);
            }
            else
            {
                _db.DocumentChecks.Add(check);
            }

            await _db.SaveChangesAsync();
        }

        public async Task DeleteByDocumentIdAsync(Guid documentId)
        {
            var check = await _db.DocumentChecks
                .FirstOrDefaultAsync(c => c.DocumentId == documentId);
            if (check != null)
            {
                _db.DocumentChecks.Remove(check);
                await _db.SaveChangesAsync();
            }
        }

        // Returns a HashSet of DocumentIds that have a check row —
        // used by the queue endpoint to set IsCheck on each document
        public async Task<HashSet<Guid>> GetDocumentIdsWithChecksAsync(Guid tenantId)
        {
            var ids = await _db.DocumentChecks
                .Join(_db.Documents,
                    c => c.DocumentId,
                    d => d.DocumentId,
                    (c, d) => new { c.DocumentId, d.TenantId })
                .Where(x => x.TenantId == tenantId)
                .Select(x => x.DocumentId)
                .ToListAsync();

            return new HashSet<Guid>(ids);
        }

        // Returns all check documents for the tenant — used by ChecksSearch
        // Projects into DocumentCheckDto — a web-layer DTO in Doario.Web.Models
        // Controller calls this and projects to anonymous object for the response
        public async Task<List<DocumentCheckQueryResult>> GetAllForTenantAsync(Guid tenantId)
        {
            return await _db.DocumentChecks
                .Join(_db.Documents,
                    c => c.DocumentId,
                    d => d.DocumentId,
                    (c, d) => new { c, d })
                .Where(x => x.d.TenantId == tenantId)
                .OrderByDescending(x => x.c.CreatedAt)
                .Select(x => new DocumentCheckQueryResult
                {
                    DocumentId = x.d.DocumentId,
                    CheckAmount = x.c.CheckAmount,
                    CheckPayerName = x.c.CheckPayerName,
                    CheckNumber = x.c.CheckNumber,
                    CreatedAt = x.c.CreatedAt,
                    OriginalFileName = x.d.OriginalFileName,
                    AiSummary = x.d.AiSummary,
                    UploadedAt = x.d.UploadedAt,
                    SenderDisplayName = x.d.Sender != null ? x.d.Sender.DisplayName : string.Empty,
                })
                .ToListAsync();
        }
    }
}