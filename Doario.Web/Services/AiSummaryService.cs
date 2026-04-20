using Azure.AI.OpenAI;
using Azure;
using Doario.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

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
                var db = scope.ServiceProvider.GetRequiredService<DoarioDataContext>();

                var doc = await db.Documents.FirstOrDefaultAsync(d => d.DocumentId == documentId);
                if (doc == null || string.IsNullOrWhiteSpace(doc.OcrText)) return;

                var endpoint = _config["AzureOpenAI:Endpoint"];
                var apiKey = _config["AzureOpenAI:ApiKey"];
                var deployment = _config["AzureOpenAI:DeploymentName"];

                var client = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
                var chatClient = client.GetChatClient(deployment);

                var prompt = $"""
                    You are an assistant that summarises physical mail documents for office staff.
                    
                    Read the following OCR-extracted text from a scanned document and produce a clean, 
                    professional summary. Include:
                    - Document type (letter, form, invoice, etc.)
                    - Sender name and contact details if present
                    - Main purpose or subject of the document
                    - Any action required or deadlines mentioned
                    - Key details (names, dates, amounts, reference numbers)
                    
                    Keep it concise — maximum 150 words. Write in plain English.
                    
                    OCR TEXT:
                    {doc.OcrText}
                    """;

                var response = await chatClient.CompleteChatAsync(
                    new UserChatMessage(prompt));

                doc.AiSummary = response.Value.Content[0].Text;
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AiSummaryService error: {ex.Message}");
            }
        });
    }
}