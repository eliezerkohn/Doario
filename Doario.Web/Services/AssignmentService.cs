using Doario.Data.Models.Mail;
using Doario.Data.Repositories;

namespace Doario.Web.Services;

public class AssignmentService
{
    private readonly IDocumentRepository _documents;
    private readonly IAssignmentRepository _assignments;
    private readonly IDeliveryRepository _deliveries;
    private readonly IStaffRepository _staff;
    private readonly EmailDeliveryService _emailDelivery;
    private readonly ILogger<AssignmentService> _logger;

    public AssignmentService(
        IDocumentRepository documents,
        IAssignmentRepository assignments,
        IDeliveryRepository deliveries,
        IStaffRepository staff,
        EmailDeliveryService emailDelivery,
        ILogger<AssignmentService> logger)
    {
        _documents = documents;
        _assignments = assignments;
        _deliveries = deliveries;
        _staff = staff;
        _emailDelivery = emailDelivery;
        _logger = logger;
    }

    public async Task<(bool Success, string Error)> AssignAsync(
        Guid documentId,
        Guid assignedToStaffId,
        Guid assignedByStaffId,
        Guid tenantId,
        string note = "")
    {
        var document = await _documents.GetByIdWithTenantAsync(documentId, tenantId);
        if (document is null)
            return (false, "Document not found.");

        var staff = await _staff.GetByIdAsync(assignedToStaffId, tenantId);
        if (staff is null)
            return (false, "Staff member not found.");

        // ── Remove existing assignment if this is a reassign ──────────────────

        var existing = await _assignments.GetAllByDocumentAsync(documentId, tenantId);

        bool isReassign = existing.Any();
        string previousEmail = string.Empty;

        if (isReassign)
        {
            previousEmail = existing.First().AssignedToEmail;

            var assignmentIds = existing.Select(a => a.DocumentAssignmentId).ToList();
            var deliveries = await _deliveries.GetByAssignmentIdsAsync(assignmentIds);

            if (deliveries.Any())
                await _deliveries.DeleteRangeAsync(deliveries);

            await _assignments.DeleteRangeAsync(existing);
        }

        // ── Create new assignment ──────────────────────────────────────────────

        var now = DateTime.UtcNow;
        var assignment = new DocumentAssignment
        {
            DocumentAssignmentId = Guid.NewGuid(),
            TenantId = tenantId,
            DocumentId = documentId,
            AssignmentTypeId = 1,
            AssignedToStaffId = assignedToStaffId,
            AssignedByStaffId = assignedByStaffId,
            AssignedToEmail = staff.Email,
            AssignedByAI = false,
            AIConfidence = 0,
            AIConfirmedByAdmin = false,
            AISuggestedEmail = string.Empty,
            Note = note ?? string.Empty,
            StaffAccessToken = GenerateToken(),
            StaffAccessTokenExpiresAt = now.AddDays(30),
            AdminAccessToken = GenerateToken(),
            AdminAccessTokenExpiresAt = now.AddDays(30),
            AssignedAt = now
        };

        await _assignments.AddAsync(assignment);
        await _documents.UpdateStatusAsync(documentId, 2); // 2 = Assigned

        // ── Notify previous staff if reassigning ───────────────────────────────

        if (isReassign && !string.IsNullOrEmpty(previousEmail))
        {
            var (notified, notifyError) = await _emailDelivery.SendReassignNotificationAsync(
                document, previousEmail, staff);

            if (!notified)
                _logger.LogWarning(
                    "Reassign notification failed for {Email}: {Error}",
                    previousEmail, notifyError);
        }

        // ── Deliver to new staff ───────────────────────────────────────────────

        var (sent, sendError) = await _emailDelivery.SendAsync(
            documentId, assignment.DocumentAssignmentId, tenantId);

        if (!sent)
        {
            _logger.LogError("EMAIL DELIVERY FAILED: {Error}", sendError);
            return (false, $"Assigned but email failed: {sendError}");
        }

        return (true, string.Empty);
    }

    public async Task<DocumentAssignment> GetAssignmentAsync(Guid documentId, Guid tenantId)
        => await _assignments.GetByDocumentAsync(documentId, tenantId);

    private static string GenerateToken()
    {
        var bytes = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }
}