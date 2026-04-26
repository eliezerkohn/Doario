using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Doario.Data.Repositories;
using Doario.Web.Services;

namespace Doario.Web.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "DoarioAdmin")]
public class AdminController : ControllerBase
{
    private readonly IDocumentRepository _documents;
    private readonly TenantContext _tenant;

    public AdminController(IDocumentRepository documents, TenantContext tenant)
    {
        _documents = documents;
        _tenant = tenant;
    }

    // GET /api/admin/queue?page=1&pageSize=50
    [HttpGet("queue")]
    public async Task<IActionResult> GetQueue(int page = 1, int pageSize = 50)
    {
        if (!_tenant.IsResolved)
            return Unauthorized();

        pageSize = Math.Min(pageSize, 500);

        var docs = await _documents.GetQueueAsync(_tenant.TenantId, page, pageSize);

        var result = docs.Select(d => new
        {
            d.DocumentId,
            d.UploadedAt,
            d.OcrText,
            StatusId = d.DocumentStatusId,
            StatusName = d.DocumentStatus.Name,
            d.AiSummary,
            d.OriginalFileName
        });

        return Ok(result);
    }
}