using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.Identity.Abstractions;
using Microsoft.Graph;
using Doario.Data;
using Microsoft.EntityFrameworkCore;
using Doario.Web.Middleware;
using Doario.Web.Services;
using Azure.Identity;

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
            .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
            .EnableTokenAcquisitionToCallDownstreamApi()
            .AddInMemoryTokenCaches();

        // ── Microsoft Graph v5 — manual registration ──────────────────────────
        builder.Services.AddScoped<GraphServiceClient>(sp =>
        {
            var credential = new Azure.Identity.ClientSecretCredential(
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

        // ── SharePoint ────────────────────────────────────────────────────────
        builder.Services.Configure<SharePointOptions>(
            builder.Configuration.GetSection("SharePoint"));
        builder.Services.AddScoped<SharePointService>();

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

        // Temporary home page for testing
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