using Azure;
using Azure.AI.OpenAI;
using Doario.Web.Controllers;
using OpenAI.Chat;

namespace Doario.Web.Services;

/// <summary>
/// Uses Azure OpenAI to determine where document boundaries are within a batch scan.
/// Falls back to single-document (all pages = one document) if AI call fails.
/// </summary>
public class AiBatchSplitService
{
    private readonly IConfiguration _config;
    private readonly ILogger<AiBatchSplitService> _logger;

    public AiBatchSplitService(IConfiguration config, ILogger<AiBatchSplitService> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Given OCR text per page, returns a list of document boundaries.
    /// Each boundary is (StartPage, PageCount) — zero-based StartPage.
    /// </summary>
    public async Task<List<DocumentBoundary>> DetectBoundariesAsync(List<string> pageTexts)
    {
        if (pageTexts == null || pageTexts.Count == 0)
            return new List<DocumentBoundary>();

        if (pageTexts.Count == 1)
            return new List<DocumentBoundary>
            {
                new DocumentBoundary { Index = 0, StartPage = 0, PageCount = 1 }
            };

        try
        {
            // Build a compact representation of each page for the prompt
            var pagesSummary = pageTexts
                .Select((text, i) =>
                {
                    var snippet = string.IsNullOrWhiteSpace(text)
                        ? "[BLANK PAGE]"
                        : text.Length > 500 ? text[..500] + "…" : text;
                    return $"PAGE {i + 1}:\n{snippet}";
                });

            var pagesBlock = string.Join("\n\n---\n\n", pagesSummary);

            const string example =
                "[{\"startPage\":1,\"endPage\":2},{\"startPage\":4,\"endPage\":4},{\"startPage\":5,\"endPage\":5}]";

            var prompt = $"""
                You are analysing a batch scan from a physical mail room.
                The scan contains {pageTexts.Count} pages which may include multiple separate documents.

                Your task: identify where each new document starts.

                Rules:
                - A new document starts when the content clearly changes to a different letter, form, invoice, or document
                - Blank pages are separators — they do NOT belong to any document
                - Consecutive pages that are part of the same letter/form stay together
                - Every non-blank page must belong to exactly one document

                Here are the first 300 characters of text from each page:

                {pagesBlock}

                Respond with ONLY a JSON array. Each element is an object with:
                  "startPage": 1-based page number where this document starts
                  "endPage":   1-based page number where this document ends (inclusive)

                Example for a 5-page scan with 3 documents (page 3 was blank):
                {example}

                Return ONLY the JSON array. No explanation, no markdown, no code fences.
                """;

            var client = new AzureOpenAIClient(
                new Uri(_config["AzureOpenAI:Endpoint"]),
                new AzureKeyCredential(_config["AzureOpenAI:ApiKey"]));
            var chatClient = client.GetChatClient(_config["AzureOpenAI:DeploymentName"]);

            var response = await chatClient.CompleteChatAsync(new UserChatMessage(prompt));
            var raw = response.Value.Content[0].Text.Trim();

            // Strip markdown fences if model added them despite instructions
            raw = raw.Replace("```json", "").Replace("```", "").Trim();

            var parsed = System.Text.Json.JsonSerializer.Deserialize<List<AiBoundaryItem>>(raw);

            if (parsed == null || parsed.Count == 0)
                return FallbackSingleDocument(pageTexts.Count);

            var boundaries = parsed
                .Where(p => p.StartPage >= 1 && p.EndPage >= p.StartPage
                         && p.StartPage <= pageTexts.Count)
                .Select((p, i) => new DocumentBoundary
                {
                    Index = i,
                    StartPage = p.StartPage - 1,               // convert to 0-based
                    PageCount = Math.Min(p.EndPage, pageTexts.Count) - p.StartPage + 1
                })
                .Where(b => b.PageCount > 0)
                .ToList();

            if (boundaries.Count == 0)
                return FallbackSingleDocument(pageTexts.Count);

            _logger.LogInformation(
                "AiBatchSplitService: detected {Count} document(s) in {Pages} pages.",
                boundaries.Count, pageTexts.Count);

            return boundaries;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "AiBatchSplitService: AI split failed, falling back to single document.");
            return FallbackSingleDocument(pageTexts.Count);
        }
    }

    private static List<DocumentBoundary> FallbackSingleDocument(int pageCount)
        => new List<DocumentBoundary>
        {
            new DocumentBoundary { Index = 0, StartPage = 0, PageCount = pageCount }
        };

    private class AiBoundaryItem
    {
        [System.Text.Json.Serialization.JsonPropertyName("startPage")]
        public int StartPage { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("endPage")]
        public int EndPage { get; set; }
    }
}