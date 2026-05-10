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

        /// <summary>
        /// AI confidence score for sender identification 0.00-1.00.
        /// </summary>
        public decimal SenderMatchConfidence { get; set; }

        /// <summary>
        /// Clean AI-generated summary of the document.
        /// Null until AI summarisation runs.
        /// </summary>
        public string AiSummary { get; set; }

        /// <summary>
        /// For email/fax sources: the time the email was received in the mailbox.
        /// For scanner: the time the document was uploaded.
        /// </summary>
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The time Doario fetched and processed this document from the monitored inbox.
        /// Null for scanner documents.
        /// </summary>
        public DateTime? FetchedAt { get; set; }

        [Required, MaxLength(500)]
        public string OriginalFileName { get; set; }

        /// <summary>
        /// Source of this document — 10=Fax, 11=Email, 12=Scanner etc.
        /// </summary>
        public int SourceTypeId { get; set; } = 12;

        public SourceType SourceType { get; set; }

        /// <summary>
        /// Groups pages scanned together in one batch.
        /// </summary>
        public Guid? BatchScanId { get; set; }
        public int? BatchPageStart { get; set; }
        public int? BatchPageEnd { get; set; }

        /// <summary>
        /// The monitored inbox this document was fetched from.
        /// Null for scanner documents.
        /// </summary>
        public Guid? MonitoredInboxId { get; set; }
        public TenantMonitoredInbox MonitoredInbox { get; set; }
    }
}