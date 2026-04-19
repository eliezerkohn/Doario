using Doario.Data;
using Doario.Data.Models.Mail;
using Doario.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Doario.Web.Controllers;

[Route("api/upload")]
[ApiController]
public class UploadController : ControllerBase
{
    private readonly SharePointService _sharePoint;
    private readonly DoarioDataContext _db;
    private readonly TenantContext _tenantContext;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UploadController> _logger;

    public UploadController(
        SharePointService sharePoint,
        DoarioDataContext db,
        TenantContext tenantContext,
        IServiceScopeFactory scopeFactory,
        ILogger<UploadController> logger)
    {
        _sharePoint = sharePoint;
        _db = db;
        _tenantContext = tenantContext;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file selected.");

        if (!_tenantContext.IsResolved)
            return Unauthorized("Tenant could not be identified.");

        var tenant = await _db.Tenants
            .FirstOrDefaultAsync(t => t.TenantId == _tenantContext.TenantId);

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
            DocumentStatusId = 1,
            SenderTypeId = tenant.UnknownSenderTypeId,
            SenderId = tenant.UnknownSenderId,
            UploadedByStaffId = tenant.SystemStaffId,
            SharePointUrl = sharePointUrl,
            OcrText = null,
            SenderDisplayName = string.Empty,
            SenderEmail = string.Empty,
            SenderMatchConfidence = 0,
            UploadedAt = DateTime.UtcNow
        };

        _db.Documents.Add(document);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Document {DocumentId} created for tenant {TenantId}.",
            document.DocumentId, _tenantContext.TenantId);

        // Fire OCR using IServiceScopeFactory — singleton, never disposed
        var documentId = document.DocumentId;
        var scopeFactory = _scopeFactory;
        _ = Task.Run(async () =>
        {
            await Task.Delay(500);
            using var scope = scopeFactory.CreateScope();
            var ocrService = scope.ServiceProvider.GetRequiredService<OcrService>();
            await ocrService.RunOcrAsync(documentId);
        });

        return Ok(new
        {
            document.DocumentId,
            document.SharePointUrl
        });
    }
}