using System;
using System.ComponentModel.DataAnnotations;
using Doario.Data.Models.SaaS;

namespace Doario.Data.Models.SaaS
{
    public class TenantMonitoredInbox
    {
        public Guid TenantMonitoredInboxId { get; set; }

        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; }

        /// <summary>
        /// The email address of the mailbox to monitor via Microsoft Graph.
        /// </summary>
        [Required, MaxLength(200)]
        public string EmailAddress { get; set; }

        /// <summary>
        /// Optional human-readable label e.g. "Main fax line", "Reception inbox".
        /// </summary>
        [MaxLength(200)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// If true, all documents from this inbox are treated as fax (SourceTypeId=10).
        /// If false, treated as email (SourceTypeId=11).
        /// </summary>
        public bool IsFaxInbox { get; set; } = false;

        /// <summary>
        /// How often this inbox is polled. In seconds. Default 60.
        /// </summary>
        public int PollingIntervalSeconds { get; set; } = 60;

        /// <summary>
        /// Last time this inbox was processed. Set to UtcNow at creation.
        /// </summary>
        public DateTime LastProcessedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this inbox was activated. Set to UtcNow at creation.
        /// </summary>
        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Soft delete — set to UtcNow to deactivate. DateTime.MaxValue = active.
        /// </summary>
        public DateTime EndDate { get; set; } = DateTime.MaxValue;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}