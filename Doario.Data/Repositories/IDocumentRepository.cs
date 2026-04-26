using Doario.Data.Models.Mail;

namespace Doario.Data.Repositories;

public interface IDocumentRepository
{
    // Used by background services (OCR, AI) — no tenant context available
    Task<Document> GetByIdAsync(Guid documentId);

    // Used by controllers — always filter by tenant for security
    Task<Document> GetByIdAsync(Guid documentId, Guid tenantId);
    Task<Document> GetByIdWithTenantAsync(Guid documentId, Guid tenantId);

    Task<List<Document>> GetQueueAsync(Guid tenantId, int page, int pageSize);
    Task UpdateStatusAsync(Guid documentId, int statusId);
    Task UpdateOcrTextAsync(Guid documentId, string ocrText);
    Task UpdateAiSummaryAsync(Guid documentId, string aiSummary);
    Task<Document> CreateAsync(Document document);
    Task SaveAsync();
}