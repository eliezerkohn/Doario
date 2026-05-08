using Doario.Data.Models.Mail;
using Doario.Data.Repositories;

namespace Doario.Web.Services;

/// <summary>
/// Handles AI assignment suggestions after AiSummaryService runs.
/// Behaviour depends on the tenant's AiAssignmentMode:
///   AutoAssign       — assigns directly, saves suggestion with Status=4
///   SuggestAndApprove — saves suggestion with Status=1, document stays Unassigned
///   Off              — does nothing (never called when Off)
/// </summary>
public class AiAssignmentService
{
    private readonly IStaffRepository _staff;
    private readonly IAiSuggestionRepository _suggestions;
    private readonly AssignmentService _assignmentService;
    private readonly ITenantRepository _tenantRepo;
    private readonly ILogger<AiAssignmentService> _logger;

    public AiAssignmentService(
        IStaffRepository staff,
        IAiSuggestionRepository suggestions,
        AssignmentService assignmentService,
        ITenantRepository tenantRepo,
        ILogger<AiAssignmentService> logger)
    {
        _staff = staff;
        _suggestions = suggestions;
        _assignmentService = assignmentService;
        _tenantRepo = tenantRepo;
        _logger = logger;
    }

    public async Task ProcessAsync(
        Guid documentId,
        Guid tenantId,
        string suggestedEmail,
        int confidence,
        string mode,
        int confidenceThreshold = 8)
    {
        try
        {
            // Find staff by email
            var staffMember = await _staff.GetByEmailAsync(suggestedEmail, tenantId);
            if (staffMember is null)
            {
                _logger.LogWarning(
                    "AiAssignmentService: suggested email {Email} not found in staff list for tenant {TenantId}",
                    suggestedEmail, tenantId);
                return;
            }

            // Use the tenant's SystemStaffId as the "assigned by" for AI actions
            var tenant = await _tenantRepo.GetByIdAsync(tenantId);
            var systemStaffId = tenant?.SystemStaffId ?? Guid.Empty;

            if (mode == "AutoAssign")
            {
                // Assign directly
                var (success, error) = await _assignmentService.AssignAsync(
                    documentId: documentId,
                    assignedToStaffId: staffMember.ImportedStaffId,
                    assignedByStaffId: systemStaffId,
                    tenantId: tenantId,
                    note: $"AI assigned with confidence {confidence}/10");

                if (!success)
                {
                    _logger.LogWarning(
                        "AiAssignmentService: AutoAssign failed for document {DocumentId}: {Error}",
                        documentId, error);
                    return;
                }

                // Save suggestion record as AutoAssigned
                await _suggestions.AddAsync(new DocumentAiSuggestion
                {
                    DocumentAiSuggestionId = Guid.NewGuid(),
                    DocumentId = documentId,
                    TenantId = tenantId,
                    SuggestedStaffId = staffMember.ImportedStaffId,
                    SuggestedEmail = suggestedEmail,
                    Confidence = confidence,
                    SuggestionStatusId = 4, // AutoAssigned
                    ReviewedByStaffId = systemStaffId,
                    ReviewedAt = DateTime.UtcNow,
                });
            }
            else if (mode == "SuggestAndApprove")
            {
                if (confidenceThreshold > 0 && confidence >= confidenceThreshold)
                {
                    // Confidence meets threshold — auto-assign directly, no approval needed
                    // Note assigned silently — email sent as normal, no mention of AI
                    var (success, error) = await _assignmentService.AssignAsync(
                        documentId: documentId,
                        assignedToStaffId: staffMember.ImportedStaffId,
                        assignedByStaffId: systemStaffId,
                        tenantId: tenantId,
                        note: string.Empty); // no AI note in the email

                    if (!success)
                    {
                        _logger.LogWarning(
                            "AiAssignmentService: threshold auto-assign failed for document {DocumentId}: {Error}",
                            documentId, error);
                        return;
                    }

                    await _suggestions.AddAsync(new DocumentAiSuggestion
                    {
                        DocumentAiSuggestionId = Guid.NewGuid(),
                        DocumentId = documentId,
                        TenantId = tenantId,
                        SuggestedStaffId = staffMember.ImportedStaffId,
                        SuggestedEmail = suggestedEmail,
                        Confidence = confidence,
                        SuggestionStatusId = 4, // AutoAssigned
                        ReviewedByStaffId = systemStaffId,
                        ReviewedAt = DateTime.UtcNow,
                    });
                }
                else
                {
                    // Below threshold — goes to Pending Approvals
                    await _suggestions.AddAsync(new DocumentAiSuggestion
                    {
                        DocumentAiSuggestionId = Guid.NewGuid(),
                        DocumentId = documentId,
                        TenantId = tenantId,
                        SuggestedStaffId = staffMember.ImportedStaffId,
                        SuggestedEmail = suggestedEmail,
                        Confidence = confidence,
                        SuggestionStatusId = 1, // Pending
                        ReviewedByStaffId = systemStaffId,
                        // ReviewedAt stays DateTime.MaxValue until admin acts
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "AiAssignmentService error for document {DocumentId}: {Error}",
                documentId, ex.Message);
        }
    }
}