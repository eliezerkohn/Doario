using System;
using System.ComponentModel.DataAnnotations;
using Doario.Data.Models.SaaS;

namespace Doario.Data.Models.SaaS
{
    public class TenantInboxSettings
    {
        public Guid TenantInboxSettingsId { get; set; }
        public Guid TenantId { get; set; }

        /// <summary>
        /// How often the background service polls monitored inboxes.
        /// In seconds. Default 60. Kept for legacy — individual inboxes
        /// now have their own PollingIntervalSeconds on TenantMonitoredInbox.
        /// </summary>
        public int InboxPollingIntervalSeconds { get; set; } = 60;

        /// <summary>
        /// How often the background service syncs staff from M365.
        /// In hours. Default 24.
        /// </summary>
        public int StaffSyncIntervalHours { get; set; } = 24;

        /// <summary>
        /// Last time staff sync ran. Set to UtcNow at creation.
        /// </summary>
        public DateTime LastStaffSyncAt { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [Required]
        public Tenant Tenant { get; set; }
    }
}