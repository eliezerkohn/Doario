using Doario.Data;
using Doario.Data.Models.Mail;
using Doario.Data.Repositories;
using Doario.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Doario.Web.Services;

public class DoarioBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<DoarioBackgroundService> _logger;
    private readonly AiProcessingQueue _aiQueue;
    private readonly ProcessInboxQueue _processInboxQueue;

    private static readonly string[] AllowedAttachmentTypes = new[]
    {
        ".pdf", ".jpg", ".jpeg", ".png", ".tiff", ".tif"
    };

    public static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, int> LastFetchCounts = new();
    public static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, bool> ForcedInboxIds = new();

    private DateTime _lastStuckCheck = DateTime.MinValue;
    private static readonly TimeSpan StuckCheckInterval = TimeSpan.FromMinutes(5);

    public DoarioBackgroundService(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        AiProcessingQueue aiQueue,
        ProcessInboxQueue processInboxQueue,
        ILogger<DoarioBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _aiQueue = aiQueue;
        _processInboxQueue = processInboxQueue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var wakeUpSeconds = _config.GetValue<int>("BackgroundService:WakeUpIntervalSeconds", 30);
        _logger.LogInformation("DoarioBackgroundService started. Wake-up interval: {Seconds}s", wakeUpSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ProcessAllAsync(); }
            catch (Exception ex) { _logger.LogError(ex, "DoarioBackgroundService: unhandled error."); }
            await Task.Delay(TimeSpan.FromSeconds(wakeUpSeconds), stoppingToken);
        }
    }

    private async Task ProcessAllAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var monitoredInboxRepo = scope.ServiceProvider.GetRequiredService<ITenantMonitoredInboxRepository>();
        var inboxSettingsRepo = scope.ServiceProvider.GetRequiredService<ITenantInboxSettingsRepository>();
        var now = DateTime.UtcNow;

        var activeInboxes = await monitoredInboxRepo.GetAllActiveAsync();
        foreach (var inbox in activeInboxes)
        {
            var secondsSinceLastPoll = (now - inbox.LastProcessedAt).TotalSeconds;
            var isForced = ForcedInboxIds.TryRemove(inbox.TenantMonitoredInboxId, out _);

            if (isForced || secondsSinceLastPoll >= inbox.PollingIntervalSeconds)
            {
                var queueStatus = _processInboxQueue.GetStatus(inbox.TenantId.ToString());
                var alreadyProcessing = queueStatus.IsRunning &&
                    queueStatus.Inboxes.Any(i =>
                        i.InboxId == inbox.TenantMonitoredInboxId &&
                        i.State == InboxJobState.Processing);

                if (alreadyProcessing)
                {
                    _logger.LogDebug("DoarioBackgroundService: skipping {Email} — already in ProcessInboxQueue.", inbox.EmailAddress);
                    continue;
                }

                await ProcessInboxAsync(scope, inbox, monitoredInboxRepo, now);
            }
        }

        var allSettings = await inboxSettingsRepo.GetAllAsync();
        foreach (var settings in allSettings)
        {
            var hoursSinceLastSync = (now - settings.LastStaffSyncAt).TotalHours;
            if (hoursSinceLastSync >= settings.StaffSyncIntervalHours)
                await SyncStaffAsync(scope, settings, inboxSettingsRepo, now);
        }

        await FlushBillingUsageAsync(scope);

        if (now - _lastStuckCheck >= StuckCheckInterval)
        {
            await RecoverStuckDocumentsAsync(scope);
            _lastStuckCheck = now;
        }
    }

    private async Task RecoverStuckDocumentsAsync(IServiceScope scope)
    {
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<DoarioDataContext>();
            var ocrService = scope.ServiceProvider.GetRequiredService<OcrService>();
            var now = DateTime.UtcNow;

            var stuckPreOcr = await db.Documents
                .Where(d => d.DocumentStatusId == 1
                         && (d.OcrText == null || d.OcrText == "")
                         && d.UploadedAt < now.AddMinutes(-10)
                         && d.SharePointUrl != null && d.SharePointUrl != "")
                .Select(d => d.DocumentId).ToListAsync();
            if (stuckPreOcr.Any()) foreach (var id in stuckPreOcr) ocrService.RetryOcr(id);

            var ocrFailed = await db.Documents
                .Where(d => d.DocumentStatusId == 5
                         && d.UploadedAt < now.AddMinutes(-30)
                         && d.SharePointUrl != null && d.SharePointUrl != "")
                .Select(d => d.DocumentId).ToListAsync();
            if (ocrFailed.Any()) foreach (var id in ocrFailed) ocrService.RetryOcr(id);

            var stuckAi = await db.Documents
                .Where(d => d.OcrText != null && d.OcrText != ""
                         && (d.AiSummary == null || d.AiSummary == "")
                         && d.UploadedAt < now.AddMinutes(-5))
                .Select(d => d.DocumentId).ToListAsync();
            if (stuckAi.Any()) _aiQueue.EnqueueBatch(stuckAi);
        }
        catch (Exception ex) { _logger.LogError(ex, "DoarioBackgroundService: stuck document recovery failed."); }
    }

    private async Task FlushBillingUsageAsync(IServiceScope scope)
    {
        try
        {
            var stripeService = scope.ServiceProvider.GetRequiredService<StripeService>();
            await stripeService.FlushAllPendingUsageAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "DoarioBackgroundService: Stripe usage flush failed."); }
    }

    private async Task ProcessInboxAsync(
        IServiceScope scope,
        Doario.Data.Models.SaaS.TenantMonitoredInbox inbox,
        ITenantMonitoredInboxRepository monitoredInboxRepo,
        DateTime now)
    {
        var tenantRepo = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
        var tenant = await tenantRepo.GetByIdAsync(inbox.TenantId);
        if (tenant == null) return;

        try
        {
            var graph = scope.ServiceProvider.GetRequiredService<GraphServiceClient>();
            var documentRepo = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
            var sharePointService = scope.ServiceProvider.GetRequiredService<SharePointService>();
            var ocrService = scope.ServiceProvider.GetRequiredService<OcrService>();
            var errorLogRepo = scope.ServiceProvider.GetRequiredService<IErrorLogRepository>();

            var fetchedAt = DateTime.UtcNow;
            var filterDate = inbox.LastProcessedAt.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var filter = $"receivedDateTime ge {filterDate}";

            var allMessages = new List<Message>();

            var firstPage = await graph.Users[inbox.EmailAddress]
                .Messages
                .GetAsync(req =>
                {
                    req.QueryParameters.Filter = filter;
                    req.QueryParameters.Select = new[] { "id", "subject", "from", "receivedDateTime", "hasAttachments" };
                    req.QueryParameters.Top = 50;
                });

            if (firstPage?.Value != null)
            {
                allMessages.AddRange(firstPage.Value);
                var pageIterator = PageIterator<Message, MessageCollectionResponse>
                    .CreatePageIterator(graph, firstPage, msg => { allMessages.Add(msg); return true; });
                await pageIterator.IterateAsync();
            }

            if (allMessages.Count == 0)
            {
                LastFetchCounts[inbox.TenantMonitoredInboxId] = 0;
                await monitoredInboxRepo.UpdateLastProcessedAtAsync(inbox.TenantMonitoredInboxId, now);
                return;
            }

            var processed = 0;
            foreach (var message in allMessages)
            {
                if (message.HasAttachments != true) continue;
                await ProcessEmailAsync(graph, message, tenant, inbox, documentRepo,
                    sharePointService, ocrService, errorLogRepo, fetchedAt);
                processed++;
            }

            await monitoredInboxRepo.UpdateLastProcessedAtAsync(inbox.TenantMonitoredInboxId, fetchedAt);
            LastFetchCounts[inbox.TenantMonitoredInboxId] = processed;

            _logger.LogInformation("DoarioBackgroundService: inbox {Inbox} — {Total} emails, {Processed} processed.",
                inbox.EmailAddress, allMessages.Count, processed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DoarioBackgroundService: inbox processing failed for {Inbox}", inbox.EmailAddress);
        }
    }

    private async Task ProcessEmailAsync(
        GraphServiceClient graph,
        Message message,
        Doario.Data.Models.SaaS.Tenant tenant,
        Doario.Data.Models.SaaS.TenantMonitoredInbox inbox,
        IDocumentRepository documentRepo,
        SharePointService sharePointService,
        OcrService ocrService,
        IErrorLogRepository errorLogRepo,
        DateTime fetchedAt)
    {
        try
        {
            var sourceTypeId = inbox.IsFaxInbox ? 10 : 11;
            var attachments = await graph.Users[inbox.EmailAddress]
                .Messages[message.Id].Attachments.GetAsync();

            if (attachments?.Value == null) return;

            foreach (var attachment in attachments.Value)
            {
                if (attachment is not FileAttachment fileAttachment) continue;
                var ext = Path.GetExtension(fileAttachment.Name ?? "").ToLowerInvariant();
                if (!AllowedAttachmentTypes.Contains(ext)) continue;
                var contentBytes = fileAttachment.ContentBytes;
                if (contentBytes == null || contentBytes.Length == 0) continue;

                var receivedTime = (message.ReceivedDateTime ?? DateTimeOffset.UtcNow)
                    .ToString("yyyyMMdd_HHmmss");
                var baseName = SanitiseFileName(
                    Path.GetFileNameWithoutExtension(fileAttachment.Name ?? "document"));

                // Format: baseName_timestamp.ext
                var fileName = $"{baseName}_{receivedTime}{ext}";

                var alreadyExists = await documentRepo.ExistsByFileNameAsync(tenant.TenantId, fileName);
                if (alreadyExists)
                {
                    _logger.LogDebug("DoarioBackgroundService: skipping duplicate {FileName}", fileName);
                    continue;
                }

                using var stream = new MemoryStream(contentBytes);
                var sharePointUrl = await sharePointService.UploadDocumentAsync(tenant.TenantId, stream, fileName);

                var document = new Document
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
                    FetchedAt = fetchedAt,
                    MonitoredInboxId = inbox.TenantMonitoredInboxId,
                };

                await documentRepo.CreateAsync(document);
                ocrService.RunInBackground(document.DocumentId);

                _logger.LogInformation("DoarioBackgroundService: saved {File} for tenant {TenantId}", fileName, tenant.TenantId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DoarioBackgroundService: failed to process email {MessageId}", message.Id);
            try
            {
                await errorLogRepo.AddAsync(new ErrorLog
                {
                    ErrorLogId = Guid.NewGuid(),
                    TenantId = tenant.TenantId,
                    ErrorType = "InboxProcessing",
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    CreatedAt = DateTime.UtcNow,
                });
            }
            catch { }
        }
    }

    private async Task SyncStaffAsync(
        IServiceScope scope,
        Doario.Data.Models.SaaS.TenantInboxSettings settings,
        ITenantInboxSettingsRepository inboxRepo,
        DateTime now)
    {
        try
        {
            var staffSyncService = scope.ServiceProvider.GetRequiredService<StaffSyncService>();
            var result = await staffSyncService.SyncAsync(settings.TenantId);
            if (result.Success)
            {
                await inboxRepo.UpdateLastStaffSyncAtAsync(settings.TenantId, now);
                _logger.LogInformation("DoarioBackgroundService: staff sync complete for {TenantId}. Added {Added}, Updated {Updated}",
                    settings.TenantId, result.Added, result.Updated);
            }
            else
            {
                _logger.LogWarning("DoarioBackgroundService: staff sync failed for {TenantId}: {Error}",
                    settings.TenantId, result.ErrorMessage);
            }
        }
        catch (Exception ex) { _logger.LogError(ex, "DoarioBackgroundService: staff sync threw for {TenantId}", settings.TenantId); }
    }

    private static string SanitiseFileName(string input)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var clean = new string(input
            .Where(c => !invalid.Contains(c) && c != ' ')
            .Take(60)
            .ToArray());
        return string.IsNullOrWhiteSpace(clean) ? "document" : clean;
    }
}