using System;
using System.ComponentModel.DataAnnotations;
using Doario.Data.Models.SaaS;

namespace Doario.Data.Models.SaaS
{
    public class TenantAiSettings
    {
        public Guid TenantAiSettingsId { get; set; }
        public Guid TenantId { get; set; }

        // "Off" | "AutoAssign" | "SuggestAndApprove"
        [Required, MaxLength(50)]
        public string AiAssignmentMode { get; set; } = "AutoAssign";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Minimum AI confidence (1-10) required for auto-assignment in SuggestAndApprove mode.
        /// Documents below this threshold go to Pending Approvals.
        /// Default 8 — only high-confidence suggestions auto-assign.
        /// </summary>
        public int AiConfidenceThreshold { get; set; } = 8;

        // Navigation
        [Required]
        public Tenant Tenant { get; set; }
    }
}