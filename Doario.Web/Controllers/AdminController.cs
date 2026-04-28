using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Doario.Data.Repositories;
using Doario.Web.Services;
using System.Security.Claims;

namespace Doario.Web.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "DoarioAdmin")]
public class AdminController : ControllerBase
{
    private readonly IDocumentRepository _documents;
    private readonly IDocumentViewedRepository _viewed;
    private readonly IStaffRepository _staff;
    private readonly TenantContext _tenant;
    private readonly StaffSyncService _staffSync;
    private readonly ITenantRepository _tenantRepo;
    private readonly StaffCsvService _staffCsvService;

    public AdminController(
        IDocumentRepository documents,
        IDocumentViewedRepository viewed,
        IStaffRepository staff,
        TenantContext tenant,
        StaffSyncService staffSync,
        ITenantRepository tenantRepo,
        StaffCsvService staffCsvService)
    {
        _documents = documents;
        _viewed = viewed;
        _staff = staff;
        _tenant = tenant;
        _staffSync = staffSync;
        _tenantRepo = tenantRepo;
        _staffCsvService = staffCsvService;
    }

    // GET /api/admin/queue?page=1&pageSize=50
    // Returns isViewed per document based on DocumentViewed table
    [HttpGet("queue")]
    public async Task<IActionResult> GetQueue(int page = 1, int pageSize = 50)
    {
        if (!_tenant.IsResolved)
            return Unauthorized();

        pageSize = Math.Min(pageSize, 500);

        var docs = await _documents.GetQueueAsync(_tenant.TenantId, page, pageSize);
        var viewedIds = await _viewed.GetViewedDocumentIdsAsync(_tenant.TenantId);

        var result = docs.Select(d => new
        {
            d.DocumentId,
            d.UploadedAt,
            d.OcrText,
            StatusId = d.DocumentStatusId,
            StatusName = d.DocumentStatus.Name,
            d.AiSummary,
            d.OriginalFileName,
            d.SharePointUrl,
            d.SenderDisplayName,
            d.SenderEmail,
            IsViewed = viewedIds.Contains(d.DocumentId)
        });

        return Ok(result);
    }

    // POST /api/admin/mark-viewed
    // Called when admin clicks a document — marks it as read for the whole tenant
    [HttpPost("mark-viewed")]
    public async Task<IActionResult> MarkViewed([FromBody] MarkViewedRequest request)
    {
        if (!_tenant.IsResolved)
            return Unauthorized();

        var email = User.FindFirst(ClaimTypes.Email)?.Value
                     ?? User.FindFirst("preferred_username")?.Value;
        var adminStaff = await _staff.GetByEmailAsync(email, _tenant.TenantId);
        if (adminStaff is null)
            return Unauthorized();

        await _viewed.MarkViewedAsync(request.DocumentId, _tenant.TenantId, adminStaff.ImportedStaffId);

        return Ok();
    }

    // POST /api/admin/mark-unread
    // Called when admin clicks "Mark as unread" — removes the viewed record for everyone
    [HttpPost("mark-unread")]
    public async Task<IActionResult> MarkUnread([FromBody] MarkViewedRequest request)
    {
        if (!_tenant.IsResolved)
            return Unauthorized();

        await _viewed.MarkUnreadAsync(request.DocumentId, _tenant.TenantId);

        return Ok();
    }

    // POST /api/admin/sync-staff
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

    // POST /api/admin/import-staff-csv
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
}

public class MarkViewedRequest
{
    public Guid DocumentId { get; set; }
}