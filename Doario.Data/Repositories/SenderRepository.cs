using Doario.Data.Models.Lookups;
using Microsoft.EntityFrameworkCore;

namespace Doario.Data.Repositories;

public class SenderRepository : ISenderRepository
{
    private readonly DoarioDataContext _db;

    public SenderRepository(DoarioDataContext db) => _db = db;

    public async Task<Sender> GetByEmailAsync(Guid tenantId, string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;

        return await _db.Senders
            .FirstOrDefaultAsync(s =>
                s.TenantId == tenantId &&
                s.Email == email &&
                s.EndDate > DateTime.UtcNow);
    }

    public async Task<Sender> GetByNameAsync(Guid tenantId, string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName)) return null;

        return await _db.Senders
            .FirstOrDefaultAsync(s =>
                s.TenantId == tenantId &&
                s.DisplayName == displayName &&
                s.EndDate > DateTime.UtcNow);
    }

    public async Task<Sender> CreateAsync(Sender sender)
    {
        _db.Senders.Add(sender);
        await _db.SaveChangesAsync();
        return sender;
    }

    public async Task UpdateAsync(Guid senderId, string displayName, string email)
    {
        var sender = await _db.Senders.FindAsync(senderId);
        if (sender is null) return;

        if (!string.IsNullOrWhiteSpace(displayName))
            sender.DisplayName = displayName;

        if (!string.IsNullOrWhiteSpace(email))
            sender.Email = email;

        await _db.SaveChangesAsync();
    }

    public async Task SaveAsync() => await _db.SaveChangesAsync();
}