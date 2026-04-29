using Doario.Data.Models.Lookups;

namespace Doario.Data.Repositories;

public interface ISenderRepository
{
    /// <summary>
    /// Find an existing sender by email address for this tenant.
    /// Returns null if not found.
    /// </summary>
    Task<Sender> GetByEmailAsync(Guid tenantId, string email);

    /// <summary>
    /// Find an existing sender by display name (case-insensitive) for this tenant.
    /// Returns null if not found.
    /// </summary>
    Task<Sender> GetByNameAsync(Guid tenantId, string displayName);

    /// <summary>
    /// Create a new sender record.
    /// </summary>
    Task<Sender> CreateAsync(Sender sender);

    /// <summary>
    /// Update display name and/or email on an existing sender.
    /// Called when AI finds better info for a sender we already know.
    /// </summary>
    Task UpdateAsync(Guid senderId, string displayName, string email);

    Task SaveAsync();
}