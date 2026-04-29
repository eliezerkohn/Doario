using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Doario.Data.Models.Mail;
using Microsoft.EntityFrameworkCore;

namespace Doario.Data.Repositories
{
    public class ExtractionFieldRepository : IExtractionFieldRepository
    {
        private readonly DoarioDataContext _db;

        public ExtractionFieldRepository(DoarioDataContext db)
        {
            _db = db;
        }

        public async Task<List<TenantExtractionField>> GetActiveFieldsAsync(Guid tenantId)
        {
            var now = DateTime.UtcNow;
            return await _db.TenantExtractionFields
                .Where(f => f.TenantId == tenantId
                         && f.StartDate <= now
                         && f.EndDate >= now)
                .OrderBy(f => f.SortOrder)
                .ToListAsync();
        }

        public async Task<List<TenantExtractionField>> GetAllFieldsAsync(Guid tenantId)
        {
            return await _db.TenantExtractionFields
                .Where(f => f.TenantId == tenantId)
                .OrderBy(f => f.SortOrder)
                .ToListAsync();
        }

        public async Task<TenantExtractionField> GetByIdAsync(Guid tenantExtractionFieldId)
        {
            return await _db.TenantExtractionFields
                .FirstOrDefaultAsync(f => f.TenantExtractionFieldId == tenantExtractionFieldId);
        }

        public async Task AddFieldAsync(TenantExtractionField field)
        {
            _db.TenantExtractionFields.Add(field);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateFieldAsync(TenantExtractionField field)
        {
            _db.TenantExtractionFields.Update(field);
            await _db.SaveChangesAsync();
        }

        // Soft delete — sets EndDate to now so the field stops being active
        // but remains in DB for history. GetAllFieldsAsync still returns it.
        public async Task DeleteFieldAsync(Guid tenantExtractionFieldId)
        {
            var field = await _db.TenantExtractionFields
                .FirstOrDefaultAsync(f => f.TenantExtractionFieldId == tenantExtractionFieldId);
            if (field != null)
            {
                field.EndDate = DateTime.UtcNow;
                _db.TenantExtractionFields.Update(field);
                await _db.SaveChangesAsync();
            }
        }
    }
}