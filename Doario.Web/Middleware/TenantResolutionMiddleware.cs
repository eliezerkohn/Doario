using System.Collections.Concurrent;
using System.Security.Claims;
using Doario.Data;
using Microsoft.EntityFrameworkCore;

namespace Doario.Web.Middleware;

public class TenantResolutionMiddleware : IMiddleware
{
    private readonly DoarioDataContext _db;

    // Cache tenant lookups in memory — keyed by domain
    // Tenants don't change at runtime so this is safe to cache indefinitely
    private static readonly ConcurrentDictionary<string, (Guid TenantId, string Name)?> _cache = new();

    public TenantResolutionMiddleware(DoarioDataContext db)
    {
        _db = db;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.User.Identity is { IsAuthenticated: true })
        {
            var upn = context.User.FindFirstValue("preferred_username")
                   ?? context.User.FindFirstValue(ClaimTypes.Upn)
                   ?? context.User.FindFirstValue(ClaimTypes.Email);

            if (!string.IsNullOrEmpty(upn))
            {
                var domain = upn.Split('@').LastOrDefault();

                if (!string.IsNullOrEmpty(domain))
                {
                    // Check cache first — only hit DB on first request per domain
                    if (!_cache.TryGetValue(domain, out var cached))
                    {
                        var tenant = await _db.Tenants
                            .Where(t => t.Domain == domain)
                            .Select(t => new { t.TenantId, t.Name })
                            .FirstOrDefaultAsync();

                        cached = tenant is not null
                            ? (tenant.TenantId, tenant.Name)
                            : null;

                        _cache[domain] = cached;
                    }

                    if (cached.HasValue)
                    {
                        context.Items["TenantId"] = cached.Value.TenantId;
                        context.Items["TenantName"] = cached.Value.Name;
                    }
                }
            }
        }

        await next(context);
    }
}