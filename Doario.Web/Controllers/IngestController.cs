using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Doario.Data.Models.Mail;
using Doario.Data.Repositories;
using Doario.Web.Services;

namespace Doario.Web.Controllers;

[ApiController]
[Route("api/ingest")]
[AllowAnonymous]
public class IngestController : ControllerBase
{
    private readonly ITenantRepository _tenantRepo;
    private readonly IDocumentRepository _documentRepo;
    private readonly IErrorLogRepository _errorLogRepo;
    private readonly OcrService _ocrService;
    private readonly SharePointService _sharePointService;
    private readonly PdfService _pdfService;
    private readonly AiBatchSplitService _aiBatchSplitService;

    public IngestController(
        ITenantRepository tenantRepo,
        IDocumentRepository documentRepo,
        IErrorLogRepository errorLogRepo,
        OcrService ocrService,
        SharePointService sharePointService,
        PdfService pdfService,
        AiBatchSplitService aiBatchSplitService)
    {
        _tenantRepo = tenantRepo;
        _documentRepo = documentRepo;
        _errorLogRepo = errorLogRepo;
        _ocrService = ocrService;
        _sharePointService = sharePointService;
        _pdfService = pdfService;
        _aiBatchSplitService = aiBatchSplitService;
    }

    // GET /api/ingest/health
    [HttpGet("health")]
    public async Task<IActionResult> Health()
    {
        var apiKey = Request.Headers["X-Api-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(apiKey))
            return Unauthorized(new { error = "Missing API key." });

        var tenant = await _tenantRepo.GetByApiKeyAsync(apiKey);
        if (tenant == null)
            return Unauthorized(new { error = "Invalid API key." });

        return Ok(new { status = "ok", tenant = tenant.Name, version = "1.0.0" });
    }

    // POST /api/ingest/scan
    // Single document scan — all pages combined into one PDF, uploaded immediately.
    [HttpPost("scan")]
    public async Task<IActionResult> IngestScan([FromBody] IngestScanRequest request)
    {
        var apiKey = Request.Headers["X-Api-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(apiKey))
            return Unauthorized(new { error = "Missing API key." });

        var tenant = await _tenantRepo.GetByApiKeyAsync(apiKey);
        if (tenant == null)
            return Unauthorized(new { error = "Invalid API key." });

        if (request?.Pages == null || request.Pages.Count == 0)
            return BadRequest(new { error = "No pages received." });

        try
        {
            var fileName = $"scan_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";

            using var pdfStream = _pdfService.BuildPdfStream(request.Pages);
            var sharePointUrl = await _sharePointService.UploadDocumentAsync(
                tenant.TenantId, pdfStream, fileName);

            var document = new Document
            {
                DocumentId = Guid.NewGuid(),
                TenantId = tenant.TenantId,
                OriginalFileName = fileName,
                SharePointUrl = sharePointUrl,
                DocumentStatusId = 1,
                SenderTypeId = tenant.UnknownSenderTypeId,
                SenderId = tenant.UnknownSenderId,
                UploadedByStaffId = tenant.SystemStaffId,
                UploadedAt = DateTime.UtcNow,
            };

            await _documentRepo.CreateAsync(document);
            _ocrService.RunInBackground(document.DocumentId);

            return Ok(new
            {
                documentId = document.DocumentId,
                sharePointUrl = document.SharePointUrl,
                message = "Document received. OCR and AI summary in progress.",
            });
        }
        catch (Exception ex)
        {
            await LogError(tenant.TenantId, ex);
            return StatusCode(500, new { error = $"Failed to process scan: {ex.Message}" });
        }
    }

    // POST /api/ingest/scan-batch
    // ── NEW FLOW ──────────────────────────────────────────────────────────────
    // Receives all scanned pages.
    // AI splits into document boundaries.
    // Returns split preview to portal — NO SharePoint upload, NO DB record yet.
    // Operator reviews, then calls scan-confirm to finalise.
    [HttpPost("scan-batch")]
    public async Task<IActionResult> IngestBatchScan([FromBody] IngestScanRequest request)
    {
        var apiKey = Request.Headers["X-Api-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(apiKey))
            return Unauthorized(new { error = "Missing API key." });

        var tenant = await _tenantRepo.GetByApiKeyAsync(apiKey);
        if (tenant == null)
            return Unauthorized(new { error = "Invalid API key." });

        if (request?.Pages == null || request.Pages.Count == 0)
            return BadRequest(new { error = "No pages received." });

        try
        {
            // ── Step 1: OCR each page for AI boundary detection ───────────────
            // Runs Azure Document Intelligence on each raw PNG image in parallel.
            // Empty string = blank page (OCR returned nothing = blank sheet).
            // Real text = content the AI uses to detect document boundaries.
            // No SharePoint involved — images stay in memory.
            var ocrTasks = request.Pages.Select(p => _ocrService.OcrPageAsync(p));
            var pageTexts = (await Task.WhenAll(ocrTasks)).ToList();

            var boundaries = await _aiBatchSplitService.DetectBoundariesAsync(pageTexts);

            var batchScanId = Guid.NewGuid().ToString();

            var documents = boundaries.Select((b, i) =>
            {
                var docPages = request.Pages
                    .Skip(b.StartPage)
                    .Take(b.PageCount)
                    .ToList();

                return new
                {
                    tempId = Guid.NewGuid().ToString(),  // client-side ID only, no DB
                    index = i,
                    pageStart = b.StartPage + 1,
                    pageEnd = b.StartPage + b.PageCount,
                    pageCount = b.PageCount,
                    pages = docPages,                   // all base64 pages for this doc
                    previewBase64 = docPages.FirstOrDefault(),  // first page as thumbnail
                };
            }).ToList();

            return Ok(new
            {
                batchScanId,
                documentCount = documents.Count,
                documents,
                message = $"{documents.Count} document{(documents.Count != 1 ? "s" : "")} detected. Review and confirm to save.",
            });
        }
        catch (Exception ex)
        {
            await LogError(tenant.TenantId, ex);
            return StatusCode(500, new { error = $"Failed to split batch: {ex.Message}" });
        }
    }

    // POST /api/ingest/scan-confirm
    // ── NEW ENDPOINT ──────────────────────────────────────────────────────────
    // Called when operator clicks Confirm on one or all documents.
    // Receives the confirmed pages for one document.
    // Builds PDF → uploads to SharePoint → creates DB record → fires OCR.
    [HttpPost("scan-confirm")]
    public async Task<IActionResult> IngestScanConfirm([FromBody] IngestConfirmRequest request)
    {
        var apiKey = Request.Headers["X-Api-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(apiKey))
            return Unauthorized(new { error = "Missing API key." });

        var tenant = await _tenantRepo.GetByApiKeyAsync(apiKey);
        if (tenant == null)
            return Unauthorized(new { error = "Invalid API key." });

        if (request?.Pages == null || request.Pages.Count == 0)
            return BadRequest(new { error = "No pages received." });

        try
        {
            var fileName = $"scan_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{request.DocumentIndex + 1}.pdf";

            using var pdfStream = _pdfService.BuildPdfStream(request.Pages);
            var sharePointUrl = await _sharePointService.UploadDocumentAsync(
                tenant.TenantId, pdfStream, fileName);

            var batchScanId = string.IsNullOrWhiteSpace(request.BatchScanId)
                ? (Guid?)null
                : Guid.TryParse(request.BatchScanId, out var g) ? g : (Guid?)null;

            var document = new Document
            {
                DocumentId = Guid.NewGuid(),
                TenantId = tenant.TenantId,
                OriginalFileName = fileName,
                SharePointUrl = sharePointUrl,
                DocumentStatusId = 1,
                SenderTypeId = tenant.UnknownSenderTypeId,
                SenderId = tenant.UnknownSenderId,
                UploadedByStaffId = tenant.SystemStaffId,
                BatchScanId = batchScanId,
                BatchPageStart = request.PageStart,
                BatchPageEnd = request.PageEnd,
                UploadedAt = DateTime.UtcNow,
            };

            await _documentRepo.CreateAsync(document);
            _ocrService.RunInBackground(document.DocumentId);

            return Ok(new
            {
                documentId = document.DocumentId,
                sharePointUrl = document.SharePointUrl,
                message = "Document saved. OCR and AI summary in progress.",
            });
        }
        catch (Exception ex)
        {
            await LogError(tenant.TenantId, ex);
            return StatusCode(500, new { error = $"Failed to confirm document: {ex.Message}" });
        }
    }

    // POST /api/ingest/scan-replace
    // Rescan an individual document — replaces its pages in the preview.
    // No SharePoint involved — returns new pages to portal for re-review.
    // Old document only deleted from SharePoint if it was already confirmed
    // (i.e. has a sharePointUrl passed in the request).
    [HttpPost("scan-replace")]
    public async Task<IActionResult> IngestScanReplace([FromBody] IngestReplaceRequest request)
    {
        var apiKey = Request.Headers["X-Api-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(apiKey))
            return Unauthorized(new { error = "Missing API key." });

        var tenant = await _tenantRepo.GetByApiKeyAsync(apiKey);
        if (tenant == null)
            return Unauthorized(new { error = "Invalid API key." });

        if (request?.Pages == null || request.Pages.Count == 0)
            return BadRequest(new { error = "No pages received." });

        try
        {
            // If this document was already confirmed and uploaded, delete the old file
            if (!string.IsNullOrWhiteSpace(request.OldSharePointUrl))
            {
                await _sharePointService.DeleteFileAsync(
                    tenant.TenantId, request.OldSharePointUrl);
            }

            // If it had a DB record, delete that too
            if (request.OldDocumentId.HasValue && request.OldDocumentId.Value != Guid.Empty)
            {
                await _documentRepo.DeleteAsync(request.OldDocumentId.Value);
            }

            // Return new pages to portal — operator reviews before confirming again
            return Ok(new
            {
                pages = request.Pages,
                previewBase64 = request.Pages[0],
                pageCount = request.Pages.Count,
                message = "Rescan received. Review and confirm to save.",
            });
        }
        catch (Exception ex)
        {
            await LogError(tenant.TenantId, ex);
            return StatusCode(500, new { error = $"Failed to replace scan: {ex.Message}" });
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task LogError(Guid tenantId, Exception ex)
    {
        try
        {
            await _errorLogRepo.AddAsync(new ErrorLog
            {
                ErrorLogId = Guid.NewGuid(),
                TenantId = tenantId,
                ErrorType = "Delivery",
                Message = ex.Message,
                StackTrace = ex.StackTrace,
                CreatedAt = DateTime.UtcNow,
            });
        }
        catch { /* never let logging block the response */ }
    }
}

// ── Request models ────────────────────────────────────────────────

public class IngestScanRequest
{
    public List<string> Pages { get; set; } = new();
}

public class IngestConfirmRequest
{
    public List<string> Pages { get; set; } = new();
    public string BatchScanId { get; set; }
    public int DocumentIndex { get; set; }
    public int? PageStart { get; set; }
    public int? PageEnd { get; set; }
}

public class IngestReplaceRequest
{
    public List<string> Pages { get; set; } = new();
    public Guid? OldDocumentId { get; set; }
    public string OldSharePointUrl { get; set; }
    public Guid? BatchScanId { get; set; }
    public int? BatchPageStart { get; set; }
    public int? BatchPageEnd { get; set; }
}

public class BatchDocumentResult
{
    public Guid DocumentId { get; set; }
    public string SharePointUrl { get; set; }
    public int PageStart { get; set; }
    public int PageEnd { get; set; }
    public string FileName { get; set; }
    public string PreviewBase64 { get; set; }
}

public class DocumentBoundary
{
    public int Index { get; set; }
    public int StartPage { get; set; }
    public int PageCount { get; set; }
}