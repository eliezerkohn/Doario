using Doario.Data.Models.Mail;

namespace Doario.Data.Repositories;

public interface IDocumentViewedRepository
{
    /// <summary>Mark a document as viewed. No-op if already viewed.</summary>
    Task MarkViewedAsync(Guid documentId, Guid tenantId, Guid viewedByStaffId);

    /// <summary>Remove the viewed record — marks document as unread for everyone.</summary>
    Task MarkUnreadAsync(Guid documentId, Guid tenantId);

    /// <summary>Returns all viewed document IDs for this tenant.</summary>
    Task<HashSet<Guid>> GetViewedDocumentIdsAsync(Guid tenantId);
}