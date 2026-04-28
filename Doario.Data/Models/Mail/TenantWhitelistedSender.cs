using System;
using System.ComponentModel.DataAnnotations;
using Doario.Data.Models.SaaS;

namespace Doario.Data.Models.Mail
{
    /// <summary>
    /// Stores sender domains or names that the admin has marked as
    /// "always treat as real mail" — never classify as spam or promotion.
    /// Created when admin clicks "Not Spam" or "Not Promotion".
    /// </summary>
    public class TenantWhitelistedSender
    {
        public Guid TenantWhitelistedSenderId { get; set; }

        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; }

        /// <summary>
        /// The sender domain extracted from the document OCR text.
        /// e.g. "medlinesupply.com" or "MedLine Supply Co."
        /// Matched case-insensitively against future document OCR text.
        /// </summary>
        [Required, MaxLength(200)]
        public string SenderIdentifier { get; set; }

        /// <summary>How this was added — always "AdminOverride" for now</summary>
        [MaxLength(50)]
        public string Source { get; set; } = "AdminOverride";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime EndDate { get; set; } = new DateTime(9999, 12, 31);
    }
}