using Azure;
using Azure.AI.DocumentIntelligence;
using Doario.Data;
using Doario.Data.Models.SaaS;
using Doario.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Graph;

namespace Doario.Web.Services;

public class OcrService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DocumentIntelligenceClient _docIntelligence;
    private readonly GraphServiceClient _graph;
    private readonly ILogger<OcrService> _logger;
    private readonly AiProcessingQueue _aiQueue;

    public OcrService(
        IServiceScopeFactory scopeFactory,
        IOptions<OcrOptions> ocrOptions,
        GraphServiceClient graph,
        AiProcessingQueue aiQueue,
        ILogger<OcrService> logger)
    {
        _scopeFactory = scopeFactory;
        _graph = graph;
        _logger = logger;
        _aiQueue = aiQueue;
        _docIntelligence = new DocumentIntelligenceClient(
            new Uri(ocrOptions.Value.Endpoint),
            new AzureKeyCredential(ocrOptions.Value.ApiKey));
    }

    // ── Full OCR — runs after document is confirmed and saved ─────────────────
    // Downloads from SharePoint, extracts all text, fires AI summary.
    // On failure — sets document status to 5 (OcrFailed) so it can be retried.

    public void RunInBackground(Guid documentId)
    {
        Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var documents = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();

                var doc = await documents.GetByIdAsync(documentId);
                if (doc is null)
                {
                    _logger.LogWarning("OcrService: Document {Id} not found.", documentId);
                    return;
                }

                if (string.IsNullOrEmpty(doc.SharePointUrl))
                {
                    _logger.LogWarning("OcrService: Document {Id} has no SharePointUrl.", documentId);
                    await documents.UpdateStatusAsync(documentId, 5); // OcrFailed
                    return;
                }

                var fileStream = await DownloadFromSharePointAsync(doc.SharePointUrl);
                if (fileStream is null)
                {
                    _logger.LogWarning("OcrService: Could not download file for Document {Id}.", documentId);
                    await documents.UpdateStatusAsync(documentId, 5); // OcrFailed
                    return;
                }

                var operation = await _docIntelligence.AnalyzeDocumentAsync(
                    WaitUntil.Completed,
                    "prebuilt-read",
                    BinaryData.FromStream(fileStream));

                var result = operation.Value;

                var pageLines = result.Pages.Select(page =>
                    string.Join(Environment.NewLine,
                        page.Lines?.Select(l => l.Content) ?? []));

                var extractedText = string.Join(
                    Environment.NewLine + Environment.NewLine,
                    pageLines);

                if (result.Tables?.Count > 0)
                {
                    var tableText = string.Join(
                        Environment.NewLine,
                        result.Tables
                              .SelectMany(t => t.Cells)
                              .Select(c => c.Content));

                    extractedText += Environment.NewLine
                                   + Environment.NewLine
                                   + "=== TABLE CONTENT ==="
                                   + Environment.NewLine
                                   + tableText;
                }

                await documents.UpdateOcrTextAsync(documentId, extractedText.Trim());

                _logger.LogInformation(
                    "OcrService: OCR complete. Document {Id}, Characters {Count}",
                    documentId, extractedText.Length);

                // Record billable usage for this document
                await RecordBillingUsageAsync(scope, doc.TenantId, documentId);

                // Use queue for rate-limit-safe parallel processing
                _aiQueue.Enqueue(documentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OcrService: OCR failed for Document {Id}.", documentId);

                // Mark as OcrFailed (status 5) so background service can detect and retry
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var documents = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
                    await documents.UpdateStatusAsync(documentId, 5);
                }
                catch (Exception statusEx)
                {
                    _logger.LogError(statusEx,
                        "OcrService: Failed to set OcrFailed status for Document {Id}.", documentId);
                }
            }
        });
    }

    // ── Retry OCR for a document — resets status and reruns ──────────────────
    // Called by background service when retrying stuck/failed OCR documents.

    public void RetryOcr(Guid documentId)
    {
        Task.Run(async () =>
        {
            try
            {
                // Reset status to Unassigned before retrying
                using var scope = _scopeFactory.CreateScope();
                var documents = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
                await documents.UpdateStatusAsync(documentId, 1);
            }
            catch { }
        });

        RunInBackground(documentId);
    }

    // ── Record one billable document usage event ──────────────────────────────

    private async Task RecordBillingUsageAsync(IServiceScope scope, Guid tenantId, Guid documentId)
    {
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<DoarioDataContext>();

            var alreadyRecorded = await db.TenantBillingUsages
                .AnyAsync(u => u.DocumentId == documentId);

            if (alreadyRecorded)
            {
                _logger.LogDebug("OcrService: Billing usage already recorded for Document {Id}.", documentId);
                return;
            }

            db.TenantBillingUsages.Add(new TenantBillingUsage
            {
                TenantBillingUsageId = Guid.NewGuid(),
                TenantId = tenantId,
                DocumentId = documentId,
                RecordedAt = DateTime.UtcNow,
                ReportedToStripe = false,
                Quantity = 1
            });

            await db.SaveChangesAsync();

            _logger.LogInformation(
                "OcrService: Billing usage recorded for Document {Id}, Tenant {TenantId}.",
                documentId, tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OcrService: Failed to record billing usage for Document {Id}.", documentId);
        }
    }

    // ── Quick OCR on a raw base64 PNG ─────────────────────────────────────────

    public async Task<string> OcrPageAsync(string base64Image)
    {
        if (string.IsNullOrWhiteSpace(base64Image))
            return string.Empty;

        try
        {
            var imageBytes = Convert.FromBase64String(base64Image);
            using var stream = new MemoryStream(imageBytes);

            var operation = await _docIntelligence.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                "prebuilt-read",
                BinaryData.FromStream(stream));

            var result = operation.Value;

            var lines = result.Pages
                .SelectMany(p => p.Lines ?? [])
                .Select(l => l.Content)
                .Where(c => !string.IsNullOrWhiteSpace(c));

            var text = string.Join(" ", lines).Trim();

            _logger.LogDebug(
                "OcrService.OcrPageAsync: extracted {Chars} characters.",
                text.Length);

            return text;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OcrService.OcrPageAsync: OCR failed for page, treating as blank.");
            return string.Empty;
        }
    }

    // ── SharePoint download via Graph sharing URL ─────────────────────────────

    private async Task<Stream> DownloadFromSharePointAsync(string sharePointWebUrl)
    {
        try
        {
            var encodedUrl = EncodeSharingUrl(sharePointWebUrl);

            var driveItem = await _graph
                .Shares[encodedUrl]
                .DriveItem
                .GetAsync();

            if (driveItem?.Id is null || driveItem.ParentReference?.DriveId is null)
                return null;

            return await _graph
                .Drives[driveItem.ParentReference.DriveId]
                .Items[driveItem.Id]
                .Content
                .GetAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "OcrService: SharePoint download failed for URL {Url}.", sharePointWebUrl);
            return null;
        }
    }

    private static string EncodeSharingUrl(string url)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(url);
        var base64 = Convert.ToBase64String(bytes);
        return "u!" + base64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}