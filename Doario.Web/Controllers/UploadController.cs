using Doario.Data.Models.Mail;
using Doario.Data.Repositories;
using Doario.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Doario.Web.Controllers;

[Route("api/upload")]
[ApiController]
public class UploadController : ControllerBase
{
    private readonly SharePointService _sharePoint;
    private readonly ITenantRepository _tenants;
    private readonly IDocumentRepository _documents;
    private readonly TenantContext _tenantContext;
    private readonly OcrService _ocrService;
    private readonly ILogger<UploadController> _logger;

    public UploadController(
        SharePointService sharePoint,
        ITenantRepository tenants,
        IDocumentRepository documents,
        TenantContext tenantContext,
        OcrService ocrService,
        ILogger<UploadController> logger)
    {
        _sharePoint = sharePoint;
        _tenants = tenants;
        _documents = documents;
        _tenantContext = tenantContext;
        _ocrService = ocrService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file selected.");

        if (!_tenantContext.IsResolved)
            return Unauthorized("Tenant could not be identified.");

        var tenant = await _tenants.GetByIdAsync(_tenantContext.TenantId);
        if (tenant is null)
            return Unauthorized("Tenant not found.");

        await using var stream = file.OpenReadStream();

        var sharePointUrl = await _sharePoint.UploadDocumentAsync(
            tenantId: _tenantContext.TenantId,
            fileStream: stream,
            fileName: file.FileName);

        var document = new Document
        {
            DocumentId = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            DocumentStatusId = 1, // Unassigned
            SenderTypeId = tenant.UnknownSenderTypeId,
            SenderId = tenant.UnknownSenderId,
            UploadedByStaffId = tenant.SystemStaffId,
            SharePointUrl = sharePointUrl,
            OcrText = null,
            SenderDisplayName = string.Empty,
            SenderEmail = string.Empty,
            SenderMatchConfidence = 0,
            UploadedAt = DateTime.UtcNow,
            OriginalFileName = file.FileName
        };

        await _documents.CreateAsync(document);

        _logger.LogInformation(
            "Document {DocumentId} created for tenant {TenantId}.",
            document.DocumentId, _tenantContext.TenantId);

        // Fire OCR in background
        _ocrService.RunInBackground(document.DocumentId);

        return Ok(new
        {
            document.DocumentId,
            document.SharePointUrl
        });
    }
}