using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Doario.Data;
using Doario.Data.Repositories;
using Doario.Web.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;

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
    private readonly DoarioDataContext _db;

    public AdminController(
        IDocumentRepository documents,
        IDocumentViewedRepository viewed,
        IStaffRepository staff,
        TenantContext tenant,
        StaffSyncService staffSync,
        ITenantRepository tenantRepo,
        StaffCsvService staffCsvService,
        IDocumentCheckRepository checkRepo,
        DoarioDataContext db)
    {
        _documents = documents;
        _viewed = viewed;
        _staff = staff;
        _tenant = tenant;
        _staffSync = staffSync;
        _tenantRepo = tenantRepo;
        _staffCsvService = staffCsvService;
        _checkRepo = checkRepo;
        _db = db;
    }

    // GET /api/admin/counts
    [HttpGet("counts")]
    public async Task<IActionResult> GetCounts()
    {
        if (!_tenant.IsResolved) return Unauthorized();

        var tenantId = _tenant.TenantId;

        var statusCounts = await _db.Documents
            .Where(d => d.TenantId == tenantId)
            .GroupBy(d => d.DocumentStatusId)
            .Select(g => new { StatusId = g.Key, Count = g.Count() })
            .ToListAsync();

        var unviewedInbox = await _db.Documents
            .Where(d => d.TenantId == tenantId
                     && (d.DocumentStatusId == 1 || d.DocumentStatusId == 2)
                     && !_db.DocumentVieweds.Any(v => v.DocumentId == d.DocumentId && v.TenantId == tenantId))
            .CountAsync();

        var checksCount = await _checkRepo.GetDocumentIdsWithChecksAsync(tenantId);
        var dict = statusCounts.ToDictionary(x => x.StatusId, x => x.Count);

        return Ok(new
        {
            Inbox = unviewedInbox,
            Unassigned = dict.GetValueOrDefault(1, 0),
            Assigned = dict.GetValueOrDefault(2, 0),
            Actioned = dict.GetValueOrDefault(4, 0),
            Spam = dict.GetValueOrDefault(7, 0),
            Promotions = dict.GetValueOrDefault(8, 0),
            Trash = dict.GetValueOrDefault(9, 0),
            Checks = checksCount.Count,
        });
    }

    // GET /api/admin/queue
    // Direct DB projection — no Include, no navigation property loading
    [HttpGet("queue")]
    public async Task<IActionResult> GetQueue(int page = 1, int pageSize = 50, string statusIds = null)
    {
        if (!_tenant.IsResolved) return Unauthorized();

        pageSize = Math.Min(pageSize, 100);
        var tenantId = _tenant.TenantId;

        int[] ids = null;
        if (!string.IsNullOrEmpty(statusIds))
        {
            ids = statusIds.Split(',')
                .Select(s => int.TryParse(s.Trim(), out var n) ? n : -1)
                .Where(n => n > 0)
                .ToArray();
        }

        // Project directly in SQL — no entity loading, no navigation joins
        var docs = await _db.Documents
            .Where(d => d.TenantId == tenantId)
            .Where(d => ids == null || ids.Contains(d.DocumentStatusId))
            .OrderByDescending(d => d.UploadedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new
            {
                d.DocumentId,
                d.UploadedAt,
                StatusId = d.DocumentStatusId,
                StatusName = d.DocumentStatus.Name,
                d.OriginalFileName,
                d.SharePointUrl,
                SenderDisplayName = d.Sender != null ? d.Sender.DisplayName : string.Empty,
                SenderEmail = d.Sender != null ? d.Sender.Email : string.Empty,
                d.AiSummary,
            })
            .ToListAsync();

        // Scoped queries — only for documents in this page
        var docIds = docs.Select(d => d.DocumentId).ToList();
        var viewedIds = await _viewed.GetViewedDocumentIdsAsync(tenantId, docIds);
        var checkIds = await _checkRepo.GetDocumentIdsWithChecksAsync(tenantId);

        var result = docs.Select(d =>
        {
            var snippet = string.IsNullOrEmpty(d.AiSummary)
                ? null
                : Regex.Replace(d.AiSummary, "<[^>]*>", " ")
                    .Replace("  ", " ").Trim();
            if (snippet != null && snippet.Length > 100)
                snippet = snippet.Substring(0, 100);

            return new
            {
                d.DocumentId,
                d.UploadedAt,
                d.StatusId,
                d.StatusName,
                d.OriginalFileName,
                d.SharePointUrl,
                d.SenderDisplayName,
                d.SenderEmail,
                IsViewed = viewedIds.Contains(d.DocumentId),
                IsCheck = checkIds.Contains(d.DocumentId),
                AiSummarySnippet = snippet,
            };
        });

        return Ok(result);
    }

    // GET /api/admin/document/{documentId}
    [HttpGet("document/{documentId:guid}")]
    public async Task<IActionResult> GetDocument(Guid documentId)
    {
        if (!_tenant.IsResolved) return Unauthorized();

        var doc = await _documents.GetByIdWithTenantAsync(documentId, _tenant.TenantId);
        if (doc is null) return NotFound();

        var checkDocIds = await _checkRepo.GetDocumentIdsWithChecksAsync(_tenant.TenantId);

        return Ok(new
        {
            doc.DocumentId,
            doc.UploadedAt,
            doc.OcrText,
            doc.AiSummary,
            StatusId = doc.DocumentStatusId,
            StatusName = doc.DocumentStatus?.Name,
            doc.OriginalFileName,
            doc.SharePointUrl,
            SenderDisplayName = doc.Sender != null ? doc.Sender.DisplayName : string.Empty,
            SenderEmail = doc.Sender != null ? doc.Sender.Email : string.Empty,
            IsCheck = checkDocIds.Contains(doc.DocumentId),
        });
    }

    // GET /api/admin/check/{documentId}
    [HttpGet("check/{documentId:guid}")]
    public async Task<IActionResult> GetCheck(Guid documentId)
    {
        if (!_tenant.IsResolved) return Unauthorized();

        var check = await _checkRepo.GetByDocumentIdAsync(documentId);
        if (check == null) return Ok(null);

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
    [HttpGet("checks")]
    public async Task<IActionResult> GetAllChecks()
    {
        if (!_tenant.IsResolved) return Unauthorized();

        var checks = await _checkRepo.GetAllForTenantAsync(_tenant.TenantId);

        return Ok(checks.Select(c => new
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
        }));
    }

    // GET /api/admin/senders
    [HttpGet("senders")]
    public async Task<IActionResult> GetSenders()
    {
        if (!_tenant.IsResolved) return Unauthorized();

        var senders = await _documents.GetDistinctSendersAsync(_tenant.TenantId);

        return Ok(senders.Select(s => new
        {
            s.DisplayName,
            s.Email,
            s.DocumentCount
        }));
    }

    // GET /api/admin/by-sender
    [HttpGet("by-sender")]
    public async Task<IActionResult> GetBySender([FromQuery] string q)
    {
        if (!_tenant.IsResolved) return Unauthorized();

        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = "Search term is required." });

        var docs = await _documents.GetBySenderAsync(_tenant.TenantId, q.Trim());
        var checkDocIds = await _checkRepo.GetDocumentIdsWithChecksAsync(_tenant.TenantId);

        return Ok(docs.Select(d => new
        {
            d.DocumentId,
            d.UploadedAt,
            d.OriginalFileName,
            d.SharePointUrl,
            SenderDisplayName = d.Sender != null ? d.Sender.DisplayName : string.Empty,
            SenderEmail = d.Sender != null ? d.Sender.Email : string.Empty,
            StatusId = d.DocumentStatusId,
            StatusName = d.DocumentStatus.Name,
            IsCheck = checkDocIds.Contains(d.DocumentId),
        }));
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
        await _checkRepo.DeleteByDocumentIdAsync(documentId);
        await _documents.DeleteAsync(documentId);
        return Ok(new { message = "Document permanently deleted." });
    }

    // POST /api/admin/mark-viewed
    [HttpPost("mark-viewed")]
    public async Task<IActionResult> MarkViewed([FromBody] MarkViewedRequest request)
    {
        if (!_tenant.IsResolved) return Unauthorized();
        var email = User.FindFirst(ClaimTypes.Email)?.Value
                     ?? User.FindFirst("preferred_username")?.Value;
        var adminStaff = await _staff.GetByEmailAsync(email, _tenant.TenantId);
        if (adminStaff is null) return Unauthorized();
        await _viewed.MarkViewedAsync(request.DocumentId, _tenant.TenantId, adminStaff.ImportedStaffId);
        return Ok();
    }

    // POST /api/admin/mark-unread
    [HttpPost("mark-unread")]
    public async Task<IActionResult> MarkUnread([FromBody] MarkViewedRequest request)
    {
        if (!_tenant.IsResolved) return Unauthorized();
        await _viewed.MarkUnreadAsync(request.DocumentId, _tenant.TenantId);
        return Ok();
    }

    // POST /api/admin/sync-staff
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

    // POST /api/admin/import-staff-csv
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

    // POST /api/admin/requeue-stuck
    [HttpPost("requeue-stuck")]
    public async Task<IActionResult> RequeueStuck([FromServices] AiProcessingQueue aiQueue)
    {
        if (!_tenant.IsResolved) return Unauthorized();

        var stuck = await _documents.GetStuckDocumentsAsync(_tenant.TenantId);
        aiQueue.EnqueueBatch(stuck.Select(d => d.DocumentId));

        return Ok(new
        {
            message = $"Requeued {stuck.Count} documents for AI processing.",
            count = stuck.Count
        });
    }
}

public class MarkViewedRequest { public Guid DocumentId { get; set; } }
public class DocumentActionRequest { public Guid DocumentId { get; set; } }