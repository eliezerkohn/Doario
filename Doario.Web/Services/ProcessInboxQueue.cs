using Doario.Data.Repositories;

namespace Doario.Web.Services;

/// <summary>
/// Singleton service that processes monitored inboxes in the background.
/// Browser reloads don't affect processing — runs server-side until complete.
/// Checks for duplicate filenames before saving to prevent double-processing.
/// </summary>
public class ProcessInboxQueue
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProcessInboxQueue> _logger;

    private volatile bool _isRunning = false;
    private readonly List<InboxJobStatus> _statuses = new();
    private readonly object _lock = new();
    private string _tenantId = string.Empty;
    private DateTime _startedAt = DateTime.MinValue;

    public ProcessInboxQueue(IServiceScopeFactory scopeFactory, ILogger<ProcessInboxQueue> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public ProcessInboxJobResult GetStatus(string tenantId)
    {
        if (_tenantId != tenantId)
            return new ProcessInboxJobResult { IsRunning = false, Inboxes = new() };

        lock (_lock)
        {
            return new ProcessInboxJobResult
            {
                IsRunning = _isRunning,
                StartedAt = _startedAt,
                Inboxes = _statuses.Select(s => new InboxJobStatus
                {
                    InboxId = s.InboxId,
                    EmailAddress = s.EmailAddress,
                    State = s.State,
                    DocumentsProcessed = s.DocumentsProcessed,
                    Error = s.Error,
                }).ToList(),
            };
        }
    }

    public bool Start(Guid tenantId, List<(Guid InboxId, string EmailAddress)> inboxes)
    {
        if (_isRunning) return false;

        lock (_lock)
        {
            _isRunning = true;
            _tenantId = tenantId.ToString();
            _startedAt = DateTime.UtcNow;
            _statuses.Clear();
            foreach (var inbox in inboxes)
            {
                _statuses.Add(new InboxJobStatus
                {
                    InboxId = inbox.InboxId,
                    EmailAddress = inbox.EmailAddress,
                    State = InboxJobState.Waiting,
                });
            }
        }

        _ = Task.Run(() => ProcessAsync(tenantId));
        return true;
    }

    private async Task ProcessAsync(Guid tenantId)
    {
        _logger.LogInformation(
            "ProcessInboxQueue: starting {Count} inboxes for tenant {TenantId}",
            _statuses.Count, tenantId);

        foreach (var status in _statuses)
        {
            SetState(status.InboxId, InboxJobState.Processing);
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var monitoredInboxRepo = scope.ServiceProvider
                    .GetRequiredService<ITenantMonitoredInboxRepository>();
                var tenantRepo = scope.ServiceProvider
                    .GetRequiredService<ITenantRepository>();
                var graph = scope.ServiceProvider
                    .GetRequiredService<Microsoft.Graph.GraphServiceClient>();
                var documentRepo = scope.ServiceProvider
                    .GetRequiredService<IDocumentRepository>();
                var sharePointService = scope.ServiceProvider
                    .GetRequiredService<SharePointService>();
                var ocrService = scope.ServiceProvider
                    .GetRequiredService<OcrService>();
                var errorLogRepo = scope.ServiceProvider
                    .GetRequiredService<IErrorLogRepository>();

                var inbox = await monitoredInboxRepo.GetByIdAsync(status.InboxId);
                if (inbox == null)
                {
                    SetState(status.InboxId, InboxJobState.Done, 0);
                    continue;
                }

                var tenant = await tenantRepo.GetByIdAsync(inbox.TenantId);
                if (tenant == null)
                {
                    SetState(status.InboxId, InboxJobState.Failed, error: "Tenant not found.");
                    continue;
                }

                var now = DateTime.UtcNow;
                var filterDate = inbox.LastProcessedAt.ToString("yyyy-MM-ddTHH:mm:ssZ");
                var filter = $"receivedDateTime ge {filterDate}";

                var allMessages = new List<Microsoft.Graph.Models.Message>();

                var firstPage = await graph.Users[inbox.EmailAddress]
                    .Messages
                    .GetAsync(req =>
                    {
                        req.QueryParameters.Filter = filter;
                        req.QueryParameters.Select = new[]
                        {
                            "id", "subject", "from", "receivedDateTime", "hasAttachments"
                        };
                        req.QueryParameters.Top = 50;
                    });

                if (firstPage?.Value != null)
                {
                    allMessages.AddRange(firstPage.Value);
                    var pageIterator = Microsoft.Graph.PageIterator<
                        Microsoft.Graph.Models.Message,
                        Microsoft.Graph.Models.MessageCollectionResponse>
                        .CreatePageIterator(graph, firstPage, msg =>
                        {
                            allMessages.Add(msg);
                            return true;
                        });
                    await pageIterator.IterateAsync();
                }

                var processed = 0;
                var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".tiff", ".tif" };
                var sourceTypeId = inbox.IsFaxInbox ? 10 : 11;

                foreach (var message in allMessages)
                {
                    if (message.HasAttachments != true) continue;

                    try
                    {
                        var attachments = await graph.Users[inbox.EmailAddress]
                            .Messages[message.Id]
                            .Attachments
                            .GetAsync();

                        if (attachments?.Value == null) continue;

                        foreach (var attachment in attachments.Value)
                        {
                            if (attachment is not Microsoft.Graph.Models.FileAttachment fileAttachment) continue;
                            var ext = Path.GetExtension(fileAttachment.Name ?? "").ToLowerInvariant();
                            if (!allowedExtensions.Contains(ext)) continue;
                            var contentBytes = fileAttachment.ContentBytes;
                            if (contentBytes == null || contentBytes.Length == 0) continue;

                            var timestamp = (message.ReceivedDateTime ?? DateTimeOffset.UtcNow)
                                .ToString("yyyyMMdd_HHmmss");
                            var prefix = inbox.IsFaxInbox ? "fax" : "email";
                            var fileName = $"{prefix}_{timestamp}_{fileAttachment.Name}";

                            // ── Duplicate check — skip if already saved ───────
                            var alreadyExists = await documentRepo.ExistsByFileNameAsync(
                                tenant.TenantId, fileName);
                            if (alreadyExists)
                            {
                                _logger.LogDebug(
                                    "ProcessInboxQueue: skipping duplicate {FileName}",
                                    fileName);
                                continue;
                            }

                            using var stream = new MemoryStream(contentBytes);
                            var sharePointUrl = await sharePointService.UploadDocumentAsync(
                                tenant.TenantId, stream, fileName);

                            var document = new Doario.Data.Models.Mail.Document
                            {
                                DocumentId = Guid.NewGuid(),
                                TenantId = tenant.TenantId,
                                OriginalFileName = fileName,
                                SharePointUrl = sharePointUrl,
                                DocumentStatusId = 1,
                                SenderTypeId = tenant.UnknownSenderTypeId,
                                SenderId = tenant.UnknownSenderId,
                                UploadedByStaffId = tenant.SystemStaffId,
                                SourceTypeId = sourceTypeId,
                                UploadedAt = message.ReceivedDateTime?.UtcDateTime ?? DateTime.UtcNow,
                            };

                            await documentRepo.CreateAsync(document);
                            ocrService.RunInBackground(document.DocumentId);
                            processed++;

                            SetState(status.InboxId, InboxJobState.Processing, processed);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "ProcessInboxQueue: failed processing message {Id}", message.Id);
                    }
                }

                await monitoredInboxRepo.UpdateLastProcessedAtAsync(status.InboxId, now);
                DoarioBackgroundService.LastFetchCounts[status.InboxId] = processed;
                SetState(status.InboxId, InboxJobState.Done, processed);

                _logger.LogInformation(
                    "ProcessInboxQueue: inbox {Email} done — {Count} documents",
                    inbox.EmailAddress, processed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "ProcessInboxQueue: inbox {InboxId} failed", status.InboxId);
                SetState(status.InboxId, InboxJobState.Failed, error: ex.Message);
            }
        }

        _isRunning = false;
        _logger.LogInformation("ProcessInboxQueue: all inboxes processed.");
    }

    private void SetState(Guid inboxId, InboxJobState state, int? docs = null, string error = null)
    {
        lock (_lock)
        {
            var s = _statuses.FirstOrDefault(x => x.InboxId == inboxId);
            if (s == null) return;
            s.State = state;
            if (docs.HasValue) s.DocumentsProcessed = docs.Value;
            if (error != null) s.Error = error;
        }
    }
}

public class InboxJobStatus
{
    public Guid InboxId { get; set; }
    public string EmailAddress { get; set; }
    public InboxJobState State { get; set; }
    public int DocumentsProcessed { get; set; }
    public string Error { get; set; }
}

public enum InboxJobState
{
    Waiting,
    Processing,
    Done,
    Failed
}

public class ProcessInboxJobResult
{
    public bool IsRunning { get; set; }
    public DateTime StartedAt { get; set; }
    public List<InboxJobStatus> Inboxes { get; set; } = new();
}