using Azure;
using Azure.AI.DocumentIntelligence;
using Doario.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Graph;

namespace Doario.Web.Services;

public class OcrService
{
    private readonly DoarioDataContext _db;
    private readonly GraphServiceClient _graph;
    private readonly DocumentIntelligenceClient _docIntelligence;
    private readonly ILogger<OcrService> _logger;
    private readonly AiSummaryService _aiSummary;

    public OcrService(
        DoarioDataContext db,
        GraphServiceClient graph,
        IOptions<OcrOptions> ocrOptions,
        ILogger<OcrService> logger,
        AiSummaryService aiSummary)
    {
        _db = db;
        _graph = graph;
        _logger = logger;
        _aiSummary = aiSummary;

        _docIntelligence = new DocumentIntelligenceClient(
            new Uri(ocrOptions.Value.Endpoint),
            new AzureKeyCredential(ocrOptions.Value.ApiKey));
    }

    public async Task RunOcrAsync(Guid documentId)
    {
        var document = await _db.Documents
            .FirstOrDefaultAsync(d => d.DocumentId == documentId);

        if (document is null)
        {
            _logger.LogWarning("OcrService: DocumentId {Id} not found.", documentId);
            return;
        }

        if (string.IsNullOrEmpty(document.SharePointUrl))
        {
            _logger.LogWarning(
                "OcrService: DocumentId {Id} has no SharePointUrl.", documentId);
            return;
        }

        try
        {
            // Download file from SharePoint
            var fileStream = await DownloadFromSharePointAsync(document.SharePointUrl);

            if (fileStream is null)
            {
                _logger.LogWarning(
                    "OcrService: Could not download file for DocumentId {Id}.", documentId);
                return;
            }

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

            // Save to Document row
            document.OcrText = extractedText.Trim();
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "OcrService: OCR complete. DocumentId={Id}, Characters={Count}",
                documentId, document.OcrText?.Length ?? 0);

            // Fire AI summary in background after OCR completes
            _aiSummary.RunInBackground(documentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "OcrService: OCR failed for DocumentId {Id}.",
                documentId);
            // Never rethrow — upload already succeeded
        }
    }

    private async Task<Stream?> DownloadFromSharePointAsync(string sharePointWebUrl)
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

            var stream = await _graph
                .Drives[driveItem.ParentReference.DriveId]
                .Items[driveItem.Id]
                .Content
                .GetAsync();

            return stream;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "OcrService: SharePoint download failed. URL: {Url}",
                sharePointWebUrl);
            return null;
        }
    }

    private static string EncodeSharingUrl(string url)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(url);
        var base64 = Convert.ToBase64String(bytes);
        var encoded = "u!" + base64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return encoded;
    }
}