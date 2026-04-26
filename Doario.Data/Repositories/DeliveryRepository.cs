using Doario.Data.Models.Mail;
using Microsoft.EntityFrameworkCore;

namespace Doario.Data.Repositories;

public class DeliveryRepository : IDeliveryRepository
{
    private readonly DoarioDataContext _db;

    public DeliveryRepository(DoarioDataContext db) => _db = db;

    public async Task<List<DocumentDelivery>> GetByAssignmentIdsAsync(List<Guid> assignmentIds)
        => await _db.DocumentDeliveries
            .Where(d => assignmentIds.Contains(d.DocumentAssignmentId))
            .ToListAsync();

    public async Task AddAsync(DocumentDelivery delivery)
    {
        _db.DocumentDeliveries.Add(delivery);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteRangeAsync(List<DocumentDelivery> deliveries)
    {
        _db.DocumentDeliveries.RemoveRange(deliveries);
        await _db.SaveChangesAsync();
    }

    public async Task SaveAsync()
        => await _db.SaveChangesAsync();
}