using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using Doario.Data.Models.Mail;
using Doario.Data.Repositories;
using System.Text.RegularExpressions;

namespace Doario.Web.Services;

public class AiSummaryService
{
    private readonly IConfiguration _config;
    private readonly IServiceScopeFactory _scopeFactory;

    // Valid email regex — rejects URLs, payment portals, web addresses
    private static readonly Regex ValidEmailRegex = new(
        @"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled);

    public AiSummaryService(IConfiguration config, IServiceScopeFactory scopeFactory)
    {
        _config = config;
        _scopeFactory = scopeFactory;
    }

    public void RunInBackground(Guid documentId)
    {
        Task.Run(async () => await GenerateAndSaveAsync(documentId));
    }

    public async Task GenerateAndSaveAsync(Guid documentId)
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
            var staffRepo = scope.ServiceProvider.GetRequiredService<IStaffRepository>();
            var aiSettingsRepo = scope.ServiceProvider.GetRequiredService<ITenantAiSettingsRepository>();
            var aiAssignmentService = scope.ServiceProvider.GetRequiredService<AiAssignmentService>();

            var doc = await documents.GetByIdAsync(documentId);
            if (doc is null || string.IsNullOrWhiteSpace(doc.OcrText)) return;

            // -- Step 1: Whitelist check --
            var isWhitelisted = await whitelistRepo.IsWhitelistedAsync(doc.TenantId, doc.OcrText);

            // -- Step 2: Load active extraction fields --
            var extractionFields = await extractionFieldRepo.GetActiveFieldsAsync(doc.TenantId);

            const string checkDetectionPrompt =
                "Detect if this document is a PHYSICAL PAPER CHECK (cheque). " +
                "A physical check has ALL of these features: a printed check number, a bank routing number, " +
                "an account number, a payee line (Pay to the order of), and a signature line. " +
                "Financial statements, invoices, remittance advice, tax forms (1099, W-2, etc.), " +
                "insurance documents, medical bills, and any document that merely mentions a dollar amount " +
                "are NOT checks. If and ONLY IF all check features are present, extract: " +
                "amount (numbers only, no currency symbol), payer name, check number. " +
                "If there is any doubt, return no for IS_CHECK and UNKNOWN for the rest.";

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
                    "If a field is NOT found in the document, do NOT mention it at all — do not write UNKNOWN, do not write the field name. " +
                    "Only include fields that have a real value.\n" +
                    string.Join("\n", fieldLines) +
                    "\n\nSTEP 7 - " + checkDetectionPrompt;
            }
            else
            {
                extractionFieldsBlock = "\n\nSTEP 6 - " + checkDetectionPrompt;
            }

            // -- Step 3: Load AI assignment config and active staff --
            var aiSettings = await aiSettingsRepo.GetByTenantAsync(doc.TenantId);
            var assignmentMode = aiSettings?.AiAssignmentMode ?? "AutoAssign";
            var confidenceThreshold = aiSettings?.AiConfidenceThreshold ?? 8;

            var assignmentBlock = string.Empty;
            if (assignmentMode != "Off")
            {
                var allStaff = await staffRepo.GetAllForTenantAsync(doc.TenantId);
                var activeStaff = allStaff.Where(s => s.IsActive).ToList();
                var assignmentCorrections = await feedbackRepo.GetAssignmentCorrectionsAsync(doc.TenantId, doc.OcrText);

                if (activeStaff.Any())
                {
                    var staffLines = activeStaff.Select(s => "- " + s.Email + ": " + s.FirstName + " " + s.LastName);
                    var correctionLines = string.Empty;
                    if (assignmentCorrections.Any())
                    {
                        var cLines = assignmentCorrections.Take(5).Select(c =>
                        {
                            var snippet = c.DocumentSnippet != null && c.DocumentSnippet.Length > 100
                                ? c.DocumentSnippet[..100] : c.DocumentSnippet ?? string.Empty;
                            return "- A document starting with \"" + snippet +
                                   "\" was suggested for " + c.AiClassification +
                                   " but admin assigned to " + c.CorrectedClassification + ".";
                        });
                        correctionLines = "\n\nASSIGNMENT LEARNING - apply these:\n" + string.Join("\n", cLines);
                    }
                    assignmentBlock =
                        "\n\nSTEP ASSIGNMENT - Based on the document content, suggest which staff member " +
                        "should handle this document. Choose from this list:\n" +
                        string.Join("\n", staffLines) + correctionLines +
                        "\nIf you cannot determine who should handle it, return UNKNOWN.";
                }
            }

            // -- Step 4: Load corrections --
            var senderCorrections = await feedbackRepo.GetRelevantForSenderAsync(doc.TenantId, doc.OcrText);
            var recentCorrections = await feedbackRepo.GetRecentForTenantAsync(doc.TenantId, 5);
            var notCheckCorrections = await feedbackRepo.GetNotCheckCorrectionsAsync(doc.TenantId);

            var allCorrections = senderCorrections
                .Concat(recentCorrections)
                .GroupBy(c => c.DocumentFeedbackId)
                .Select(g => g.First())
                .ToList();

            string correctionExamples = string.Empty;
            if (allCorrections.Any())
            {
                var lines = new List<string>();
                foreach (var c in senderCorrections.Take(5))
                {
                    var snippet = c.DocumentSnippet?[..Math.Min(120, c.DocumentSnippet?.Length ?? 0)];
                    lines.Add($"- IMPORTANT: A document from this same sender (starting with \"{snippet}\") " +
                              $"was previously classified as \"{c.AiClassification}\" but the admin confirmed " +
                              $"it is \"{c.CorrectedClassification}\". Apply this correction.");
                }
                foreach (var c in recentCorrections
                    .Where(r => !senderCorrections.Any(s => s.DocumentFeedbackId == r.DocumentFeedbackId))
                    .Take(5))
                {
                    var snippet = c.DocumentSnippet?[..Math.Min(100, c.DocumentSnippet?.Length ?? 0)];
                    lines.Add($"- A document starting with \"{snippet}\" was incorrectly " +
                              $"classified as \"{c.AiClassification}\" but is actually \"{c.CorrectedClassification}\".");
                }
                correctionExamples = "\n\nLEARNING FROM PAST CORRECTIONS - apply these:\n" + string.Join("\n", lines);
            }

            var notCheckBlock = string.Empty;
            if (notCheckCorrections.Any())
            {
                var ncLines = notCheckCorrections.Take(5).Select(c =>
                {
                    var snippet = c.DocumentSnippet?[..Math.Min(120, c.DocumentSnippet?.Length ?? 0)];
                    return $"- A document starting with \"{snippet}\" was incorrectly flagged as a check. It is NOT a check.";
                });
                notCheckBlock = "\n\nCHECK DETECTION LEARNING - these were NOT checks:\n" + string.Join("\n", ncLines);
            }

            // -- Step 5: Call Azure OpenAI --
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

                    STEP 3 - Extract the sender's full name or company name.
                    Use the most specific name available.
                    If no sender name is found, write UNKNOWN.

                    STEP 4 - Extract the sender's genuine contact email address.
                    Only include a real person or company email address in the format name@domain.com.
                    Do NOT extract payment portal addresses, website URLs, QR code addresses, or web addresses.
                    Do NOT extract addresses from "pay at", "visit", "log in at", or "go to" instructions.
                    Do NOT extract anything that is not a standard email address with a proper @ symbol and domain.
                    If no genuine contact email address is present, write UNKNOWN.

                    STEP 5 - Write a structured summary on a SINGLE LINE using EXACTLY this format:
                    <strong>Sender:</strong> [name or company] <strong>Purpose:</strong> [main subject] <strong>Action Required:</strong> [action or None] <strong>Key Details:</strong> [relevant details or None]

                    STEP NAME - Generate a short meaningful filename for this document (no extension, no spaces, use underscores).
                    The name should describe what the document is, who it is from, and if applicable the year or reference number.
                    Examples: ShelterPoint_1099MISC_2025, NYP_PatientSummary_Kohn, DEA_InspectionNotice_2026
                    Maximum 50 characters. No special characters except underscores.
                    {extractionFieldsBlock}{notCheckBlock}{assignmentBlock}

                    Return ONLY these lines, nothing else:
                    CATEGORY: [mail|promotion|spam]
                    CONFIDENCE: [1-10]
                    FROM_NAME: [sender full name or company, or UNKNOWN]
                    FROM_EMAIL: [sender email address, or UNKNOWN]
                    SUMMARY: [your single-line summary here]
                    DOCUMENT_NAME: [meaningful filename without extension]
                    IS_CHECK: [yes|no]
                    CHECK_AMOUNT: [amount numbers only, or UNKNOWN]
                    CHECK_PAYER: [payer name, or UNKNOWN]
                    CHECK_NUMBER: [check number, or UNKNOWN]
                    SUGGESTED_STAFF: [staff email from the list, or UNKNOWN]
                    ASSIGNMENT_CONFIDENCE: [1-10]

                    OCR TEXT:
                    {doc.OcrText}
                    """;

            var response = await chatClient.CompleteChatAsync(new UserChatMessage(prompt));
            var raw = response.Value.Content[0].Text.Trim();

            // -- Step 6: Parse response --
            var category = "mail";
            var confidence = 0;
            var summary = string.Empty;
            var fromName = string.Empty;
            var fromEmail = string.Empty;
            var documentName = string.Empty;
            var isCheck = false;
            var checkAmount = string.Empty;
            var checkPayer = string.Empty;
            var checkNumber = string.Empty;
            var suggestedStaffEmail = string.Empty;
            var assignmentConfidence = 0;

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
                    fromName = val.Equals("UNKNOWN", StringComparison.OrdinalIgnoreCase) ? string.Empty : val;
                }
                else if (trimmed.StartsWith("FROM_EMAIL:", StringComparison.OrdinalIgnoreCase))
                {
                    var val = trimmed.Substring(11).Trim();
                    fromEmail = val.Equals("UNKNOWN", StringComparison.OrdinalIgnoreCase) ? string.Empty : val;
                }
                else if (trimmed.StartsWith("SUMMARY:", StringComparison.OrdinalIgnoreCase))
                    summary = trimmed.Substring(8).Trim();
                else if (trimmed.StartsWith("DOCUMENT_NAME:", StringComparison.OrdinalIgnoreCase))
                {
                    var val = trimmed.Substring(14).Trim();
                    documentName = val.Equals("UNKNOWN", StringComparison.OrdinalIgnoreCase) ? string.Empty : val;
                }
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
                else if (trimmed.StartsWith("SUGGESTED_STAFF:", StringComparison.OrdinalIgnoreCase))
                {
                    var val = trimmed.Substring(16).Trim();
                    suggestedStaffEmail = val.Equals("UNKNOWN", StringComparison.OrdinalIgnoreCase) ? string.Empty : val;
                }
                else if (trimmed.StartsWith("ASSIGNMENT_CONFIDENCE:", StringComparison.OrdinalIgnoreCase))
                    int.TryParse(trimmed.Substring(22).Trim(), out assignmentConfidence);
            }

            if (string.IsNullOrWhiteSpace(summary)) summary = raw;

            // -- Step 7: Validate extracted email --
            // Reject payment portals, URLs, and anything that isn't a real email address
            if (!string.IsNullOrWhiteSpace(fromEmail) && !ValidEmailRegex.IsMatch(fromEmail))
            {
                _logger.LogDebug(
                    "AiSummaryService: rejected invalid/non-contact email '{Email}' for document {Id}",
                    fromEmail, documentId);
                fromEmail = string.Empty;
            }

            // -- Step 8: Strip UNKNOWN fields from summary --
            summary = Regex.Replace(summary, @",?\s*[\w\s\-/]+:\s*UNKNOWN\b", "", RegexOptions.IgnoreCase).Trim();
            summary = Regex.Replace(summary, @",\s*$", "").Trim();
            summary = Regex.Replace(summary, @"\s{2,}", " ").Trim();

            // -- Step 9: Format summary --
            var html = summary
                .Replace("<strong>Sender:", "<br><strong>Sender:")
                .Replace("<strong>Purpose:", "<br><strong>Purpose:")
                .Replace("<strong>Action Required:", "<br><strong>Action Required:")
                .Replace("<strong>Key Details:", "<br><strong>Key Details:");

            // Remove leading <br> if present
            if (html.StartsWith("<br>", StringComparison.OrdinalIgnoreCase))
                html = html.Substring(4).TrimStart();

            await documents.UpdateAiSummaryAsync(documentId, html);

            // -- Step 10: Update filename with AI-generated meaningful name --
            if (!string.IsNullOrWhiteSpace(documentName))
            {
                var cleanName = Regex.Replace(documentName, @"[^a-zA-Z0-9_]", "_");
                cleanName = Regex.Replace(cleanName, @"_+", "_").Trim('_');
                if (cleanName.Length > 50) cleanName = cleanName[..50];
                var originalExt = Path.GetExtension(doc.OriginalFileName);
                var newFileName = cleanName + (string.IsNullOrEmpty(originalExt) ? ".pdf" : originalExt);
                await documents.UpdateFileNameAsync(documentId, newFileName);
            }

            // -- Step 11: Resolve sender --
            await senderResolution.ResolveAsync(documentId, doc.TenantId, fromName, fromEmail);

            // -- Step 12: Save check if detected --
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

            // -- Step 13: AI Assignment --
            if (!string.IsNullOrWhiteSpace(suggestedStaffEmail) && assignmentMode != "Off")
            {
                await aiAssignmentService.ProcessAsync(
                    documentId, doc.TenantId, suggestedStaffEmail,
                    assignmentConfidence, assignmentMode, confidenceThreshold);
            }

            // -- Step 14: Decide folder --
            int statusId;
            if (isWhitelisted || category == "mail")
                statusId = 1;
            else if (confidence >= 8)
                statusId = category switch { "spam" => 7, "promotion" => 8, _ => 1 };
            else
                statusId = 1;

            if (statusId != 1)
                await documents.UpdateStatusAsync(documentId, statusId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AiSummaryService error: {ex.Message}");
        }
    }

    private ILogger<AiSummaryService> _logger =>
        _scopeFactory.CreateScope().ServiceProvider
            .GetRequiredService<ILogger<AiSummaryService>>();
}