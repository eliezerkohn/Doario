using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Doario.Data.Repositories;
using Doario.Web.Services;

namespace Doario.Web.Controllers;

[ApiController]
[Route("api/assignment")]
[Authorize(Roles = "DoarioAdmin")]
public class AssignmentController : ControllerBase
{
    private readonly IAssignmentRepository _assignments;
    private readonly IDeliveryRepository _deliveries;
    private readonly IStaffRepository _staff;
    private readonly TenantContext _tenant;
    private readonly AssignmentService _assignmentService;

    public AssignmentController(
        IAssignmentRepository assignments,
        IDeliveryRepository deliveries,
        IStaffRepository staff,
        TenantContext tenant,
        AssignmentService assignmentService)
    {
        _assignments = assignments;
        _deliveries = deliveries;
        _staff = staff;
        _tenant = tenant;
        _assignmentService = assignmentService;
    }

    // GET /api/assignment/staff
    [HttpGet("staff")]
    public async Task<IActionResult> GetStaff()
    {
        if (!_tenant.IsResolved) return Unauthorized();

        var staff = await _staff.GetAllForTenantAsync(_tenant.TenantId);
        var result = staff.Select(s => new {
            s.ImportedStaffId,
            s.FirstName,
            s.LastName,
            s.Email,
            s.IsAdmin
        });
        return Ok(result);
    }

    // POST /api/assignment/assign
    [HttpPost("assign")]
    public async Task<IActionResult> Assign([FromBody] AssignRequest request)
    {
        if (!_tenant.IsResolved) return Unauthorized();

        var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                      ?? User.FindFirst("preferred_username")?.Value
                      ?? string.Empty;

        var adminStaff = await _staff.GetByEmailAsync(userEmail, _tenant.TenantId);
        if (adminStaff is null)
            return BadRequest(new { error = "Admin staff record not found." });

        var (success, error) = await _assignmentService.AssignAsync(
            documentId: request.DocumentId,
            assignedToStaffId: request.StaffId,
            assignedByStaffId: adminStaff.ImportedStaffId,
            tenantId: _tenant.TenantId,
            note: request.Note ?? string.Empty);

        if (!success) return BadRequest(new { error });
        return Ok(new { message = "Document assigned successfully." });
    }

    // GET /api/assignment/{documentId}
    [HttpGet("{documentId:guid}")]
    public async Task<IActionResult> GetAssignment(Guid documentId)
    {
        if (!_tenant.IsResolved) return Unauthorized();

        var assignment = await _assignments.GetByDocumentAsync(documentId, _tenant.TenantId);
        if (assignment is null) return Ok(null);

        var deliveries = await _deliveries.GetByAssignmentIdsAsync(
            new List<Guid> { assignment.DocumentAssignmentId });

        var latest = deliveries
            .OrderByDescending(d => d.CreatedAt)
            .FirstOrDefault();

        var deliveryStatus = latest?.SystemStatusId switch
        {
            8 => "sent",
            5 => "failed",
            9 => "permanent_fail",
            7 => "pending",
            _ => "unknown"
        };

        return Ok(new
        {
            assignment.DocumentAssignmentId,
            assignment.AssignedToEmail,
            StaffName = $"{assignment.AssignedToStaff.FirstName} {assignment.AssignedToStaff.LastName}",
            assignment.AssignedAt,
            assignment.Note,
            DeliveryStatus = deliveryStatus,
            DeliveryError = latest?.ErrorMessage
        });
    }

    // GET /api/assignment/by-email?email=sarah@specialtyrx.com
    [HttpGet("by-email")]
    public async Task<IActionResult> GetByEmail([FromQuery] string email)
    {
        if (!_tenant.IsResolved) return Unauthorized();

        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { error = "Email is required." });

        var assignments = await _assignments.GetByEmailAsync(email, _tenant.TenantId);

        var result = assignments.Select(a => new {
            a.DocumentId,
            a.Document.UploadedAt,
            a.Document.OriginalFileName,
            a.Document.SharePointUrl,
            SenderDisplayName = a.Document.Sender != null ? a.Document.Sender.DisplayName : string.Empty,
            SenderEmail = a.Document.Sender != null ? a.Document.Sender.Email : string.Empty,
            a.Document.AiSummary,
            a.Document.OcrText,
            StatusId = a.Document.DocumentStatusId,
            StatusName = a.Document.DocumentStatus.Name,
            a.AssignedAt,
            a.AssignedToEmail,
            AssignedToName = $"{a.AssignedToStaff.FirstName} {a.AssignedToStaff.LastName}",
            a.Note,
            IsViewed = false
        });

        return Ok(result);
    }
}

public class AssignRequest
{
    public Guid DocumentId { get; set; }
    public Guid StaffId { get; set; }
    public Guid? CcStaffId { get; set; }
    public string Note { get; set; }
}