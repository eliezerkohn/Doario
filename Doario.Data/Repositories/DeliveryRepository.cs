using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Doario.Data.Models.Mail;
using Microsoft.EntityFrameworkCore;

namespace Doario.Data.Repositories
{
    public class DeliveryRepository : IDeliveryRepository
    {
        private readonly DoarioDataContext _db;

        public DeliveryRepository(DoarioDataContext db)
        {
            _db = db;
        }

        public async Task<List<DocumentDelivery>> GetByAssignmentIdsAsync(List<Guid> assignmentIds)
        {
            return await _db.DocumentDeliveries
                .Where(d => assignmentIds.Contains(d.DocumentAssignmentId))
                .ToListAsync();
        }

        /// <summary>
        /// Returns all Failed (SystemStatusId=5) deliveries with RetryCount less than 3.
        /// Includes Document and DocumentAssignment for re-delivery.
        /// </summary>
        public async Task<List<DocumentDelivery>> GetFailedForRetryAsync()
        {
            return await _db.DocumentDeliveries
                .Include(d => d.Document)
                .Include(d => d.DocumentAssignment)
                .Where(d => d.SystemStatusId == 5 && d.RetryCount < 3)
                .ToListAsync();
        }

        public async Task AddAsync(DocumentDelivery delivery)
        {
            await _db.DocumentDeliveries.AddAsync(delivery);
        }

        public async Task DeleteRangeAsync(List<DocumentDelivery> deliveries)
        {
            _db.DocumentDeliveries.RemoveRange(deliveries);
        }

        public async Task SaveAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}