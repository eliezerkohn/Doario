using System.Collections.Concurrent;
using System.Security.Claims;
using Doario.Data;
using Microsoft.EntityFrameworkCore;

namespace Doario.Web.Middleware;

public class TenantResolutionMiddleware : IMiddleware
{
    private readonly DoarioDataContext _db;

    // Cache tenant lookups in memory — keyed by domain
    private static readonly ConcurrentDictionary<string, (Guid TenantId, string Name)?> _cache = new();

    public TenantResolutionMiddleware(DoarioDataContext db)
    {
        _db = db;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.User.Identity is { IsAuthenticated: true })
        {
            // Microsoft auth — resolve tenant by email domain
            var upn = context.User.FindFirstValue("preferred_username")
                   ?? context.User.FindFirstValue(ClaimTypes.Upn)
                   ?? context.User.FindFirstValue(ClaimTypes.Email);

            if (!string.IsNullOrEmpty(upn))
            {
                var domain = upn.Split('@').LastOrDefault();

                if (!string.IsNullOrEmpty(domain))
                {
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
        else
        {
            // Demo mode — check for demo cookie and use hardcoded tenant
            var demoCookie = context.Request.Cookies["doario_demo"];
            if (demoCookie == "authenticated")
            {
                context.Items["TenantId"] = Guid.Parse("a1b2c3d4-0001-0001-0001-000000000001");
                context.Items["TenantName"] = "Eli Pro Software Solutions";
            }
        }

        await next(context);
    }
}