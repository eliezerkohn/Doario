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

    /// <summary>
    /// Returns viewed document IDs scoped to a specific set of document IDs.
    /// Much faster than loading all viewed IDs — only checks the current page.
    /// </summary>
    Task<HashSet<Guid>> GetViewedDocumentIdsAsync(Guid tenantId, IEnumerable<Guid> documentIds);
}