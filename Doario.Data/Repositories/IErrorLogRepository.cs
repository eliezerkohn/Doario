using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Doario.Data.Models.Mail;

namespace Doario.Data.Repositories
{
    public interface IErrorLogRepository
    {
        Task AddAsync(ErrorLog errorLog);
        Task<List<ErrorLog>> GetRecentForTenantAsync(Guid tenantId, int count = 50);
        Task SaveAsync();
    }
}