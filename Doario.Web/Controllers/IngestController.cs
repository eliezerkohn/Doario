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
    private readonly ScanConfirmQueue _scanConfirmQueue;

    public IngestController(
        ITenantRepository tenantRepo,
        IDocumentRepository documentRepo,
        IErrorLogRepository errorLogRepo,
        OcrService ocrService,
        SharePointService sharePointService,
        PdfService pdfService,
        AiBatchSplitService aiBatchSplitService,
        ScanConfirmQueue scanConfirmQueue)
    {
        _tenantRepo = tenantRepo;
        _documentRepo = documentRepo;
        _errorLogRepo = errorLogRepo;
        _ocrService = ocrService;
        _sharePointService = sharePointService;
        _pdfService = pdfService;
        _aiBatchSplitService = aiBatchSplitService;
        _scanConfirmQueue = scanConfirmQueue;
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
    // Receives all scanned pages, AI splits into document boundaries.
    // Returns split preview — NO SharePoint upload yet.
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
            // OCR pages one at a time to avoid OutOfMemoryException
            // Sequential is safer — parallel OCR holds all images in memory simultaneously
            var pageTexts = new List<string>();
            foreach (var page in request.Pages)
            {
                var text = string.IsNullOrWhiteSpace(page)
                    ? string.Empty
                    : await _ocrService.OcrPageAsync(page);
                pageTexts.Add(text ?? string.Empty);
            }

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
                    tempId = Guid.NewGuid().ToString(),
                    index = i,
                    pageStart = b.StartPage + 1,
                    pageEnd = b.StartPage + b.PageCount,
                    pageCount = b.PageCount,
                    pages = docPages,
                    previewBase64 = docPages.FirstOrDefault(),
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
    // Legacy single-document confirm — kept for backwards compatibility.
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

    // POST /api/ingest/scan-confirm-batch
    [HttpPost("scan-confirm-batch")]
    public async Task<IActionResult> IngestScanConfirmBatch([FromBody] ScanConfirmBatchRequest request)
    {
        var apiKey = Request.Headers["X-Api-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(apiKey))
            return Unauthorized(new { error = "Missing API key." });

        var tenant = await _tenantRepo.GetByApiKeyAsync(apiKey);
        if (tenant == null)
            return Unauthorized(new { error = "Invalid API key." });

        if (request?.Documents == null || request.Documents.Count == 0)
            return BadRequest(new { error = "No documents received." });

        var current = _scanConfirmQueue.GetStatus(tenant.TenantId);
        if (current.IsRunning)
            return Ok(new { alreadyRunning = true, message = "Confirmation already in progress." });

        var started = _scanConfirmQueue.Start(tenant.TenantId, apiKey, request.Documents);

        return Ok(new
        {
            started,
            total = request.Documents.Count,
            message = $"Started confirming {request.Documents.Count} documents in background.",
        });
    }

    // GET /api/ingest/scan-confirm-status
    [HttpGet("scan-confirm-status")]
    public async Task<IActionResult> GetScanConfirmStatus()
    {
        var apiKey = Request.Headers["X-Api-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(apiKey))
            return Unauthorized(new { error = "Missing API key." });

        var tenant = await _tenantRepo.GetByApiKeyAsync(apiKey);
        if (tenant == null)
            return Unauthorized(new { error = "Invalid API key." });

        var status = _scanConfirmQueue.GetStatus(tenant.TenantId);

        return Ok(new
        {
            status.IsRunning,
            status.StartedAt,
            documents = status.Documents.Select(d => new
            {
                d.TempId,
                d.Index,
                State = d.State.ToString(),
                d.DocumentId,
                d.SharePointUrl,
                d.Error,
            }),
        });
    }

    // POST /api/ingest/scan-replace
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
            if (!string.IsNullOrWhiteSpace(request.OldSharePointUrl))
                await _sharePointService.DeleteFileAsync(tenant.TenantId, request.OldSharePointUrl);

            if (request.OldDocumentId.HasValue && request.OldDocumentId.Value != Guid.Empty)
                await _documentRepo.DeleteAsync(request.OldDocumentId.Value);

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
        catch { }
    }
}

// ── Request models ────────────────────────────────────────────────────────────

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

public class ScanConfirmBatchRequest
{
    public List<ScanConfirmRequest> Documents { get; set; } = new();
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