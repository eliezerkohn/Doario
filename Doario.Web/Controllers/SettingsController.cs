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
    private readonly ITenantAiSettingsRepository _aiSettings;
    private readonly ITenantInboxSettingsRepository _inboxSettings;
    private readonly ITenantMonitoredInboxRepository _monitoredInboxes;
    private readonly ProcessInboxQueue _processInboxQueue;

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
        IExtractionFieldRepository extractionFields,
        ITenantAiSettingsRepository aiSettings,
        ITenantInboxSettingsRepository inboxSettings,
        ITenantMonitoredInboxRepository monitoredInboxes,
        ProcessInboxQueue processInboxQueue)
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
        _aiSettings = aiSettings;
        _inboxSettings = inboxSettings;
        _monitoredInboxes = monitoredInboxes;
        _processInboxQueue = processInboxQueue;
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
        await _inboxSettings.UpdateLastStaffSyncAtAsync(_tenant.TenantId, DateTime.UtcNow);
        return Ok(new
        {
            message = $"Sync complete. {result.Added} staff added, {result.Updated} updated.",
            added = result.Added,
            updated = result.Updated,
            totalPulled = result.TotalPulled,
            lastStaffSyncAt = DateTime.UtcNow,
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
        var plan = sub.SubscriptionPlan;
        var now = DateTime.UtcNow;
        var docsUsed = await _docRepo.GetMonthlyCountAsync(_tenant.TenantId, now.Year, now.Month);
        return Ok(new
        {
            planName = plan?.Name,
            monthlyPrice = plan?.MonthlyPrice,
            includedDocuments = plan?.IncludedDocuments,
            extraDocumentPrice = plan?.ExtraDocumentPrice,
            sub.DiscountPercent,
            sub.StartDate,
            documentsUsed = docsUsed,
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

    [HttpPost("switch-plan")]
    public async Task<IActionResult> SwitchPlan([FromBody] SwitchPlanRequest request)
    {
        if (!_tenant.IsResolved) return Unauthorized();
        var plan = await _planRepo.GetByIdAsync(request.SubscriptionPlanId);
        if (plan == null) return NotFound(new { error = "Plan not found." });
        await _subRepo.SwitchPlanAsync(_tenant.TenantId, plan);
        return Ok(new { message = $"Switched to {plan.Name}.", planName = plan.Name });
    }

    // ── Extraction Fields ─────────────────────────────────────────

    [HttpGet("extraction-fields")]
    public async Task<IActionResult> GetExtractionFields()
    {
        if (!_tenant.IsResolved) return Unauthorized();
        var fields = await _extractionFields.GetAllFieldsAsync(_tenant.TenantId);
        var now = DateTime.UtcNow;
        return Ok(fields.Select(f => new
        {
            f.TenantExtractionFieldId,
            f.FieldName,
            f.FieldDescription,
            f.SortOrder,
            f.StartDate,
            f.EndDate,
            IsActive = f.StartDate <= now && f.EndDate >= now,
        }));
    }

    [HttpPost("extraction-fields")]
    public async Task<IActionResult> AddExtractionField([FromBody] ExtractionFieldRequest request)
    {
        if (!_tenant.IsResolved) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request.FieldName))
            return BadRequest(new { error = "Field name is required." });
        var existing = await _extractionFields.GetAllFieldsAsync(_tenant.TenantId);
        var sortOrder = existing.Any() ? existing.Max(f => f.SortOrder) + 100 : 100;
        var field = new TenantExtractionField
        {
            TenantExtractionFieldId = Guid.NewGuid(),
            TenantId = _tenant.TenantId,
            FieldName = request.FieldName.Trim(),
            FieldDescription = request.FieldDescription?.Trim() ?? string.Empty,
            SortOrder = sortOrder,
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

    [HttpDelete("extraction-fields/{id:guid}")]
    public async Task<IActionResult> DeleteExtractionField(Guid id)
    {
        if (!_tenant.IsResolved) return Unauthorized();
        var field = await _extractionFields.GetByIdAsync(id);
        if (field == null || field.TenantId != _tenant.TenantId) return NotFound();
        await _extractionFields.DeleteFieldAsync(id);
        return Ok(new { message = "Field deactivated." });
    }

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

    // ── AI Assignment ─────────────────────────────────────────────

    [HttpGet("ai-assignment")]
    public async Task<IActionResult> GetAiAssignment()
    {
        if (!_tenant.IsResolved) return Unauthorized();
        var settings = await _aiSettings.GetByTenantAsync(_tenant.TenantId);
        return Ok(new
        {
            mode = settings?.AiAssignmentMode ?? "AutoAssign",
            confidenceThreshold = settings?.AiConfidenceThreshold ?? 8,
        });
    }

    [HttpPut("ai-assignment")]
    public async Task<IActionResult> UpdateAiAssignment([FromBody] AiAssignmentRequest request)
    {
        if (!_tenant.IsResolved) return Unauthorized();
        var validModes = new[] { "Off", "AutoAssign", "SuggestAndApprove" };
        if (!validModes.Contains(request.Mode))
            return BadRequest(new { error = "Invalid mode. Must be Off, AutoAssign, or SuggestAndApprove." });
        var threshold = request.ConfidenceThreshold.HasValue
            ? Math.Clamp(request.ConfidenceThreshold.Value, 0, 10) : 8;
        await _aiSettings.UpsertAsync(_tenant.TenantId, request.Mode, threshold);
        return Ok(new { message = "AI assignment settings updated.", mode = request.Mode, confidenceThreshold = threshold });
    }

    // ── Inbox Settings ────────────────────────────────────────────────

    [HttpGet("inbox")]
    public async Task<IActionResult> GetInboxSettings()
    {
        if (!_tenant.IsResolved) return Unauthorized();
        var settings = await _inboxSettings.GetByTenantAsync(_tenant.TenantId);
        return Ok(new { inboxPollingIntervalSeconds = settings?.InboxPollingIntervalSeconds ?? 60 });
    }

    [HttpPut("inbox")]
    public async Task<IActionResult> UpdateInboxSettings([FromBody] InboxSettingsRequest request)
    {
        if (!_tenant.IsResolved) return Unauthorized();
        var pollingSeconds = Math.Max(10, request.InboxPollingIntervalSeconds);
        await _inboxSettings.UpsertAsync(_tenant.TenantId, pollingSeconds);
        return Ok(new { message = "Inbox settings updated." });
    }

    [HttpGet("staff-sync-schedule")]
    public async Task<IActionResult> GetStaffSyncSchedule()
    {
        if (!_tenant.IsResolved) return Unauthorized();
        var settings = await _inboxSettings.GetByTenantAsync(_tenant.TenantId);
        return Ok(new
        {
            staffSyncIntervalHours = settings?.StaffSyncIntervalHours ?? 24,
            lastStaffSyncAt = settings?.LastStaffSyncAt,
        });
    }

    [HttpPut("staff-sync-schedule")]
    public async Task<IActionResult> UpdateStaffSyncSchedule([FromBody] StaffSyncScheduleRequest request)
    {
        if (!_tenant.IsResolved) return Unauthorized();
        var hours = Math.Max(1, request.StaffSyncIntervalHours);
        await _inboxSettings.UpdateStaffSyncScheduleAsync(_tenant.TenantId, hours);
        return Ok(new { message = "Sync schedule updated." });
    }

    [HttpGet("monitored-inboxes/stats")]
    public async Task<IActionResult> GetInboxStats()
    {
        if (!_tenant.IsResolved) return Unauthorized();
        var inboxes = await _monitoredInboxes.GetAllForTenantAsync(_tenant.TenantId);
        var now = DateTime.UtcNow;
        var stats = inboxes.Select(i => new
        {
            i.TenantMonitoredInboxId,
            i.EmailAddress,
            i.LastProcessedAt,
            i.PollingIntervalSeconds,
            IsActive = i.StartDate <= now && i.EndDate >= now,
            LastFetchCount = DoarioBackgroundService.LastFetchCounts.TryGetValue(
                i.TenantMonitoredInboxId, out var c) ? c : (int?)null,
        });
        return Ok(stats);
    }

    [HttpPost("monitored-inboxes/{id:guid}/process-now")]
    public async Task<IActionResult> ProcessInboxNow(Guid id)
    {
        if (!_tenant.IsResolved) return Unauthorized();
        var inbox = await _monitoredInboxes.GetByIdAsync(id);
        if (inbox == null || inbox.TenantId != _tenant.TenantId) return NotFound();

        var current = _processInboxQueue.GetStatus(_tenant.TenantId.ToString());
        if (current.IsRunning)
            return Ok(new { alreadyRunning = true, message = "Processing already in progress." });

        var started = _processInboxQueue.Start(
            _tenant.TenantId,
            new List<(Guid, string)> { (inbox.TenantMonitoredInboxId, inbox.EmailAddress) });

        return Ok(new { started, message = "Processing triggered." });
    }

    [HttpPost("process-inbox")]
    public async Task<IActionResult> ProcessAllInboxesNow()
    {
        if (!_tenant.IsResolved) return Unauthorized();

        var current = _processInboxQueue.GetStatus(_tenant.TenantId.ToString());
        if (current.IsRunning)
            return Ok(new { alreadyRunning = true, message = "Processing already in progress." });

        var inboxes = await _monitoredInboxes.GetActiveForTenantAsync(_tenant.TenantId);
        if (!inboxes.Any())
            return Ok(new { message = "No active inboxes.", total = 0 });

        var inboxList = inboxes
            .Select(i => (i.TenantMonitoredInboxId, i.EmailAddress))
            .ToList();

        var started = _processInboxQueue.Start(_tenant.TenantId, inboxList);

        return Ok(new
        {
            started,
            total = inboxList.Count,
            message = $"Started processing {inboxList.Count} inboxes in background."
        });
    }

    [HttpGet("process-inbox-status")]
    public IActionResult GetProcessInboxStatus()
    {
        if (!_tenant.IsResolved) return Unauthorized();
        var status = _processInboxQueue.GetStatus(_tenant.TenantId.ToString());
        return Ok(status);
    }

    // ── Monitored Inboxes ─────────────────────────────────────────────

    [HttpGet("monitored-inboxes")]
    public async Task<IActionResult> GetMonitoredInboxes()
    {
        if (!_tenant.IsResolved) return Unauthorized();
        var inboxes = await _monitoredInboxes.GetAllForTenantAsync(_tenant.TenantId);
        var now = DateTime.UtcNow;
        return Ok(inboxes.Select(i => new
        {
            i.TenantMonitoredInboxId,
            i.EmailAddress,
            i.Description,
            i.IsFaxInbox,
            i.PollingIntervalSeconds,
            i.LastProcessedAt,
            i.StartDate,
            i.EndDate,
            IsActive = i.StartDate <= now && i.EndDate >= now,
        }));
    }

    [HttpPost("monitored-inboxes")]
    public async Task<IActionResult> AddMonitoredInbox([FromBody] MonitoredInboxRequest request)
    {
        if (!_tenant.IsResolved) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request.EmailAddress))
            return BadRequest(new { error = "Email address is required." });
        var inbox = new Doario.Data.Models.SaaS.TenantMonitoredInbox
        {
            TenantMonitoredInboxId = Guid.NewGuid(),
            TenantId = _tenant.TenantId,
            EmailAddress = request.EmailAddress.Trim().ToLowerInvariant(),
            Description = request.Description?.Trim() ?? string.Empty,
            IsFaxInbox = request.IsFaxInbox,
            PollingIntervalSeconds = Math.Max(10, request.PollingIntervalSeconds),
            LastProcessedAt = DateTime.UtcNow,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.MaxValue,
        };
        await _monitoredInboxes.AddAsync(inbox);
        return Ok(new
        {
            inbox.TenantMonitoredInboxId,
            inbox.EmailAddress,
            inbox.Description,
            inbox.IsFaxInbox,
            inbox.PollingIntervalSeconds,
            inbox.StartDate,
            inbox.EndDate,
            IsActive = true,
        });
    }

    [HttpPut("monitored-inboxes/{id:guid}")]
    public async Task<IActionResult> UpdateMonitoredInbox(Guid id, [FromBody] MonitoredInboxRequest request)
    {
        if (!_tenant.IsResolved) return Unauthorized();
        var inbox = await _monitoredInboxes.GetByIdAsync(id);
        if (inbox == null || inbox.TenantId != _tenant.TenantId) return NotFound();
        if (!string.IsNullOrWhiteSpace(request.EmailAddress))
            inbox.EmailAddress = request.EmailAddress.Trim().ToLowerInvariant();
        inbox.Description = request.Description?.Trim() ?? string.Empty;
        inbox.IsFaxInbox = request.IsFaxInbox;
        inbox.PollingIntervalSeconds = Math.Max(10, request.PollingIntervalSeconds);
        await _monitoredInboxes.SaveAsync();
        var now = DateTime.UtcNow;
        return Ok(new
        {
            inbox.TenantMonitoredInboxId,
            inbox.EmailAddress,
            inbox.Description,
            inbox.IsFaxInbox,
            inbox.PollingIntervalSeconds,
            inbox.StartDate,
            inbox.EndDate,
            IsActive = inbox.StartDate <= now && inbox.EndDate >= now,
        });
    }

    [HttpDelete("monitored-inboxes/{id:guid}")]
    public async Task<IActionResult> DeactivateMonitoredInbox(Guid id)
    {
        if (!_tenant.IsResolved) return Unauthorized();
        var inbox = await _monitoredInboxes.GetByIdAsync(id);
        if (inbox == null || inbox.TenantId != _tenant.TenantId) return NotFound();
        await _monitoredInboxes.DeactivateAsync(id);
        return Ok(new { message = "Inbox deactivated." });
    }

    [HttpPost("monitored-inboxes/{id:guid}/restore")]
    public async Task<IActionResult> RestoreMonitoredInbox(Guid id)
    {
        if (!_tenant.IsResolved) return Unauthorized();
        var inbox = await _monitoredInboxes.GetByIdAsync(id);
        if (inbox == null || inbox.TenantId != _tenant.TenantId) return NotFound();
        await _monitoredInboxes.RestoreAsync(id);
        return Ok(new { message = "Inbox restored." });
    }

    // ── Request DTOs ──────────────────────────────────────────────

    public class SwitchPlanRequest { public Guid SubscriptionPlanId { get; set; } }
    public class StaffSyncScheduleRequest { public int StaffSyncIntervalHours { get; set; } = 24; }
    public class MonitoredInboxRequest
    {
        public string EmailAddress { get; set; }
        public string Description { get; set; }
        public bool IsFaxInbox { get; set; }
        public int PollingIntervalSeconds { get; set; } = 60;
    }
    public class InboxSettingsRequest { public int InboxPollingIntervalSeconds { get; set; } = 60; }
    public class AiAssignmentRequest
    {
        public string Mode { get; set; }
        public int? ConfidenceThreshold { get; set; }
    }
    public class ExtractionFieldRequest
    {
        public string FieldName { get; set; }
        public string FieldDescription { get; set; }
        public DateTime? EndDate { get; set; }
    }
}