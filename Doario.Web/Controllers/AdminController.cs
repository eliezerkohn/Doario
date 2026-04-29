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
    private readonly IDocumentCheckRepository _checkRepo;

    public AdminController(
        IDocumentRepository documents,
        IDocumentViewedRepository viewed,
        IStaffRepository staff,
        TenantContext tenant,
        StaffSyncService staffSync,
        ITenantRepository tenantRepo,
        StaffCsvService staffCsvService,
        IDocumentCheckRepository checkRepo)
    {
        _documents = documents;
        _viewed = viewed;
        _staff = staff;
        _tenant = tenant;
        _staffSync = staffSync;
        _tenantRepo = tenantRepo;
        _staffCsvService = staffCsvService;
        _checkRepo = checkRepo;
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

        // Load check IDs for this tenant to mark isCheck on each document
        var checkDocIds = await _checkRepo.GetDocumentIdsWithChecksAsync(_tenant.TenantId);

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
            SenderDisplayName = d.Sender != null ? d.Sender.DisplayName : string.Empty,
            SenderEmail = d.Sender != null ? d.Sender.Email : string.Empty,
            IsViewed = viewedIds.Contains(d.DocumentId),
            IsCheck = checkDocIds.Contains(d.DocumentId),
        });

        return Ok(result);
    }

    // GET /api/admin/check/{documentId}
    // Returns check data for a single document. 404 if not a check.
    [HttpGet("check/{documentId:guid}")]
    public async Task<IActionResult> GetCheck(Guid documentId)
    {
        if (!_tenant.IsResolved) return Unauthorized();

        var check = await _checkRepo.GetByDocumentIdAsync(documentId);
        if (check == null) return NotFound();

        return Ok(new
        {
            check.DocumentCheckId,
            check.DocumentId,
            check.CheckAmount,
            check.CheckPayerName,
            check.CheckNumber,
            check.CreatedAt,
        });
    }

    // GET /api/admin/checks
    // Returns all check documents for this tenant — used by ChecksSearch panel
    [HttpGet("checks")]
    public async Task<IActionResult> GetAllChecks()
    {
        if (!_tenant.IsResolved) return Unauthorized();

        var checks = await _checkRepo.GetAllForTenantAsync(_tenant.TenantId);

        var result = checks.Select(c => new
        {
            c.DocumentId,
            c.CheckAmount,
            c.CheckPayerName,
            c.CheckNumber,
            c.CreatedAt,
            c.OriginalFileName,
            c.AiSummary,
            c.UploadedAt,
            c.SenderDisplayName,
            IsCheck = true,
        });

        return Ok(result);
    }

    // GET /api/admin/senders
    [HttpGet("senders")]
    public async Task<IActionResult> GetSenders()
    {
        if (!_tenant.IsResolved)
            return Unauthorized();

        var senders = await _documents.GetDistinctSendersAsync(_tenant.TenantId);

        var result = senders.Select(s => new
        {
            s.DisplayName,
            s.Email,
            s.DocumentCount
        });

        return Ok(result);
    }

    // GET /api/admin/by-sender?q=medline
    [HttpGet("by-sender")]
    public async Task<IActionResult> GetBySender([FromQuery] string q)
    {
        if (!_tenant.IsResolved)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = "Search term is required." });

        var docs = await _documents.GetBySenderAsync(_tenant.TenantId, q.Trim());

        var result = docs.Select(d => new
        {
            d.DocumentId,
            d.UploadedAt,
            d.OriginalFileName,
            d.SharePointUrl,
            SenderDisplayName = d.Sender != null ? d.Sender.DisplayName : string.Empty,
            SenderEmail = d.Sender != null ? d.Sender.Email : string.Empty,
            d.AiSummary,
            d.OcrText,
            StatusId = d.DocumentStatusId,
            StatusName = d.DocumentStatus.Name,
        });

        return Ok(result);
    }

    // POST /api/admin/trash
    [HttpPost("trash")]
    public async Task<IActionResult> TrashDocument([FromBody] DocumentActionRequest request)
    {
        if (!_tenant.IsResolved) return Unauthorized();

        var doc = await _documents.GetByIdAsync(request.DocumentId, _tenant.TenantId);
        if (doc is null) return NotFound();

        await _documents.UpdateStatusAsync(request.DocumentId, 9);
        return Ok(new { message = "Document moved to Trash." });
    }

    // POST /api/admin/restore
    [HttpPost("restore")]
    public async Task<IActionResult> RestoreDocument([FromBody] DocumentActionRequest request)
    {
        if (!_tenant.IsResolved) return Unauthorized();

        var doc = await _documents.GetByIdAsync(request.DocumentId, _tenant.TenantId);
        if (doc is null) return NotFound();

        await _documents.UpdateStatusAsync(request.DocumentId, 1);
        return Ok(new { message = "Document restored to Inbox." });
    }

    // DELETE /api/admin/delete/{documentId}
    [HttpDelete("delete/{documentId:guid}")]
    public async Task<IActionResult> DeleteForever(Guid documentId,
        [FromServices] SharePointService sharePointService)
    {
        if (!_tenant.IsResolved) return Unauthorized();

        var doc = await _documents.GetByIdAsync(documentId, _tenant.TenantId);
        if (doc is null) return NotFound();

        if (doc.DocumentStatusId != 9)
            return BadRequest(new { error = "Document must be in Trash before permanent deletion." });

        if (!string.IsNullOrWhiteSpace(doc.SharePointUrl))
            await sharePointService.DeleteFileAsync(_tenant.TenantId, doc.SharePointUrl);

        // Delete check row first if exists (FK restrict would block otherwise)
        await _checkRepo.DeleteByDocumentIdAsync(documentId);

        await _documents.DeleteAsync(documentId);

        return Ok(new { message = "Document permanently deleted." });
    }

    // POST /api/admin/mark-viewed
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

public class DocumentActionRequest
{
    public Guid DocumentId { get; set; }
}