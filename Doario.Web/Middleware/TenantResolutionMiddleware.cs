using System.Security.Claims;
using Doario.Data;
using Microsoft.EntityFrameworkCore;

namespace Doario.Web.Middleware;

public class TenantResolutionMiddleware : IMiddleware
{
    private readonly DoarioDataContext _db;

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
                    var tenant = await _db.Tenants
                        .Where(t => t.Domain == domain)
                        .Select(t => new { t.TenantId, t.Name })
                        .FirstOrDefaultAsync();

                    if (tenant is not null)
                    {
                        context.Items["TenantId"] = tenant.TenantId;
                        context.Items["TenantName"] = tenant.Name;
                    }
                }
            }
        }

        await next(context);
    }
}