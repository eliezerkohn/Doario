using Doario.Data.Models.Mail;

namespace Doario.Data.Repositories;

public interface IDeliveryRepository
{
    Task<List<DocumentDelivery>> GetByAssignmentIdsAsync(List<Guid> assignmentIds);
    Task AddAsync(DocumentDelivery delivery);
    Task DeleteRangeAsync(List<DocumentDelivery> deliveries);
    Task SaveAsync();
}