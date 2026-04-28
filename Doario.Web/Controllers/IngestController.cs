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
    private readonly AiSummaryService _aiSummaryService;
    private readonly SharePointService _sharePointService;

    public IngestController(
        ITenantRepository tenantRepo,
        IDocumentRepository documentRepo,
        IErrorLogRepository errorLogRepo,
        OcrService ocrService,
        AiSummaryService aiSummaryService,
        SharePointService sharePointService)
    {
        _tenantRepo = tenantRepo;
        _documentRepo = documentRepo;
        _errorLogRepo = errorLogRepo;
        _ocrService = ocrService;
        _aiSummaryService = aiSummaryService;
        _sharePointService = sharePointService;
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

        return Ok(new
        {
            status = "ok",
            tenant = tenant.Name,
            version = "1.0.0",
        });
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
            // Upload first page as PNG — Day 15 replaces with proper PDF
            var imageBytes = Convert.FromBase64String(request.Pages[0]);
            var fileName = $"scan_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png";

            using var stream = new MemoryStream(imageBytes);
            var sharePointUrl = await _sharePointService.UploadDocumentAsync(
                tenant.TenantId, stream, fileName);

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
                SenderDisplayName = "Scanner",
                SenderEmail = string.Empty,
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
            await _errorLogRepo.AddAsync(new ErrorLog
            {
                ErrorLogId = Guid.NewGuid(),
                TenantId = tenant.TenantId,
                ErrorType = "Delivery",
                Message = ex.Message,
                StackTrace = ex.StackTrace,
                CreatedAt = DateTime.UtcNow,
            });

            return StatusCode(500, new { error = $"Failed to process scan: {ex.Message}" });
        }
    }

    // POST /api/ingest/scan-batch
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
            var batchScanId = Guid.NewGuid();
            var documents = new List<BatchDocumentResult>();
            var boundaries = DetectDocumentBoundaries(request.Pages);

            foreach (var boundary in boundaries)
            {
                var pages = request.Pages.GetRange(boundary.StartPage, boundary.PageCount);

                // Upload first page as PNG — Day 15 replaces with proper multi-page PDF
                var imageBytes = Convert.FromBase64String(pages[0]);
                var fileName = $"scan_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{boundary.Index + 1}.png";

                using var stream = new MemoryStream(imageBytes);
                var sharePointUrl = await _sharePointService.UploadDocumentAsync(
                    tenant.TenantId, stream, fileName);

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
                    SenderDisplayName = "Scanner",
                    SenderEmail = string.Empty,
                    BatchScanId = batchScanId,
                    BatchPageStart = boundary.StartPage + 1,
                    BatchPageEnd = boundary.StartPage + boundary.PageCount,
                    UploadedAt = DateTime.UtcNow,
                };

                await _documentRepo.CreateAsync(document);
                _ocrService.RunInBackground(document.DocumentId);

                documents.Add(new BatchDocumentResult
                {
                    DocumentId = document.DocumentId,
                    SharePointUrl = sharePointUrl,
                    PageStart = document.BatchPageStart.Value,
                    PageEnd = document.BatchPageEnd.Value,
                    FileName = fileName,
                    PreviewBase64 = pages[0],
                });
            }

            return Ok(new
            {
                batchScanId,
                documentCount = documents.Count,
                documents,
                message = $"{documents.Count} document{(documents.Count != 1 ? "s" : "")} detected. OCR and AI summaries in progress.",
            });
        }
        catch (Exception ex)
        {
            await _errorLogRepo.AddAsync(new ErrorLog
            {
                ErrorLogId = Guid.NewGuid(),
                TenantId = tenant.TenantId,
                ErrorType = "Delivery",
                Message = ex.Message,
                StackTrace = ex.StackTrace,
                CreatedAt = DateTime.UtcNow,
            });

            return StatusCode(500, new { error = $"Failed to process batch scan: {ex.Message}" });
        }
    }

    // ── Document boundary detection ───────────────────────────────

    private List<DocumentBoundary> DetectDocumentBoundaries(List<string> pages)
    {
        var boundaries = new List<DocumentBoundary>();
        var currentStart = 0;
        var index = 0;

        for (int i = 0; i < pages.Count; i++)
        {
            if (IsBlankPage(pages[i]) && i > currentStart)
            {
                boundaries.Add(new DocumentBoundary
                {
                    Index = index++,
                    StartPage = currentStart,
                    PageCount = i - currentStart,
                });
                currentStart = i + 1;
            }
        }

        if (currentStart < pages.Count)
        {
            boundaries.Add(new DocumentBoundary
            {
                Index = index,
                StartPage = currentStart,
                PageCount = pages.Count - currentStart,
            });
        }

        if (boundaries.Count == 0)
        {
            boundaries.Add(new DocumentBoundary
            {
                Index = 0,
                StartPage = 0,
                PageCount = pages.Count,
            });
        }

        return boundaries;
    }

    private bool IsBlankPage(string base64Image)
    {
        try
        {
            var bytes = Convert.FromBase64String(base64Image);
            return bytes.Length < 500;
        }
        catch { return false; }
    }
}

// ── Request / Response models ─────────────────────────────────────

public class IngestScanRequest
{
    public List<string> Pages { get; set; } = new();
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