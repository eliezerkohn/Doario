using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Doario.Data.Models.Lookups;

namespace Doario.Data.Models.SaaS
{
    public class TenantConnection
    {
        public Guid TenantConnectionId { get; set; }

        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; }

        public int SourceTypeId { get; set; }
        public SourceType SourceType { get; set; }

        /// <summary>
        /// Defaults to Active=1 on insert.
        /// Updated to Expired=2 or Revoked=3 if connection fails.
        /// </summary>
        public int SystemStatusId { get; set; } = 1;
        public SystemStatus SystemStatus { get; set; }

        [MaxLength(2000)]
        public string AccessToken { get; set; }

        public DateTime TokenExpiresAt { get; set; }
        public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(2000)]
        public string ErrorMessage { get; set; }

        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime EndDate { get; set; } = DateTime.MaxValue;
    }
}