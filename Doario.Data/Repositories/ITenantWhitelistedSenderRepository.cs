using Doario.Data.Models.Mail;

namespace Doario.Data.Repositories;

public interface ITenantWhitelistedSenderRepository
{
    Task AddAsync(TenantWhitelistedSender sender);
    Task<List<TenantWhitelistedSender>> GetAllForTenantAsync(Guid tenantId);
    Task<bool> IsWhitelistedAsync(Guid tenantId, string ocrText);
}