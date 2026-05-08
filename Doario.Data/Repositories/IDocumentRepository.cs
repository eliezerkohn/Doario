using Doario.Data.Models.Mail;

namespace Doario.Data.Repositories;

public interface IDocumentRepository
{
    Task<Document> GetByIdAsync(Guid documentId);
    Task<Document> GetByIdAsync(Guid documentId, Guid tenantId);
    Task<Document> GetByIdWithTenantAsync(Guid documentId, Guid tenantId);
    Task<List<Document>> GetQueueAsync(Guid tenantId, int page, int pageSize);
    Task<List<Document>> GetQueueByStatusAsync(Guid tenantId, int[] statusIds, int page, int pageSize);
    Task<int> GetMonthlyCountAsync(Guid tenantId, int year, int month);
    Task UpdateStatusAsync(Guid documentId, int statusId);
    Task UpdateOcrTextAsync(Guid documentId, string ocrText);
    Task UpdateAiSummaryAsync(Guid documentId, string aiSummary);
    Task UpdateSenderIdAsync(Guid documentId, Guid senderId);
    Task<Document> CreateAsync(Document document);
    Task DeleteAsync(Guid documentId);
    Task<List<Document>> GetBySenderAsync(Guid tenantId, string query);
    Task<List<SenderSummary>> GetDistinctSendersAsync(Guid tenantId);
    Task<List<Document>> GetStuckDocumentsAsync(Guid tenantId);

    /// <summary>
    /// Returns true if a document with this exact filename already exists for the tenant.
    /// Used to prevent duplicate processing when multiple fetchers run concurrently.
    /// </summary>
    Task<bool> ExistsByFileNameAsync(Guid tenantId, string fileName);

    Task SaveAsync();
}