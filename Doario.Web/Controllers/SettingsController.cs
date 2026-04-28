using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Doario.Data.Repositories;
using Doario.Web.Services;
using Doario.Web.Models;

namespace Doario.Web.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize(Roles = "DoarioAdmin,TenantAdmin")]
public class SettingsController : ControllerBase
{
    private readonly ITenantRepository _tenantRepo;
    private readonly ISubscriptionRepository _subRepo;
    private readonly ISubscriptionPlanRepository _planRepo;
    private readonly IDocumentRepository _docRepo;
    private readonly IStaffRepository _staffRepo;
    private readonly StaffSyncService _staffSync;
    private readonly StaffCsvService _staffCsvService;
    private readonly ApiKeyService _apiKeyService;
    private readonly TenantContext _tenant;

    public SettingsController(
        ITenantRepository tenantRepo,
        ISubscriptionRepository subRepo,
        ISubscriptionPlanRepository planRepo,
        IDocumentRepository docRepo,
        IStaffRepository staffRepo,
        StaffSyncService staffSync,
        StaffCsvService staffCsvService,
        ApiKeyService apiKeyService,
        TenantContext tenant)
    {
        _tenantRepo = tenantRepo;
        _subRepo = subRepo;
        _planRepo = planRepo;
        _docRepo = docRepo;
        _staffRepo = staffRepo;
        _staffSync = staffSync;
        _staffCsvService = staffCsvService;
        _apiKeyService = apiKeyService;
        _tenant = tenant;
    }

    // ── Organisation ─────────────────────────────────────────────

    // GET /api/settings/organisation
    [HttpGet("organisation")]
    public async Task<IActionResult> GetOrganisation()
    {
        if (!_tenant.IsResolved)
            return Unauthorized();

        var tenant = await _tenantRepo.GetByIdAsync(_tenant.TenantId);
        if (tenant == null)
            return NotFound();

        return Ok(new
        {
            tenant.Name,
            tenant.Domain,
            tenant.MailboxAddress,
            tenant.SharePointSiteUrl,
            tenant.IsHipaaEnabled,
            tenant.ScanInboxAddress
        });
    }

    // PUT /api/settings/organisation
    [HttpPut("organisation")]
    public async Task<IActionResult> UpdateOrganisation([FromBody] UpdateOrganisationRequest request)
    {
        if (!_tenant.IsResolved)
            return Unauthorized();

        var tenant = await _tenantRepo.GetByIdAsync(_tenant.TenantId);
        if (tenant == null)
            return NotFound();

        tenant.Name = request.Name?.Trim() ?? tenant.Name;
        tenant.MailboxAddress = request.MailboxAddress?.Trim() ?? tenant.MailboxAddress;
        tenant.SharePointSiteUrl = request.SharePointSiteUrl?.Trim() ?? tenant.SharePointSiteUrl;

        await _tenantRepo.SaveAsync();

        return Ok(new { message = "Organisation updated successfully." });
    }

    // ── Staff Management ─────────────────────────────────────────

    // POST /api/settings/sync-staff
    [HttpPost("sync-staff")]
    public async Task<IActionResult> SyncStaff()
    {
        if (!_tenant.IsResolved)
            return Unauthorized();

        var result = await _staffSync.SyncAsync(_tenant.TenantId);

        if (!result.Success)
            return StatusCode(500, new { error = result.ErrorMessage });

        return Ok(new
        {
            message = $"Sync complete. {result.Added} staff added, {result.Updated} updated.",
            added = result.Added,
            updated = result.Updated,
            totalPulled = result.TotalPulled
        });
    }

    // POST /api/settings/import-staff-csv
    [HttpPost("import-staff-csv")]
    public async Task<IActionResult> ImportStaffCsv(IFormFile file)
    {
        if (!_tenant.IsResolved)
            return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded." });

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "File must be a .csv" });

        var tenant = await _tenantRepo.GetByIdAsync(_tenant.TenantId);
        if (tenant == null)
            return NotFound();

        using var stream = file.OpenReadStream();
        var result = await _staffCsvService.ImportAsync(stream, tenant.TenantId, tenant.Domain);

        return Ok(new
        {
            message = $"Import complete. Added: {result.Added}, Updated: {result.Updated}, Skipped: {result.Skipped}",
            added = result.Added,
            updated = result.Updated,
            skipped = result.Skipped,
            errors = result.Errors
        });
    }

    // ── API Key ───────────────────────────────────────────────────

    // GET /api/settings/api-key
    [HttpGet("api-key")]
    public async Task<IActionResult> GetApiKey()
    {
        if (!_tenant.IsResolved)
            return Unauthorized();

        var prefix = await _apiKeyService.GetPrefixAsync(_tenant.TenantId);

        return Ok(new
        {
            hasKey = !string.IsNullOrWhiteSpace(prefix),
            prefix = string.IsNullOrWhiteSpace(prefix) ? null : prefix + "..."
        });
    }

    // POST /api/settings/generate-api-key
    [HttpPost("generate-api-key")]
    public async Task<IActionResult> GenerateApiKey()
    {
        if (!_tenant.IsResolved)
            return Unauthorized();

        var rawKey = await _apiKeyService.GenerateAsync(_tenant.TenantId);

        return Ok(new
        {
            message = "API key generated. Copy this key now — it will not be shown again.",
            apiKey = rawKey
        });
    }

    // POST /api/settings/regenerate-api-key
    [HttpPost("regenerate-api-key")]
    public async Task<IActionResult> RegenerateApiKey()
    {
        if (!_tenant.IsResolved)
            return Unauthorized();

        var rawKey = await _apiKeyService.GenerateAsync(_tenant.TenantId);

        return Ok(new
        {
            message = "API key regenerated. Copy this key now — it will not be shown again. Your previous key is now invalid.",
            apiKey = rawKey
        });
    }

    // ── Subscription ──────────────────────────────────────────────

    // GET /api/settings/subscription
    [HttpGet("subscription")]
    public async Task<IActionResult> GetSubscription()
    {
        if (!_tenant.IsResolved)
            return Unauthorized();

        var sub = await _subRepo.GetActiveForTenantAsync(_tenant.TenantId);
        if (sub == null)
            return Ok(null);

        var now = DateTime.UtcNow;
        var docsUsed = await _docRepo.GetMonthlyCountAsync(
            _tenant.TenantId, now.Year, now.Month);

        return Ok(new
        {
            sub.PlanName,
            sub.MonthlyPrice,
            sub.IncludedDocuments,
            sub.ExtraDocumentPrice,
            sub.DiscountPercent,
            sub.StartDate,
            DocumentsUsed = docsUsed,
        });
    }

    // GET /api/settings/plans
    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans()
    {
        var plans = await _planRepo.GetAllActiveAsync();

        return Ok(plans.Select(p => new
        {
            p.SubscriptionPlanId,
            p.Name,
            p.Description,
            p.MonthlyPrice,
            p.IncludedDocuments,
            p.ExtraDocumentPrice,
            p.SortOrder
        }));
    }
}