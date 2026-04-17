using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Doario.Data.Models.Lookups;

namespace Doario.Data.Models.SaaS
{
    /// <summary>
    /// Stores the column mapping configuration for a custom database connection.
    /// Allows Doario to read staff and sender data directly from a client's
    /// existing database without writing custom code per client.
    /// One record per tenant per custom connection.
    /// </summary>
    public class TenantConnectorConfig
    {
        public Guid TenantConnectorConfigId { get; set; }

        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; }

        public int SourceTypeId { get; set; }
        public SourceType SourceType { get; set; }

        public int SystemStatusId { get; set; } = 1;
        public SystemStatus SystemStatus { get; set; }

        /// <summary>
        /// Encrypted connection string to the client's database
        /// </summary>
        [MaxLength(2000)]
        public string ConnectionString { get; set; }

        // ── Staff mapping ─────────────────────────────────────────────

        /// <summary>
        /// Client table that contains staff records e.g. tblStaff, Users
        /// </summary>
        [MaxLength(200)]
        public string StaffTableName { get; set; }

        [MaxLength(200)]
        public string StaffIdColumn { get; set; }

        [MaxLength(200)]
        public string StaffNameColumn { get; set; }

        [MaxLength(200)]
        public string StaffEmailColumn { get; set; }

        [MaxLength(200)]
        public string StaffJobTitleColumn { get; set; }

        [MaxLength(200)]
        public string StaffDepartmentColumn { get; set; }

        // ── Sender mapping ────────────────────────────────────────────

        /// <summary>
        /// Client table that contains sender records e.g. tblStudents, Contacts
        /// </summary>
        [MaxLength(200)]
        public string SenderTableName { get; set; }

        [MaxLength(200)]
        public string SenderIdColumn { get; set; }

        [MaxLength(200)]
        public string SenderNameColumn { get; set; }

        [MaxLength(200)]
        public string SenderEmailColumn { get; set; }

        [MaxLength(200)]
        public string SenderTypeIdColumn { get; set; }

        // ── Status ────────────────────────────────────────────────────

        /// <summary>
        /// When this config was last tested successfully
        /// </summary>
        public DateTime LastTestedAt { get; set; }

        [MaxLength(2000)]
        public string ErrorMessage { get; set; }

        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime EndDate { get; set; } = DateTime.MaxValue;
    }
}