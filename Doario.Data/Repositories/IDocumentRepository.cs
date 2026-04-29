using Doario.Data.Models.Mail;

namespace Doario.Data.Repositories;

public interface IDocumentRepository
{
    Task<Document> GetByIdAsync(Guid documentId);
    Task<Document> GetByIdAsync(Guid documentId, Guid tenantId);
    Task<Document> GetByIdWithTenantAsync(Guid documentId, Guid tenantId);
    Task<List<Document>> GetQueueAsync(Guid tenantId, int page, int pageSize);
    Task<int> GetMonthlyCountAsync(Guid tenantId, int year, int month);
    Task UpdateStatusAsync(Guid documentId, int statusId);
    Task UpdateOcrTextAsync(Guid documentId, string ocrText);
    Task UpdateAiSummaryAsync(Guid documentId, string aiSummary);

    /// <summary>
    /// Updates Document.SenderId after sender resolution links the document
    /// to a row in the Sender table.
    /// </summary>
    Task UpdateSenderIdAsync(Guid documentId, Guid senderId);

    Task<Document> CreateAsync(Document document);
    Task DeleteAsync(Guid documentId);
    Task<List<Document>> GetBySenderAsync(Guid tenantId, string query);
    Task<List<SenderSummary>> GetDistinctSendersAsync(Guid tenantId);
    Task SaveAsync();
}