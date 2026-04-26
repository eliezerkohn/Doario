using Azure;
using Azure.AI.DocumentIntelligence;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Doario.Data.Repositories;

namespace Doario.Web.Services;

public class OcrService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DocumentIntelligenceClient _docIntelligence;
    private readonly GraphServiceClient _graph;
    private readonly ILogger<OcrService> _logger;

    public OcrService(
        IServiceScopeFactory scopeFactory,
        IOptions<OcrOptions> ocrOptions,
        GraphServiceClient graph,
        ILogger<OcrService> logger)
    {
        _scopeFactory = scopeFactory;
        _graph = graph;
        _logger = logger;
        _docIntelligence = new DocumentIntelligenceClient(
            new Uri(ocrOptions.Value.Endpoint),
            new AzureKeyCredential(ocrOptions.Value.ApiKey));
    }

    public void RunInBackground(Guid documentId)
    {
        Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var documents = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
                var aiSummaryService = scope.ServiceProvider.GetRequiredService<AiSummaryService>();

                var doc = await documents.GetByIdAsync(documentId);
                if (doc is null)
                {
                    _logger.LogWarning("OcrService: Document {Id} not found.", documentId);
                    return;
                }

                if (string.IsNullOrEmpty(doc.SharePointUrl))
                {
                    _logger.LogWarning("OcrService: Document {Id} has no SharePointUrl.", documentId);
                    return;
                }

                // Download from SharePoint via Graph sharing URL
                var fileStream = await DownloadFromSharePointAsync(doc.SharePointUrl);
                if (fileStream is null)
                {
                    _logger.LogWarning("OcrService: Could not download file for Document {Id}.", documentId);
                    return;
                }

                // Run OCR — same call as original working version
                var operation = await _docIntelligence.AnalyzeDocumentAsync(
                    WaitUntil.Completed,
                    "prebuilt-read",
                    BinaryData.FromStream(fileStream));

                var result = operation.Value;

                // Build extracted text from all pages
                var pageLines = result.Pages.Select(page =>
                    string.Join(Environment.NewLine,
                        page.Lines?.Select(l => l.Content) ?? []));

                var extractedText = string.Join(
                    Environment.NewLine + Environment.NewLine,
                    pageLines);

                // Append table content if present
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

                // Fire AI summary in background
                aiSummaryService.RunInBackground(documentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OcrService: OCR failed for Document {Id}.", documentId);
            }
        });
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
            _logger.LogError(ex, "OcrService: SharePoint download failed for URL {Url}.", sharePointWebUrl);
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