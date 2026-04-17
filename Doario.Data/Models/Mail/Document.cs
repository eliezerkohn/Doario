using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Doario.Data.Models.Lookups;
using Doario.Data.Models.SaaS;

namespace Doario.Data.Models.Mail
{
    public class Document
    {
        public Guid DocumentId { get; set; }

        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; }

        public int DocumentStatusId { get; set; }
        public DocumentStatus DocumentStatus { get; set; }

        public Guid SenderTypeId { get; set; }
        public SenderType SenderType { get; set; }

        public Guid SenderId { get; set; }
        public Sender Sender { get; set; }

        public Guid UploadedByStaffId { get; set; }
        public ImportedStaff UploadedByStaff { get; set; }

        [Required, MaxLength(1000)]
        public string SharePointUrl { get; set; }

        /// <summary>
        /// Full text extracted by Azure Document Intelligence.
        /// Only nullable column in the schema — null until OCR runs.
        /// </summary>
        public string OcrText { get; set; }

        [Required, MaxLength(200)]
        public string SenderDisplayName { get; set; }

        /// <summary>
        /// Email address extracted from the document by AI.
        /// Empty string if not found.
        /// Displayed to staff in delivery email as a clickable contact link.
        /// Fed back into Senders table when admin confirms sender identity.
        /// </summary>
        [MaxLength(200)]
        public string SenderEmail { get; set; }

        /// <summary>
        /// AI confidence score for sender identification 0.00-1.00.
        /// Separate from DocumentAssignment.AIConfidence which tracks
        /// staff assignment confidence.
        /// </summary>
        public decimal SenderMatchConfidence { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}