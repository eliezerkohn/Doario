using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /// Defaults to Pending=7 on insert.
        /// Updated to Sent=8 on success or Failed=5 on error.
        /// </summary>
        public int SystemStatusId { get; set; } = 7;
        public SystemStatus SystemStatus { get; set; }

        [Required, MaxLength(200)]
        public string SentToEmail { get; set; }

        public DateTime SentAt { get; set; }

        [MaxLength(2000)]
        public string ErrorMessage { get; set; }

        public int RetryCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}