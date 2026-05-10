using Doario.Data.Models.Mail;
using Doario.Data.Repositories;

namespace Doario.Web.Services;

/// <summary>
/// Singleton queue that processes scan confirmations server-side.
/// Browser reloads don't affect processing — runs until all documents are saved.
/// Each tenant gets its own job slot — keyed by tenantId.
/// </summary>
public class ScanConfirmQueue
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScanConfirmQueue> _logger;

    // One active job per tenant
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, ScanConfirmJob> _jobs = new();

    public ScanConfirmQueue(IServiceScopeFactory scopeFactory, ILogger<ScanConfirmQueue> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public ScanConfirmJobResult GetStatus(Guid tenantId)
    {
        if (_jobs.TryGetValue(tenantId, out var job))
        {
            lock (job)
            {
                return new ScanConfirmJobResult
                {
                    IsRunning = job.IsRunning,
                    StartedAt = job.StartedAt,
                    Documents = job.Documents.Select(d => new ScanConfirmDocStatus
                    {
                        TempId = d.TempId,
                        Index = d.Index,
                        State = d.State,
                        DocumentId = d.DocumentId,
                        SharePointUrl = d.SharePointUrl,
                        Error = d.Error,
                    }).ToList(),
                };
            }
        }
        return new ScanConfirmJobResult { IsRunning = false, Documents = new() };
    }

    /// <summary>
    /// Starts confirming a batch of documents server-side.
    /// Returns false if a job is already running for this tenant.
    /// </summary>
    public bool Start(Guid tenantId, string apiKey, List<ScanConfirmRequest> documents)
    {
        if (_jobs.TryGetValue(tenantId, out var existing) && existing.IsRunning)
            return false;

        var job = new ScanConfirmJob
        {
            IsRunning = true,
            StartedAt = DateTime.UtcNow,
            Documents = documents.Select(d => new ScanConfirmDocStatus
            {
                TempId = d.TempId,
                Index = d.DocumentIndex,
                State = ScanConfirmState.Waiting,
                Pages = d.Pages,
                BatchScanId = d.BatchScanId,
                PageStart = d.PageStart,
                PageEnd = d.PageEnd,
            }).ToList(),
        };

        _jobs[tenantId] = job;

        _ = Task.Run(() => ProcessAsync(tenantId, apiKey, job));
        return true;
    }

    private async Task ProcessAsync(Guid tenantId, string apiKey, ScanConfirmJob job)
    {
        _logger.LogInformation(
            "ScanConfirmQueue: starting {Count} documents for tenant {TenantId}",
            job.Documents.Count, tenantId);

        foreach (var doc in job.Documents)
        {
            // Skip already confirmed
            if (doc.State == ScanConfirmState.Done) continue;

            SetState(job, doc.TempId, ScanConfirmState.Processing);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var tenantRepo = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
                var documentRepo = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
                var sharePointService = scope.ServiceProvider.GetRequiredService<SharePointService>();
                var pdfService = scope.ServiceProvider.GetRequiredService<PdfService>();
                var ocrService = scope.ServiceProvider.GetRequiredService<OcrService>();

                var tenant = await tenantRepo.GetByApiKeyAsync(apiKey);
                if (tenant == null)
                {
                    SetState(job, doc.TempId, ScanConfirmState.Failed, error: "Invalid API key.");
                    continue;
                }

                var fileName = $"scan_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{doc.Index + 1}.pdf";

                using var pdfStream = pdfService.BuildPdfStream(doc.Pages);
                var sharePointUrl = await sharePointService.UploadDocumentAsync(
                    tenant.TenantId, pdfStream, fileName);

                var batchScanId = string.IsNullOrWhiteSpace(doc.BatchScanId)
                    ? (Guid?)null
                    : Guid.TryParse(doc.BatchScanId, out var g) ? g : (Guid?)null;

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
                    BatchScanId = batchScanId,
                    BatchPageStart = doc.PageStart,
                    BatchPageEnd = doc.PageEnd,
                    UploadedAt = DateTime.UtcNow,
                };

                await documentRepo.CreateAsync(document);
                ocrService.RunInBackground(document.DocumentId);

                SetState(job, doc.TempId, ScanConfirmState.Done,
                    documentId: document.DocumentId,
                    sharePointUrl: sharePointUrl);

                _logger.LogInformation(
                    "ScanConfirmQueue: confirmed doc {Index} for tenant {TenantId} — {DocId}",
                    doc.Index, tenantId, document.DocumentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "ScanConfirmQueue: failed to confirm doc {Index} for tenant {TenantId}",
                    doc.Index, tenantId);
                SetState(job, doc.TempId, ScanConfirmState.Failed, error: ex.Message);
            }
        }

        lock (job) { job.IsRunning = false; }

        _logger.LogInformation(
            "ScanConfirmQueue: all documents processed for tenant {TenantId}", tenantId);
    }

    private void SetState(ScanConfirmJob job, string tempId, ScanConfirmState state,
        Guid? documentId = null, string sharePointUrl = null, string error = null)
    {
        lock (job)
        {
            var doc = job.Documents.FirstOrDefault(d => d.TempId == tempId);
            if (doc == null) return;
            doc.State = state;
            if (documentId.HasValue) doc.DocumentId = documentId.Value;
            if (sharePointUrl != null) doc.SharePointUrl = sharePointUrl;
            if (error != null) doc.Error = error;
        }
    }
}

// ── Job models ────────────────────────────────────────────────────────────────

public class ScanConfirmJob
{
    public bool IsRunning { get; set; }
    public DateTime StartedAt { get; set; }
    public List<ScanConfirmDocStatus> Documents { get; set; } = new();
}

public class ScanConfirmDocStatus
{
    public string TempId { get; set; }
    public int Index { get; set; }
    public ScanConfirmState State { get; set; }
    public Guid DocumentId { get; set; }
    public string SharePointUrl { get; set; }
    public string Error { get; set; }

    // Internal — not returned to client
    public List<string> Pages { get; set; }
    public string BatchScanId { get; set; }
    public int? PageStart { get; set; }
    public int? PageEnd { get; set; }
}

public enum ScanConfirmState { Waiting, Processing, Done, Failed }

public class ScanConfirmJobResult
{
    public bool IsRunning { get; set; }
    public DateTime StartedAt { get; set; }
    public List<ScanConfirmDocStatus> Documents { get; set; } = new();
}

public class ScanConfirmRequest
{
    public string TempId { get; set; }
    public List<string> Pages { get; set; }
    public string BatchScanId { get; set; }
    public int DocumentIndex { get; set; }
    public int? PageStart { get; set; }
    public int? PageEnd { get; set; }
}