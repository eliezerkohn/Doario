using System.Security.Cryptography;
using System.Text;
using Doario.Data.Repositories;

namespace Doario.Web.Services;

public class ApiKeyService
{
    private readonly ITenantRepository _tenantRepo;

    public ApiKeyService(ITenantRepository tenantRepo)
    {
        _tenantRepo = tenantRepo;
    }

    /// <summary>
    /// Generates a new API key for the tenant.
    /// Returns the raw key ONCE — never stored.
    /// Saves hash + prefix to DB.
    /// </summary>
    public async Task<string> GenerateAsync(Guid tenantId)
    {
        var tenant = await _tenantRepo.GetByIdAsync(tenantId);
        if (tenant == null)
            throw new InvalidOperationException("Tenant not found.");

        var rawKey = GenerateRawKey();

        tenant.ApiKeyHash = HashKey(rawKey);
        tenant.ApiKeyPrefix = rawKey[..16];

        await _tenantRepo.SaveAsync();

        return rawKey;
    }

    /// <summary>
    /// Validates an incoming API key against the stored hash.
    /// Used by IngestController to authenticate DoarioScan Bridge requests.
    /// </summary>
    public async Task<bool> ValidateAsync(Guid tenantId, string rawKey)
    {
        var tenant = await _tenantRepo.GetByIdAsync(tenantId);
        if (tenant == null)
            return false;

        if (string.IsNullOrWhiteSpace(tenant.ApiKeyHash))
            return false;

        return tenant.ApiKeyHash == HashKey(rawKey);
    }

    /// <summary>
    /// Returns the key prefix so the portal can show
    /// which key is currently active without exposing the full key.
    /// </summary>
    public async Task<string> GetPrefixAsync(Guid tenantId)
    {
        var tenant = await _tenantRepo.GetByIdAsync(tenantId);
        if (tenant == null)
            return null;

        return tenant.ApiKeyPrefix;
    }

    // ── Private helpers ──────────────────────────────────────────

    private static string GenerateRawKey()
    {
        var random = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "");

        return $"doa_live_{random}";
    }

    private static string HashKey(string rawKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToHexString(bytes).ToLower();
    }
}