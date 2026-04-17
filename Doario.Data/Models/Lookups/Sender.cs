using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Doario.Data.Models.SaaS;

namespace Doario.Data.Models.Lookups
{
    public class Sender
    {
        public Guid SenderId { get; set; }

        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; }

        public Guid SenderTypeId { get; set; }
        public SenderType SenderType { get; set; }

        [Required, MaxLength(200)]
        public string DisplayName { get; set; }

        [MaxLength(200)]
        public string Address { get; set; }

        [MaxLength(200)]
        public string Email { get; set; }

        [MaxLength(200)]
        public string Phone { get; set; }

        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime EndDate { get; set; } = DateTime.MaxValue;

        /// <summary>
        /// Creates the Unknown Sender placeholder row for a new tenant.
        /// Call during onboarding. Store the returned SenderId in tenant.UnknownSenderId.
        /// Used as Document.SenderId when AI cannot identify the sender.
        /// </summary>
        public static Sender CreateUnknown(Guid tenantId)
        {
            return new Sender
            {
                SenderId = Guid.NewGuid(),
                TenantId = tenantId,
                DisplayName = "Unknown Sender",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.MaxValue
            };
        }
    }
}