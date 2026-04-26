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

                var doc = await documents.GetByIdAsync(documentId);
                if (doc is null || string.IsNullOrWhiteSpace(doc.OcrText)) return;

                var client = new AzureOpenAIClient(
                    new Uri(_config["AzureOpenAI:Endpoint"]),
                    new AzureKeyCredential(_config["AzureOpenAI:ApiKey"]));
                var chatClient = client.GetChatClient(_config["AzureOpenAI:DeploymentName"]);

                var prompt = $"""
                    You are an assistant that summarises physical mail documents for office staff.

                    Read the following OCR text and produce a structured summary.
                    Output exactly these five fields, each on its own line:

                    <strong>Document Type:</strong> [type]
                    <strong>Sender:</strong> [name and contact details, or "Not provided"]
                    <strong>Purpose:</strong> [main subject]
                    <strong>Action Required:</strong> [what needs to be done, or "None"]
                    <strong>Key Details:</strong> [names, dates, amounts, reference numbers, or "None"]

                    Rules:
                    - Output only the five lines above, nothing else
                    - No extra blank lines between fields
                    - Use the <strong> tags exactly as shown — no markdown, no asterisks
                    - Each value is one sentence maximum
                    - Plain English only

                    OCR TEXT:
                    {doc.OcrText}
                    """;

                var response = await chatClient.CompleteChatAsync(new UserChatMessage(prompt));
                var raw = response.Value.Content[0].Text.Trim();

                // Each field is on its own line — convert to <br> for HTML rendering
                var html = raw
                    .Replace("\r\n", "\n")
                    .Replace("\r", "\n")
                    .Replace("\n", "<br>");

                await documents.UpdateAiSummaryAsync(documentId, html);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AiSummaryService error: {ex.Message}");
            }
        });
    }
}