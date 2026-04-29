using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.Graph;
using Doario.Data;
using Doario.Data.Repositories;
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
                options.Events = new OpenIdConnectEvents
                {
                    OnTokenValidated = ctx =>
                    {
                        var roleClaims = ctx.Principal.Claims
                            .Where(c =>
                                c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
                             || c.Type == "roles")
                            .Select(c => new Claim(ClaimTypes.Role, c.Value))
                            .ToList();

                        if (roleClaims.Any())
                        {
                            var identity = ctx.Principal.Identity
                                as System.Security.Claims.ClaimsIdentity;
                            identity?.AddClaims(roleClaims);
                        }
                        return Task.CompletedTask;
                    }
                };
            })
            .EnableTokenAcquisitionToCallDownstreamApi()
            .AddInMemoryTokenCaches();

        // ── Microsoft Graph ───────────────────────────────────────────────────
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

        // ── Tenant resolution ─────────────────────────────────────────────────
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<TenantContext>();
        builder.Services.AddScoped<TenantResolutionMiddleware>();

        // ── Repositories ──────────────────────────────────────────────────────
        builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
        builder.Services.AddScoped<IAssignmentRepository, AssignmentRepository>();
        builder.Services.AddScoped<IDeliveryRepository, DeliveryRepository>();
        builder.Services.AddScoped<IStaffRepository, StaffRepository>();
        builder.Services.AddScoped<ITenantRepository, TenantRepository>();
        builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        builder.Services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
        builder.Services.AddScoped<IDocumentFeedbackRepository, DocumentFeedbackRepository>();
        builder.Services.AddScoped<ITenantWhitelistedSenderRepository, TenantWhitelistedSenderRepository>();
        builder.Services.AddScoped<IErrorLogRepository, ErrorLogRepository>();
        builder.Services.AddScoped<IDocumentViewedRepository, DocumentViewedRepository>();
        builder.Services.AddScoped<ISenderRepository, SenderRepository>();

        // ── Services ──────────────────────────────────────────────────────────
        builder.Services.Configure<OcrOptions>(
            builder.Configuration.GetSection("DocumentIntelligence"));
        builder.Services.AddScoped<OcrService>();

        builder.Services.Configure<SharePointOptions>(
            builder.Configuration.GetSection("SharePoint"));
        builder.Services.AddScoped<SharePointService>();

        builder.Services.AddScoped<AiSummaryService>();
        builder.Services.AddScoped<EmailDeliveryService>();
        builder.Services.AddScoped<AssignmentService>();
        builder.Services.AddScoped<StaffSyncService>();
        builder.Services.AddScoped<StaffCsvService>();

        builder.Services.AddScoped<ApiKeyService>();

        builder.Services.AddScoped<PdfService>();
        builder.Services.AddScoped<AiBatchSplitService>();

        builder.Services.AddScoped<SenderResolutionService>();

        builder.Services.AddScoped<IExtractionFieldRepository, ExtractionFieldRepository>();
        builder.Services.AddScoped<IDocumentCheckRepository, DocumentCheckRepository>();

        var app = builder.Build();

        // ── Pipeline ──────────────────────────────────────────────────────────
        if (!app.Environment.IsDevelopment())
            app.UseHsts();

        app.UseStaticFiles();

        if (app.Environment.IsDevelopment())
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "npm",
                Arguments = "run dev",
                WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "ClientApp"),
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