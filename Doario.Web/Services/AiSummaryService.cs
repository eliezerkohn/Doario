using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using Doario.Data.Repositories;

namespace Doario.Web.Services;

public class AiSummaryService
{
    private readonly IConfiguration _config;
    private readonly IServiceScopeFactory _scopeFactory;

    public AiSummaryService(IConfiguration config, IServiceScopeFactory scopeFactory)
    {
        _config = config;
        _scopeFactory = scopeFactory;
    }

    public void RunInBackground(Guid documentId)
    {
        Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var documents = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
                var feedbackRepo = scope.ServiceProvider.GetRequiredService<IDocumentFeedbackRepository>();
                var whitelistRepo = scope.ServiceProvider.GetRequiredService<ITenantWhitelistedSenderRepository>();

                var doc = await documents.GetByIdAsync(documentId);
                if (doc is null || string.IsNullOrWhiteSpace(doc.OcrText)) return;

                // ── Step 1: Whitelist check ───────────────────────────────────────────
                // If this sender was previously marked "Not Spam / Not Promotion" by admin,
                // skip AI classification entirely — always treat as real mail.
                var isWhitelisted = await whitelistRepo.IsWhitelistedAsync(doc.TenantId, doc.OcrText);

                // ── Step 2: Load corrections — two sources ────────────────────────────
                //
                // Source A: Sender-specific corrections
                //   ALL corrections where the stored snippet shares keywords with
                //   the current document. If MedLine was corrected twice before,
                //   both corrections are included regardless of how many others exist.
                //   These are the most important — they teach the AI about THIS sender.
                //
                // Source B: General recent corrections (last 5)
                //   The most recent corrections for anything else at this tenant.
                //   These teach the AI about document types it hasn't seen from this sender.
                //
                // The two lists are merged and deduplicated by ID.

                var senderCorrections = await feedbackRepo.GetRelevantForSenderAsync(
                    doc.TenantId, doc.OcrText);

                var recentCorrections = await feedbackRepo.GetRecentForTenantAsync(
                    doc.TenantId, 5);

                // Merge: sender-specific first (highest priority), then recent general ones
                var allCorrections = senderCorrections
                    .Concat(recentCorrections)
                    .GroupBy(c => c.DocumentFeedbackId)
                    .Select(g => g.First())
                    .ToList();

                // Build correction text for the prompt
                string correctionExamples;
                if (allCorrections.Any())
                {
                    var lines = new List<string>();

                    // Sender-specific corrections get a stronger message
                    foreach (var c in senderCorrections.Take(5))
                    {
                        var snippet = c.DocumentSnippet?[..Math.Min(120, c.DocumentSnippet?.Length ?? 0)];
                        lines.Add($"- IMPORTANT: A document from this same sender " +
                                  $"(starting with \"{snippet}\") was previously classified as " +
                                  $"\"{c.AiClassification}\" but the admin confirmed it is " +
                                  $"\"{c.CorrectedClassification}\". Apply this correction.");
                    }

                    // General corrections get a lighter message
                    foreach (var c in recentCorrections
                        .Where(r => !senderCorrections.Any(s => s.DocumentFeedbackId == r.DocumentFeedbackId))
                        .Take(5))
                    {
                        var snippet = c.DocumentSnippet?[..Math.Min(100, c.DocumentSnippet?.Length ?? 0)];
                        lines.Add($"- A document starting with \"{snippet}\" was incorrectly " +
                                  $"classified as \"{c.AiClassification}\" but is actually " +
                                  $"\"{c.CorrectedClassification}\".");
                    }

                    correctionExamples = "\n\nLEARNING FROM PAST CORRECTIONS — apply these:\n" +
                                         string.Join("\n", lines);
                }
                else
                {
                    correctionExamples = string.Empty;
                }

                // ── Step 3: Call Azure OpenAI ─────────────────────────────────────────
                var client = new AzureOpenAIClient(
                    new Uri(_config["AzureOpenAI:Endpoint"]),
                    new AzureKeyCredential(_config["AzureOpenAI:ApiKey"]));
                var chatClient = client.GetChatClient(_config["AzureOpenAI:DeploymentName"]);

                var prompt = $"""
                    You are an assistant that analyses physical mail documents for an office mail room system.

                    Read the following OCR text and do THREE things:

                    STEP 1 — Classify the document into exactly one of these categories:
                    - mail         (a real document needing staff attention)
                    - promotion    (marketing material, advertisements, offers)
                    - spam         (junk mail, unsolicited bulk mail)
                    {correctionExamples}

                    STEP 2 — Rate your confidence in this classification from 1 to 10.
                    10 = absolutely certain. 1 = complete guess.
                    Be conservative — if there is any doubt, score lower.

                    STEP 3 — Write a structured summary on a SINGLE LINE (no line breaks) using EXACTLY
                    this format for ALL categories including spam and promotion:
                    <strong>Document Type:</strong> [type] <strong>Sender:</strong> [name or company] <strong>Purpose:</strong> [main subject] <strong>Action Required:</strong> [action or None] <strong>Key Details:</strong> [relevant details or None]

                    Return ONLY these three lines, nothing else:
                    CATEGORY: [mail|promotion|spam]
                    CONFIDENCE: [1-10]
                    SUMMARY: [your single-line summary here]

                    OCR TEXT:
                    {doc.OcrText}
                    """;

                var response = await chatClient.CompleteChatAsync(new UserChatMessage(prompt));
                var raw = response.Value.Content[0].Text.Trim();

                // ── Step 4: Parse response ────────────────────────────────────────────
                var category = "mail";
                var confidence = 0;
                var summary = string.Empty;

                foreach (var line in raw.Split('\n'))
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("CATEGORY:", StringComparison.OrdinalIgnoreCase))
                        category = trimmed.Substring(9).Trim().ToLowerInvariant();
                    else if (trimmed.StartsWith("CONFIDENCE:", StringComparison.OrdinalIgnoreCase))
                        int.TryParse(trimmed.Substring(11).Trim(), out confidence);
                    else if (trimmed.StartsWith("SUMMARY:", StringComparison.OrdinalIgnoreCase))
                        summary = trimmed.Substring(8).Trim();
                }

                if (string.IsNullOrWhiteSpace(summary))
                    summary = raw;

                // ── Step 5: Format summary with line breaks between fields ─────────────
                var html = summary
                    .Replace("<strong>Sender:", "<br><strong>Sender:")
                    .Replace("<strong>Purpose:", "<br><strong>Purpose:")
                    .Replace("<strong>Action Required:", "<br><strong>Action Required:")
                    .Replace("<strong>Key Details:", "<br><strong>Key Details:");

                await documents.UpdateAiSummaryAsync(documentId, html);

                // ── Step 6: Decide folder ─────────────────────────────────────────────
                //
                // Decision tree:
                //   Whitelisted sender     → always Unassigned (1)
                //   AI says mail           → Unassigned (1)
                //   AI says spam/promo
                //     Confidence >= 8      → move to Spam (7) or Promotions (8)
                //     Confidence < 8       → Unassigned (1) — admin decides
                //
                int statusId;

                if (isWhitelisted || category == "mail")
                {
                    statusId = 1;
                    if (isWhitelisted)
                        Console.WriteLine($"AiSummaryService: document {documentId} — " +
                                          "sender is whitelisted, forced to Unassigned.");
                }
                else if (confidence >= 8)
                {
                    statusId = category switch
                    {
                        "spam" => 7,
                        "promotion" => 8,
                        _ => 1
                    };
                    Console.WriteLine($"AiSummaryService: document {documentId} — " +
                                      $"classified as {category} with confidence {confidence}, " +
                                      $"moved to status {statusId}.");
                }
                else
                {
                    // AI not confident enough — leave for admin
                    statusId = 1;
                    Console.WriteLine($"AiSummaryService: document {documentId} — " +
                                      $"classified as {category} but confidence {confidence} " +
                                      $"is below threshold 8, staying Unassigned.");
                }

                if (statusId != 1)
                    await documents.UpdateStatusAsync(documentId, statusId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AiSummaryService error: {ex.Message}");
            }
        });
    }
}