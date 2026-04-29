using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using Doario.Data.Models.Mail;
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
                var senderResolution = scope.ServiceProvider.GetRequiredService<SenderResolutionService>();
                var extractionFieldRepo = scope.ServiceProvider.GetRequiredService<IExtractionFieldRepository>();
                var documentCheckRepo = scope.ServiceProvider.GetRequiredService<IDocumentCheckRepository>();

                var doc = await documents.GetByIdAsync(documentId);
                if (doc is null || string.IsNullOrWhiteSpace(doc.OcrText)) return;

                // -- Step 1: Whitelist check --
                var isWhitelisted = await whitelistRepo.IsWhitelistedAsync(doc.TenantId, doc.OcrText);

                // -- Step 2: Load active extraction fields --
                var extractionFields = await extractionFieldRepo.GetActiveFieldsAsync(doc.TenantId);

                var extractionFieldsBlock = string.Empty;
                if (extractionFields.Any())
                {
                    var fieldLines = extractionFields.Select(f =>
                        string.IsNullOrWhiteSpace(f.FieldDescription)
                            ? "- " + f.FieldName
                            : "- " + f.FieldName + ": " + f.FieldDescription);

                    extractionFieldsBlock =
                        "\n\nSTEP 6 - Extract the following custom fields if present in the document. " +
                        "For each field found, add it to the SUMMARY Key Details section in the format [FieldName]: [value]. " +
                        "If a field is not found, do not mention it.\n" +
                        string.Join("\n", fieldLines) +
                        "\n\nSTEP 7 - Detect if this document is a physical check (cheque). " +
                        "If it is a check, extract: amount (numbers only, no currency symbol), payer name, check number. " +
                        "If it is not a check, return no for IS_CHECK and UNKNOWN for the rest.";
                }
                else
                {
                    extractionFieldsBlock =
                        "\n\nSTEP 6 - Detect if this document is a physical check (cheque). " +
                        "If it is a check, extract: amount (numbers only, no currency symbol), payer name, check number. " +
                        "If it is not a check, return no for IS_CHECK and UNKNOWN for the rest.";
                }

                // -- Step 3: Load corrections --
                var senderCorrections = await feedbackRepo.GetRelevantForSenderAsync(
                    doc.TenantId, doc.OcrText);

                var recentCorrections = await feedbackRepo.GetRecentForTenantAsync(
                    doc.TenantId, 5);

                var allCorrections = senderCorrections
                    .Concat(recentCorrections)
                    .GroupBy(c => c.DocumentFeedbackId)
                    .Select(g => g.First())
                    .ToList();

                string correctionExamples;
                if (allCorrections.Any())
                {
                    var lines = new List<string>();

                    foreach (var c in senderCorrections.Take(5))
                    {
                        var snippet = c.DocumentSnippet?[..Math.Min(120, c.DocumentSnippet?.Length ?? 0)];
                        lines.Add($"- IMPORTANT: A document from this same sender " +
                                  $"(starting with \"{snippet}\") was previously classified as " +
                                  $"\"{c.AiClassification}\" but the admin confirmed it is " +
                                  $"\"{c.CorrectedClassification}\". Apply this correction.");
                    }

                    foreach (var c in recentCorrections
                        .Where(r => !senderCorrections.Any(s => s.DocumentFeedbackId == r.DocumentFeedbackId))
                        .Take(5))
                    {
                        var snippet = c.DocumentSnippet?[..Math.Min(100, c.DocumentSnippet?.Length ?? 0)];
                        lines.Add($"- A document starting with \"{snippet}\" was incorrectly " +
                                  $"classified as \"{c.AiClassification}\" but is actually " +
                                  $"\"{c.CorrectedClassification}\".");
                    }

                    correctionExamples = "\n\nLEARNING FROM PAST CORRECTIONS - apply these:\n" +
                                         string.Join("\n", lines);
                }
                else
                {
                    correctionExamples = string.Empty;
                }

                // -- Step 4: Call Azure OpenAI --
                var client = new AzureOpenAIClient(
                    new Uri(_config["AzureOpenAI:Endpoint"]),
                    new AzureKeyCredential(_config["AzureOpenAI:ApiKey"]));
                var chatClient = client.GetChatClient(_config["AzureOpenAI:DeploymentName"]);

                var prompt = $"""
                    You are an assistant that analyses physical mail documents for an office mail room system.

                    Read the following OCR text and do the following:

                    STEP 1 - Classify the document into exactly one of these categories:
                    - mail         (a real document needing staff attention)
                    - promotion    (marketing material, advertisements, offers)
                    - spam         (junk mail, unsolicited bulk mail)
                    {correctionExamples}

                    STEP 2 - Rate your confidence in this classification from 1 to 10.
                    10 = absolutely certain. 1 = complete guess.
                    Be conservative - if there is any doubt, score lower.

                    STEP 3 - Extract the sender's full name or company name.
                    Use the most specific name available (e.g. "Dr. Sarah Jones" over "Jones Medical").
                    If no sender name is found, write UNKNOWN.

                    STEP 4 - Extract the sender's email address.
                    Only include a real email address found in the document.
                    If no email address is present, write UNKNOWN.

                    STEP 5 - Write a structured summary on a SINGLE LINE (no line breaks) using EXACTLY
                    this format for ALL categories including spam and promotion:
                    <strong>Document Type:</strong> [type] <strong>Sender:</strong> [name or company] <strong>Purpose:</strong> [main subject] <strong>Action Required:</strong> [action or None] <strong>Key Details:</strong> [relevant details or None]
                    {extractionFieldsBlock}

                    Return ONLY these lines, nothing else:
                    CATEGORY: [mail|promotion|spam]
                    CONFIDENCE: [1-10]
                    FROM_NAME: [sender full name or company, or UNKNOWN]
                    FROM_EMAIL: [sender email address, or UNKNOWN]
                    SUMMARY: [your single-line summary here]
                    IS_CHECK: [yes|no]
                    CHECK_AMOUNT: [amount numbers only, or UNKNOWN]
                    CHECK_PAYER: [payer name, or UNKNOWN]
                    CHECK_NUMBER: [check number, or UNKNOWN]

                    OCR TEXT:
                    {doc.OcrText}
                    """;

                var response = await chatClient.CompleteChatAsync(new UserChatMessage(prompt));
                var raw = response.Value.Content[0].Text.Trim();

                // -- Step 5: Parse response --
                var category = "mail";
                var confidence = 0;
                var summary = string.Empty;
                var fromName = string.Empty;
                var fromEmail = string.Empty;
                var isCheck = false;
                var checkAmount = string.Empty;
                var checkPayer = string.Empty;
                var checkNumber = string.Empty;

                foreach (var line in raw.Split('\n'))
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("CATEGORY:", StringComparison.OrdinalIgnoreCase))
                        category = trimmed.Substring(9).Trim().ToLowerInvariant();
                    else if (trimmed.StartsWith("CONFIDENCE:", StringComparison.OrdinalIgnoreCase))
                        int.TryParse(trimmed.Substring(11).Trim(), out confidence);
                    else if (trimmed.StartsWith("FROM_NAME:", StringComparison.OrdinalIgnoreCase))
                    {
                        var val = trimmed.Substring(10).Trim();
                        fromName = val.Equals("UNKNOWN", StringComparison.OrdinalIgnoreCase)
                            ? string.Empty : val;
                    }
                    else if (trimmed.StartsWith("FROM_EMAIL:", StringComparison.OrdinalIgnoreCase))
                    {
                        var val = trimmed.Substring(11).Trim();
                        fromEmail = val.Equals("UNKNOWN", StringComparison.OrdinalIgnoreCase)
                            ? string.Empty : val;
                    }
                    else if (trimmed.StartsWith("SUMMARY:", StringComparison.OrdinalIgnoreCase))
                        summary = trimmed.Substring(8).Trim();
                    else if (trimmed.StartsWith("IS_CHECK:", StringComparison.OrdinalIgnoreCase))
                        isCheck = trimmed.Substring(9).Trim().Equals("yes", StringComparison.OrdinalIgnoreCase);
                    else if (trimmed.StartsWith("CHECK_AMOUNT:", StringComparison.OrdinalIgnoreCase))
                    {
                        var val = trimmed.Substring(13).Trim();
                        checkAmount = val.Equals("UNKNOWN", StringComparison.OrdinalIgnoreCase) ? string.Empty : val;
                    }
                    else if (trimmed.StartsWith("CHECK_PAYER:", StringComparison.OrdinalIgnoreCase))
                    {
                        var val = trimmed.Substring(12).Trim();
                        checkPayer = val.Equals("UNKNOWN", StringComparison.OrdinalIgnoreCase) ? string.Empty : val;
                    }
                    else if (trimmed.StartsWith("CHECK_NUMBER:", StringComparison.OrdinalIgnoreCase))
                    {
                        var val = trimmed.Substring(13).Trim();
                        checkNumber = val.Equals("UNKNOWN", StringComparison.OrdinalIgnoreCase) ? string.Empty : val;
                    }
                }

                if (string.IsNullOrWhiteSpace(summary))
                    summary = raw;

                // -- Step 6: Format summary --
                var html = summary
                    .Replace("<strong>Sender:", "<br><strong>Sender:")
                    .Replace("<strong>Purpose:", "<br><strong>Purpose:")
                    .Replace("<strong>Action Required:", "<br><strong>Action Required:")
                    .Replace("<strong>Key Details:", "<br><strong>Key Details:");

                await documents.UpdateAiSummaryAsync(documentId, html);

                // -- Step 7: Resolve sender to Sender table --
                await senderResolution.ResolveAsync(
                    documentId, doc.TenantId, fromName, fromEmail);

                // -- Step 8: Save check if detected --
                if (isCheck && !string.IsNullOrWhiteSpace(checkPayer))
                {
                    decimal.TryParse(checkAmount, out var parsedAmount);
                    await documentCheckRepo.SaveAsync(new DocumentCheck
                    {
                        DocumentCheckId = Guid.NewGuid(),
                        DocumentId = documentId,
                        CheckAmount = parsedAmount,
                        CheckPayerName = checkPayer,
                        CheckNumber = checkNumber
                    });
                }

                // -- Step 9: Decide folder --
                int statusId;

                if (isWhitelisted || category == "mail")
                {
                    statusId = 1;
                    if (isWhitelisted)
                        Console.WriteLine($"AiSummaryService: document {documentId} -- " +
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
                    Console.WriteLine($"AiSummaryService: document {documentId} -- " +
                                      $"classified as {category} with confidence {confidence}, " +
                                      $"moved to status {statusId}.");
                }
                else
                {
                    statusId = 1;
                    Console.WriteLine($"AiSummaryService: document {documentId} -- " +
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