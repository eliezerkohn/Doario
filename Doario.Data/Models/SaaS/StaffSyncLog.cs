using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Doario.Data.Models.Lookups;

namespace Doario.Data.Models.SaaS
{
    public class StaffSyncLog
    {
        public Guid StaffSyncLogId { get; set; }

        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; }

        public int SourceTypeId { get; set; }
        public SourceType SourceType { get; set; }

        /// <summary>
        /// Defaults to Pending=7 on insert.
        /// Updated to Success=4, Failed=5, or PartialSuccess=6 on completion.
        /// </summary>
        public int SystemStatusId { get; set; } = 7;
        public SystemStatus SystemStatus { get; set; }

        public DateTime SyncStartedAt { get; set; } = DateTime.UtcNow;
        public DateTime SyncCompletedAt { get; set; }

        public int RecordsSynced { get; set; }
        public int RecordsAdded { get; set; }
        public int RecordsUpdated { get; set; }

        [MaxLength(2000)]
        public string ErrorMessage { get; set; }
    }
}