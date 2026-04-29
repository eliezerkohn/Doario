using Doario.Data.Models.Mail;
using Doario.Data.Repositories;
using Doario.Web.Models;
using Doario.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    private readonly IExtractionFieldRepository _extractionFields;

    public SettingsController(
        ITenantRepository tenantRepo,
        ISubscriptionRepository subRepo,
        ISubscriptionPlanRepository planRepo,
        IDocumentRepository docRepo,
        IStaffRepository staffRepo,
        StaffSyncService staffSync,
        StaffCsvService staffCsvService,
        ApiKeyService apiKeyService,
        TenantContext tenant,
        IExtractionFieldRepository extractionFields)
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
        _extractionFields = extractionFields;
    }

    // ── Organisation ─────────────────────────────────────────────

    [HttpGet("organisation")]
    public async Task<IActionResult> GetOrganisation()
    {
        if (!_tenant.IsResolved) return Unauthorized();

        var tenant = await _tenantRepo.GetByIdAsync(_tenant.TenantId);
        if (tenant == null) return NotFound();

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

    [HttpPut("organisation")]
    public async Task<IActionResult> UpdateOrganisation([FromBody] UpdateOrganisationRequest request)
    {
        if (!_tenant.IsResolved) return Unauthorized();

        var tenant = await _tenantRepo.GetByIdAsync(_tenant.TenantId);
        if (tenant == null) return NotFound();

        tenant.Name = request.Name?.Trim() ?? tenant.Name;
        tenant.MailboxAddress = request.MailboxAddress?.Trim() ?? tenant.MailboxAddress;
        tenant.SharePointSiteUrl = request.SharePointSiteUrl?.Trim() ?? tenant.SharePointSiteUrl;

        await _tenantRepo.SaveAsync();

        return Ok(new { message = "Organisation updated successfully." });
    }

    // ── Staff ─────────────────────────────────────────────────────

    [HttpPost("sync-staff")]
    public async Task<IActionResult> SyncStaff()
    {
        if (!_tenant.IsResolved) return Unauthorized();

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

    [HttpPost("import-staff-csv")]
    public async Task<IActionResult> ImportStaffCsv(IFormFile file)
    {
        if (!_tenant.IsResolved) return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded." });

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "File must be a .csv" });

        var tenant = await _tenantRepo.GetByIdAsync(_tenant.TenantId);
        if (tenant == null)
            return BadRequest(new { message = "Tenant not found." });

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

    [HttpGet("api-key")]
    public async Task<IActionResult> GetApiKey()
    {
        if (!_tenant.IsResolved) return Unauthorized();

        var prefix = await _apiKeyService.GetPrefixAsync(_tenant.TenantId);

        return Ok(new
        {
            hasKey = !string.IsNullOrWhiteSpace(prefix),
            prefix = string.IsNullOrWhiteSpace(prefix) ? null : prefix + "..."
        });
    }

    [HttpPost("generate-api-key")]
    public async Task<IActionResult> GenerateApiKey()
    {
        if (!_tenant.IsResolved) return Unauthorized();

        var rawKey = await _apiKeyService.GenerateAsync(_tenant.TenantId);

        return Ok(new
        {
            message = "API key generated. Copy this key now — it will not be shown again.",
            apiKey = rawKey
        });
    }

    [HttpPost("regenerate-api-key")]
    public async Task<IActionResult> RegenerateApiKey()
    {
        if (!_tenant.IsResolved) return Unauthorized();

        var rawKey = await _apiKeyService.GenerateAsync(_tenant.TenantId);

        return Ok(new
        {
            message = "API key regenerated. Copy this key now — it will not be shown again. Your previous key is now invalid.",
            apiKey = rawKey
        });
    }

    // ── Subscription ──────────────────────────────────────────────

    [HttpGet("subscription")]
    public async Task<IActionResult> GetSubscription()
    {
        if (!_tenant.IsResolved) return Unauthorized();

        var sub = await _subRepo.GetActiveForTenantAsync(_tenant.TenantId);
        if (sub == null) return Ok(null);

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

    // ── Extraction Fields ─────────────────────────────────────────

    // GET /api/settings/extraction-fields
    // Returns all extraction fields for the tenant (active and expired)
    [HttpGet("extraction-fields")]
    public async Task<IActionResult> GetExtractionFields()
    {
        if (!_tenant.IsResolved) return Unauthorized();

        var fields = await _extractionFields.GetAllFieldsAsync(_tenant.TenantId);

        var now = DateTime.UtcNow;
        var result = fields.Select(f => new
        {
            f.TenantExtractionFieldId,
            f.FieldName,
            f.FieldDescription,
            f.SortOrder,
            f.StartDate,
            f.EndDate,
            IsActive = f.StartDate <= now && f.EndDate >= now,
        });

        return Ok(result);
    }

    // POST /api/settings/extraction-fields
    // Creates a new extraction field
    [HttpPost("extraction-fields")]
    public async Task<IActionResult> AddExtractionField([FromBody] ExtractionFieldRequest request)
    {
        if (!_tenant.IsResolved) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.FieldName))
            return BadRequest(new { error = "Field name is required." });

        // SortOrder: place at end — max existing + 100
        var existing = await _extractionFields.GetAllFieldsAsync(_tenant.TenantId);
        var sortOrder = existing.Any() ? existing.Max(f => f.SortOrder) + 100 : 100;

        var field = new TenantExtractionField
        {
            TenantExtractionFieldId = Guid.NewGuid(),
            TenantId = _tenant.TenantId,
            FieldName = request.FieldName.Trim(),
            FieldDescription = request.FieldDescription?.Trim() ?? string.Empty,
            SortOrder = sortOrder,
            // StartDate and EndDate defaulted by model constructor
        };

        await _extractionFields.AddFieldAsync(field);

        return Ok(new
        {
            field.TenantExtractionFieldId,
            field.FieldName,
            field.FieldDescription,
            field.SortOrder,
            field.StartDate,
            field.EndDate,
            IsActive = true,
        });
    }

    // PUT /api/settings/extraction-fields/{id}
    // Updates field name, description, and end date
    [HttpPut("extraction-fields/{id:guid}")]
    public async Task<IActionResult> UpdateExtractionField(Guid id, [FromBody] ExtractionFieldRequest request)
    {
        if (!_tenant.IsResolved) return Unauthorized();

        var field = await _extractionFields.GetByIdAsync(id);
        if (field == null || field.TenantId != _tenant.TenantId) return NotFound();

        if (!string.IsNullOrWhiteSpace(request.FieldName))
            field.FieldName = request.FieldName.Trim();

        field.FieldDescription = request.FieldDescription?.Trim() ?? string.Empty;

        if (request.EndDate.HasValue)
            field.EndDate = request.EndDate.Value;

        await _extractionFields.UpdateFieldAsync(field);

        var now = DateTime.UtcNow;
        return Ok(new
        {
            field.TenantExtractionFieldId,
            field.FieldName,
            field.FieldDescription,
            field.SortOrder,
            field.StartDate,
            field.EndDate,
            IsActive = field.StartDate <= now && field.EndDate >= now,
        });
    }

    // DELETE /api/settings/extraction-fields/{id}
    // Permanently removes a field
    [HttpDelete("extraction-fields/{id:guid}")]
    public async Task<IActionResult> DeleteExtractionField(Guid id)
    {
        if (!_tenant.IsResolved) return Unauthorized();

        var field = await _extractionFields.GetByIdAsync(id);
        if (field == null || field.TenantId != _tenant.TenantId) return NotFound();

        await _extractionFields.DeleteFieldAsync(id);

        return Ok(new { message = "Field deactivated." });
    }


    // POST /api/settings/extraction-fields/{id}/restore
    // Restores a soft-deleted field by setting EndDate back to DateTime.MaxValue
    [HttpPost("extraction-fields/{id:guid}/restore")]
    public async Task<IActionResult> RestoreExtractionField(Guid id)
    {
        if (!_tenant.IsResolved) return Unauthorized();

        var field = await _extractionFields.GetByIdAsync(id);
        if (field == null || field.TenantId != _tenant.TenantId) return NotFound();

        field.EndDate = DateTime.MaxValue;
        await _extractionFields.UpdateFieldAsync(field);

        return Ok(new
        {
            field.TenantExtractionFieldId,
            field.FieldName,
            field.FieldDescription,
            field.SortOrder,
            field.StartDate,
            field.EndDate,
            IsActive = true,
        });
    }

    public class ExtractionFieldRequest
    {
        public string FieldName { get; set; }
        public string FieldDescription { get; set; }
        public DateTime? EndDate { get; set; }
    }
}