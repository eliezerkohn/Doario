using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Doario.Data.Models.Mail;
using Microsoft.EntityFrameworkCore;

namespace Doario.Data.Repositories
{
    public class ErrorLogRepository : IErrorLogRepository
    {
        private readonly DoarioDataContext _db;

        public ErrorLogRepository(DoarioDataContext db)
        {
            _db = db;
        }

        public async Task AddAsync(ErrorLog errorLog)
        {
            await _db.ErrorLogs.AddAsync(errorLog);
        }

        public async Task<List<ErrorLog>> GetRecentForTenantAsync(Guid tenantId, int count = 50)
        {
            return await _db.ErrorLogs
                .Where(e => e.TenantId == tenantId)
                .OrderByDescending(e => e.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task SaveAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}