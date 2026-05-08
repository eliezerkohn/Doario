using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Doario.Data;
using Doario.Data.Repositories;
using Doario.Web.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
    private readonly IAiSuggestionRepository _suggestions;
    private readonly IDocumentFeedbackRepository _feedbackRepo;
    private readonly DoarioDataContext _db;
    private readonly ApproveAllQueue _approveAllQueue;

    public AssignmentController(
        IAssignmentRepository assignments,
        IDeliveryRepository deliveries,
        IStaffRepository staff,
        TenantContext tenant,
        AssignmentService assignmentService,
        IAiSuggestionRepository suggestions,
        IDocumentFeedbackRepository feedbackRepo,
        DoarioDataContext db,
        ApproveAllQueue approveAllQueue)
    {
        _assignments = assignments;
        _deliveries = deliveries;
        _staff = staff;
        _tenant = tenant;
        _assignmentService = assignmentService;
        _suggestions = suggestions;
        _feedbackRepo = feedbackRepo;
        _db = db;
        _approveAllQueue = approveAllQueue;
    }

    [HttpGet("staff")]
    public async Task<IActionResult> GetStaff()
    {
        if (!_tenant.IsResolved) return Unauthorized();
        var staff = await _staff.GetAllForTenantAsync(_tenant.TenantId);
        return Ok(staff.Select(s => new
        {
            s.ImportedStaffId,
            s.FirstName,
            s.LastName,
            s.Email,
            s.IsAdmin
        }));
    }

    [HttpPost("assign")]
    public async Task<IActionResult> Assign([FromBody] AssignRequest request)
    {
        if (!_tenant.IsResolved) return Unauthorized();
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value
                      ?? User.FindFirst("preferred_username")?.Value ?? string.Empty;
        var adminStaff = await _staff.GetByEmailAsync(userEmail, _tenant.TenantId);
        if (adminStaff is null) return BadRequest(new { error = "Admin staff record not found." });
        var (success, error) = await _assignmentService.AssignAsync(
            documentId: request.DocumentId,
            assignedToStaffId: request.StaffId,
            assignedByStaffId: adminStaff.ImportedStaffId,
            tenantId: _tenant.TenantId,
            note: request.Note ?? string.Empty);
        if (!success) return BadRequest(new { error });
        return Ok(new { message = "Document assigned successfully." });
    }

    [HttpGet("{documentId:guid}")]
    public async Task<IActionResult> GetAssignment(Guid documentId)
    {
        if (!_tenant.IsResolved) return Unauthorized();
        var assignment = await _assignments.GetByDocumentAsync(documentId, _tenant.TenantId);
        if (assignment is null) return Ok(null);
        var deliveries = await _deliveries.GetByAssignmentIdsAsync(
            new List<Guid> { assignment.DocumentAssignmentId });
        var latest = deliveries.OrderByDescending(d => d.CreatedAt).FirstOrDefault();
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

    // GET /api/assignment/suggestion/{documentId}
    [HttpGet("suggestion/{documentId:guid}")]
    public async Task<IActionResult> GetSuggestion(Guid documentId)
    {
        if (!_tenant.IsResolved) return Unauthorized();
        var suggestion = await _suggestions.GetByDocumentAsync(documentId, _tenant.TenantId);
        if (suggestion == null) return Ok(null);
        return Ok(new
        {
            suggestion.DocumentAiSuggestionId,
            suggestion.DocumentId,
            suggestion.SuggestedEmail,
            SuggestedStaffName = $"{suggestion.SuggestedStaff.FirstName} {suggestion.SuggestedStaff.LastName}",
            suggestion.Confidence,
            suggestion.CreatedAt,
        });
    }

    [HttpGet("by-email")]
    public async Task<IActionResult> GetByEmail([FromQuery] string email)
    {
        if (!_tenant.IsResolved) return Unauthorized();
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { error = "Email is required." });
        var assignments = await _assignments.GetByEmailAsync(email, _tenant.TenantId);
        return Ok(assignments.Select(a => new
        {
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
        }));
    }

    // GET /api/assignment/pending-suggestions
    // Direct DB projection — no AiSummary in list
    [HttpGet("pending-suggestions")]
    public async Task<IActionResult> GetPendingSuggestions()
    {
        if (!_tenant.IsResolved) return Unauthorized();

        var results = await _db.DocumentAiSuggestions
            .Where(s => s.TenantId == _tenant.TenantId && s.SuggestionStatusId == 1)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new
            {
                s.DocumentAiSuggestionId,
                s.DocumentId,
                s.SuggestedEmail,
                SuggestedStaffName = s.SuggestedStaff.FirstName + " " + s.SuggestedStaff.LastName,
                s.Confidence,
                s.CreatedAt,
                s.Document.OriginalFileName,
                s.Document.UploadedAt,
                SenderDisplayName = s.Document.Sender != null ? s.Document.Sender.DisplayName : string.Empty,
                StatusName = s.Document.DocumentStatus.Name,
            })
            .ToListAsync();

        return Ok(results);
    }

    [HttpGet("pending-count")]
    public async Task<IActionResult> GetPendingCount()
    {
        if (!_tenant.IsResolved) return Unauthorized();
        var count = await _suggestions.GetPendingCountAsync(_tenant.TenantId);
        return Ok(new { count });
    }

    [HttpPost("approve/{suggestionId:guid}")]
    public async Task<IActionResult> ApproveSuggestion(Guid suggestionId)
    {
        if (!_tenant.IsResolved) return Unauthorized();
        var suggestion = await _suggestions.GetByIdAsync(suggestionId, _tenant.TenantId);
        if (suggestion is null) return NotFound();
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value
                      ?? User.FindFirst("preferred_username")?.Value ?? string.Empty;
        var adminStaff = await _staff.GetByEmailAsync(userEmail, _tenant.TenantId);
        if (adminStaff is null) return BadRequest(new { error = "Admin staff record not found." });
        var (success, error) = await _assignmentService.AssignAsync(
            documentId: suggestion.DocumentId,
            assignedToStaffId: suggestion.SuggestedStaffId,
            assignedByStaffId: adminStaff.ImportedStaffId,
            tenantId: _tenant.TenantId,
            note: string.Empty);
        if (!success) return BadRequest(new { error });
        suggestion.SuggestionStatusId = 2;
        suggestion.ReviewedByStaffId = adminStaff.ImportedStaffId;
        suggestion.ReviewedAt = DateTime.UtcNow;
        await _suggestions.UpdateAsync(suggestion);
        return Ok(new { message = "Suggestion approved and document assigned." });
    }

    // POST /api/assignment/approve-all
    // Starts a background job — returns immediately with job status
    // Browser reloads won't affect processing
    [HttpPost("approve-all")]
    public async Task<IActionResult> ApproveAll()
    {
        if (!_tenant.IsResolved) return Unauthorized();

        // Check if already running
        var currentStatus = _approveAllQueue.GetStatus(_tenant.TenantId.ToString());
        if (currentStatus.IsRunning)
            return Ok(new { alreadyRunning = true, message = "Approval already in progress." });

        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value
                      ?? User.FindFirst("preferred_username")?.Value ?? string.Empty;
        var adminStaff = await _staff.GetByEmailAsync(userEmail, _tenant.TenantId);
        if (adminStaff is null) return BadRequest(new { error = "Admin staff record not found." });

        // Get all pending suggestion IDs
        var suggestionIds = await _db.DocumentAiSuggestions
            .Where(s => s.TenantId == _tenant.TenantId && s.SuggestionStatusId == 1)
            .Select(s => s.DocumentAiSuggestionId)
            .ToListAsync();

        if (!suggestionIds.Any())
            return Ok(new { message = "No pending suggestions.", total = 0 });

        var started = _approveAllQueue.StartApproveAll(
            _tenant.TenantId, suggestionIds, adminStaff.ImportedStaffId);

        return Ok(new
        {
            started,
            total = suggestionIds.Count,
            message = $"Started approving {suggestionIds.Count} suggestions in background."
        });
    }

    // GET /api/assignment/approve-all-status
    // Poll this to get live progress — safe to call every 2 seconds
    [HttpGet("approve-all-status")]
    public IActionResult GetApproveAllStatus()
    {
        if (!_tenant.IsResolved) return Unauthorized();
        var status = _approveAllQueue.GetStatus(_tenant.TenantId.ToString());
        return Ok(status);
    }

    [HttpPost("overwrite/{suggestionId:guid}")]
    public async Task<IActionResult> OverwriteSuggestion(
        Guid suggestionId, [FromBody] AssignRequest request)
    {
        if (!_tenant.IsResolved) return Unauthorized();
        var suggestion = await _suggestions.GetByIdAsync(suggestionId, _tenant.TenantId);
        if (suggestion is null) return NotFound();
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value
                      ?? User.FindFirst("preferred_username")?.Value ?? string.Empty;
        var adminStaff = await _staff.GetByEmailAsync(userEmail, _tenant.TenantId);
        if (adminStaff is null) return BadRequest(new { error = "Admin staff record not found." });
        var (success, error) = await _assignmentService.AssignAsync(
            documentId: suggestion.DocumentId,
            assignedToStaffId: request.StaffId,
            assignedByStaffId: adminStaff.ImportedStaffId,
            tenantId: _tenant.TenantId,
            note: request.Note ?? string.Empty);
        if (!success) return BadRequest(new { error });
        suggestion.SuggestionStatusId = 3;
        suggestion.ReviewedByStaffId = adminStaff.ImportedStaffId;
        suggestion.ReviewedAt = DateTime.UtcNow;
        await _suggestions.UpdateAsync(suggestion);
        var chosenStaff = await _staff.GetByIdAsync(request.StaffId, _tenant.TenantId);
        if (chosenStaff != null)
        {
            var doc = suggestion.Document;
            var snippet = doc?.OcrText?.Length > 120 ? doc.OcrText[..120] : doc?.OcrText ?? string.Empty;
            await _feedbackRepo.AddAsync(new Doario.Data.Models.Mail.DocumentFeedback
            {
                DocumentFeedbackId = Guid.NewGuid(),
                TenantId = _tenant.TenantId,
                DocumentId = suggestion.DocumentId,
                FeedbackTypeId = 2,
                AiClassification = suggestion.SuggestedEmail,
                CorrectedClassification = chosenStaff.Email,
                DocumentSnippet = snippet,
            });
        }
        return Ok(new { message = "Suggestion overwritten and correction saved for AI learning." });
    }

    public class AssignRequest
    {
        public Guid DocumentId { get; set; }
        public Guid StaffId { get; set; }
        public Guid? CcStaffId { get; set; }
        public string Note { get; set; }
    }
}