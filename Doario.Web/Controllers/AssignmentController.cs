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
    private readonly IStaffRepository _staff;
    private readonly TenantContext _tenant;
    private readonly AssignmentService _assignmentService;

    public AssignmentController(
        IAssignmentRepository assignments,
        IStaffRepository staff,
        TenantContext tenant,
        AssignmentService assignmentService)
    {
        _assignments = assignments;
        _staff = staff;
        _tenant = tenant;
        _assignmentService = assignmentService;
    }

    // GET /api/assignment/staff
    [HttpGet("staff")]
    public async Task<IActionResult> GetStaff()
    {
        if (!_tenant.IsResolved)
            return Unauthorized();

        var staff = await _staff.GetAllForTenantAsync(_tenant.TenantId);

        var result = staff.Select(s => new
        {
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
        if (!_tenant.IsResolved)
            return Unauthorized();

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

        if (!success)
            return BadRequest(new { error });

        return Ok(new { message = "Document assigned successfully." });
    }

    // GET /api/assignment/{documentId}
    [HttpGet("{documentId:guid}")]
    public async Task<IActionResult> GetAssignment(Guid documentId)
    {
        if (!_tenant.IsResolved)
            return Unauthorized();

        var assignment = await _assignments.GetByDocumentAsync(documentId, _tenant.TenantId);
        if (assignment is null)
            return Ok(null);

        return Ok(new
        {
            assignment.DocumentAssignmentId,
            assignment.AssignedToEmail,
            StaffName = $"{assignment.AssignedToStaff.FirstName} {assignment.AssignedToStaff.LastName}",
            assignment.AssignedAt,
            assignment.Note
        });
    }
}

public class AssignRequest
{
    public Guid DocumentId { get; set; }
    public Guid StaffId { get; set; }
    public Guid? CcStaffId { get; set; }
    public string Note { get; set; }
}