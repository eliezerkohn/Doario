using System;
using System.ComponentModel.DataAnnotations;
using Doario.Data.Models.Lookups;
using Doario.Data.Models.SaaS;

namespace Doario.Data.Models.Mail
{
    public class DocumentAiSuggestion
    {
        public Guid DocumentAiSuggestionId { get; set; }
        public Guid DocumentId { get; set; }
        public Guid TenantId { get; set; }
        public Guid SuggestedStaffId { get; set; }

        [Required, MaxLength(200)]
        public string SuggestedEmail { get; set; }

        // 1-10 — always stored regardless of mode
        public int Confidence { get; set; }

        // FK → SuggestionStatus
        // 1=Pending, 2=Approved, 3=Overwritten, 4=AutoAssigned
        public int SuggestionStatusId { get; set; }

        // Staff who approved/overwrote — SystemStaffId until reviewed
        public Guid ReviewedByStaffId { get; set; }

        // DateTime.MaxValue until reviewed
        public DateTime ReviewedAt { get; set; } = DateTime.MaxValue;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [Required]
        public Document Document { get; set; }
        [Required]
        public Tenant Tenant { get; set; }
        [Required]
        public ImportedStaff SuggestedStaff { get; set; }
        [Required]
        public SuggestionStatus SuggestionStatus { get; set; }
    }
}