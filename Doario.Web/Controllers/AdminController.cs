using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Doario.Data;
using Doario.Web.Services;

namespace Doario.Web.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "DoarioAdmin")]
public class AdminController : ControllerBase
{
    private readonly DoarioDataContext _db;
    private readonly TenantContext _tenant;

    public AdminController(DoarioDataContext db, TenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    // GET /api/admin/queue
    [HttpGet("queue")]
    public async Task<IActionResult> GetQueue()
    {
        if (!_tenant.IsResolved)
            return Unauthorized();

        var docs = await _db.Documents
            .Where(d => d.TenantId == _tenant.TenantId)
            .OrderByDescending(d => d.UploadedAt)
            .Take(50)
            .Select(d => new
            {
                d.DocumentId,
                d.UploadedAt,
                d.OcrText,
                StatusId = d.DocumentStatusId,
                SenderId = d.SenderId,
                d.AiSummary
            })
            .ToListAsync();

        return Ok(docs);
    }

    [HttpGet("whoami")]
    [AllowAnonymous]
    public IActionResult WhoAmI()
    {
        return Ok(new
        {
            IsAuthenticated = User.Identity.IsAuthenticated,
            Name = User.Identity.Name,
            AuthType = User.Identity.AuthenticationType,
            Claims = User.Claims.Select(c => new { c.Type, c.Value })
        });
    }
}