using System;
using System.ComponentModel.DataAnnotations;

using Doario.Data.Models.Lookups;
using Doario.Data.Models.SaaS;

namespace Doario.Data.Models.Mail
{
    public class DocumentDelivery
    {
        public Guid DocumentDeliveryId { get; set; }

        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; }

        public Guid DocumentId { get; set; }
        public Document Document { get; set; }

        public Guid DocumentAssignmentId { get; set; }
        public DocumentAssignment DocumentAssignment { get; set; }

        /// <summary>
        /// 7 = Pending, 8 = Sent, 5 = Failed, 9 = PermanentFail
        /// </summary>
        public int SystemStatusId { get; set; } = 7;
        public SystemStatus SystemStatus { get; set; }

        [Required, MaxLength(200)]
        public string SentToEmail { get; set; }

        public DateTime SentAt { get; set; }

        [MaxLength(2000)]
        public string ErrorMessage { get; set; }

        public int RetryCount { get; set; } = 0;

        public DateTime? LastRetryAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}