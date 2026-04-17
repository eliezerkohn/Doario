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
    public class ImportedSender
    {
        public Guid ImportedSenderId { get; set; }

        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; }

        public Guid SenderTypeId { get; set; }
        public SenderType SenderType { get; set; }

        public int SourceTypeId { get; set; }
        public SourceType SourceType { get; set; }

        [MaxLength(200)]
        public string ExternalId { get; set; }

        [Required, MaxLength(200)]
        public string DisplayName { get; set; }

        [MaxLength(200)]
        public string Email { get; set; }

        [MaxLength(200)]
        public string Phone { get; set; }

        public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime EndDate { get; set; } = DateTime.MaxValue;
    }
}