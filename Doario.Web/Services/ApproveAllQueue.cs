namespace Doario.Web.Services;

/// <summary>
/// Singleton service that processes AI suggestion approvals in the background.
/// Browser reloads don't affect processing — it runs server-side until complete.
/// Progress is tracked in memory and polled by the frontend.
/// </summary>
public class ApproveAllQueue
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ApproveAllQueue> _logger;

    // Current job state — one job at a time per server instance
    private volatile bool _isRunning = false;
    private int _total = 0;
    private int _approved = 0;
    private int _failed = 0;
    private string _tenantId = string.Empty;
    private DateTime _startedAt = DateTime.MinValue;

    public ApproveAllQueue(IServiceScopeFactory scopeFactory, ILogger<ApproveAllQueue> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Returns current job status for polling.
    /// </summary>
    public ApproveAllStatus GetStatus(string tenantId)
    {
        // Only return status for the tenant that started the job
        if (_tenantId != tenantId)
            return new ApproveAllStatus { IsRunning = false, Total = 0, Approved = 0, Failed = 0 };

        return new ApproveAllStatus
        {
            IsRunning = _isRunning,
            Total = _total,
            Approved = _approved,
            Failed = _failed,
            StartedAt = _startedAt,
        };
    }

    /// <summary>
    /// Starts a background approval job for all pending suggestions for a tenant.
    /// Returns false if a job is already running.
    /// </summary>
    public bool StartApproveAll(
        Guid tenantId,
        List<Guid> suggestionIds,
        Guid adminStaffId)
    {
        if (_isRunning) return false;

        _isRunning = true;
        _total = suggestionIds.Count;
        _approved = 0;
        _failed = 0;
        _tenantId = tenantId.ToString();
        _startedAt = DateTime.UtcNow;

        _ = Task.Run(() => ProcessAsync(tenantId, suggestionIds, adminStaffId));
        return true;
    }

    private async Task ProcessAsync(Guid tenantId, List<Guid> suggestionIds, Guid adminStaffId)
    {
        _logger.LogInformation(
            "ApproveAllQueue: starting {Count} approvals for tenant {TenantId}",
            suggestionIds.Count, tenantId);

        foreach (var suggestionId in suggestionIds)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var suggestions = scope.ServiceProvider.GetRequiredService<Doario.Data.Repositories.IAiSuggestionRepository>();
                var assignmentService = scope.ServiceProvider.GetRequiredService<AssignmentService>();

                var suggestion = await suggestions.GetByIdAsync(suggestionId, tenantId);
                if (suggestion == null)
                {
                    _failed++;
                    continue;
                }

                // Skip already approved
                if (suggestion.SuggestionStatusId != 1)
                {
                    _approved++;
                    continue;
                }

                var (success, error) = await assignmentService.AssignAsync(
                    documentId: suggestion.DocumentId,
                    assignedToStaffId: suggestion.SuggestedStaffId,
                    assignedByStaffId: adminStaffId,
                    tenantId: tenantId,
                    note: string.Empty);

                if (success)
                {
                    suggestion.SuggestionStatusId = 2; // Approved
                    suggestion.ReviewedByStaffId = adminStaffId;
                    suggestion.ReviewedAt = DateTime.UtcNow;
                    await suggestions.UpdateAsync(suggestion);
                    _approved++;
                }
                else
                {
                    _logger.LogWarning(
                        "ApproveAllQueue: approval failed for suggestion {Id}: {Error}",
                        suggestionId, error);
                    _failed++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "ApproveAllQueue: exception approving suggestion {Id}", suggestionId);
                _failed++;
            }
        }

        _logger.LogInformation(
            "ApproveAllQueue: finished. Approved={Approved}, Failed={Failed}",
            _approved, _failed);

        _isRunning = false;
    }
}

public class ApproveAllStatus
{
    public bool IsRunning { get; set; }
    public int Total { get; set; }
    public int Approved { get; set; }
    public int Failed { get; set; }
    public DateTime StartedAt { get; set; }
}