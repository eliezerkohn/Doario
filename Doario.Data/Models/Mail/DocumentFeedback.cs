using System;
using System.ComponentModel.DataAnnotations;
using Doario.Data.Models.SaaS;

namespace Doario.Data.Models.Mail
{
    /// <summary>
    /// Stores admin corrections to AI classification.
    /// Used to improve future AI prompts for this tenant.
    /// e.g. "AI said spam but admin moved it to mail"
    /// </summary>
    public class DocumentFeedback
    {
        public Guid DocumentFeedbackId { get; set; }

        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; }

        public Guid DocumentId { get; set; }
        public Document Document { get; set; }

        /// <summary>What the AI classified it as (spam/promotion/mail)</summary>
        [Required, MaxLength(50)]
        public string AiClassification { get; set; }

        /// <summary>What the admin corrected it to (always "mail" for now)</summary>
        [Required, MaxLength(50)]
        public string CorrectedClassification { get; set; }

        /// <summary>Short snippet of the document to help AI recognise similar ones</summary>
        [MaxLength(500)]
        public string DocumentSnippet { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}