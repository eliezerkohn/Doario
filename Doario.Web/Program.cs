using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.Identity.Abstractions;
using Microsoft.Graph;
using Doario.Data;
using Microsoft.EntityFrameworkCore;
using Doario.Web.Middleware;
using Doario.Web.Services;
using Azure.Identity;
using System.Security.Claims;

namespace Doario.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ── Database ──────────────────────────────────────────────────────────
        builder.Services.AddDbContext<DoarioDataContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("ConStr")));

        // ── Microsoft Identity / Azure AD ─────────────────────────────────────
        builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApp(options =>
            {
                builder.Configuration.GetSection("AzureAd").Bind(options);
                options.TokenValidationParameters.RoleClaimType =
                    "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
                options.Events = new Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectEvents
                {
                    OnTokenValidated = ctx =>
                    {
                        var roleClaims = ctx.Principal.Claims
                            .Where(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
                                     || c.Type == "roles")
                            .Select(c => new Claim(ClaimTypes.Role, c.Value))
                            .ToList();

                        if (roleClaims.Any())
                        {
                            var identity = ctx.Principal.Identity as System.Security.Claims.ClaimsIdentity;
                            identity?.AddClaims(roleClaims);
                        }
                        return Task.CompletedTask;
                    }
                };
            })
            .EnableTokenAcquisitionToCallDownstreamApi()
            .AddInMemoryTokenCaches();

        // ── Microsoft Graph v5 — manual registration ──────────────────────────
        builder.Services.AddScoped<GraphServiceClient>(sp =>
        {
            var credential = new ClientSecretCredential(
                builder.Configuration["AzureAd:TenantId"],
                builder.Configuration["AzureAd:ClientId"],
                builder.Configuration["AzureAd:ClientSecret"]);
            return new GraphServiceClient(credential);
        });

        // ── Authorisation ─────────────────────────────────────────────────────
        builder.Services.AddAuthorization(options =>
        {
            options.FallbackPolicy = options.DefaultPolicy;
        });

        // ── MVC + Identity UI ─────────────────────────────────────────────────
        builder.Services.AddControllersWithViews()
            .AddMicrosoftIdentityUI();

        builder.Services.AddRazorPages();

        // ── Tenant Resolution ─────────────────────────────────────────────────
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<TenantContext>();
        builder.Services.AddScoped<TenantResolutionMiddleware>();

        // ── OCR Service ───────────────────────────────────────────────────────
        builder.Services.Configure<OcrOptions>(
            builder.Configuration.GetSection("DocumentIntelligence"));
        builder.Services.AddScoped<OcrService>();

        // ── SharePoint ────────────────────────────────────────────────────────
        builder.Services.Configure<SharePointOptions>(
            builder.Configuration.GetSection("SharePoint"));
        builder.Services.AddScoped<SharePointService>();

        // ── AiSummaryService ──────────────────────────────────────────────────
        builder.Services.AddScoped<AiSummaryService>();

        var app = builder.Build();



        // ── Pipeline ──────────────────────────────────────────────────────────
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        app.UseStaticFiles();

        // ── SPA Dev Server ────────────────────────────────────────────────────
        if (app.Environment.IsDevelopment())
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "npm",
                Arguments = "run dev",
                WorkingDirectory = Path.Combine(
                    Directory.GetCurrentDirectory(), "ClientApp"),
                UseShellExecute = true
            };

            var spaProcess = System.Diagnostics.Process.Start(psi);
            app.Lifetime.ApplicationStopping.Register(() =>
            {
                if (spaProcess is { HasExited: false })
                    spaProcess.Kill(true);
            });
        }

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<TenantResolutionMiddleware>();

        app.MapGet("/home", () => Results.Content(
            "<html><body><h1>Doario</h1><p>Logged in.</p></body></html>",
            "text/html"));

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller}/{action=Index}/{id?}");

        app.MapRazorPages();
        app.MapFallbackToFile("index.html");

        app.Run();
    }
}