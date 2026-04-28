using System;
using Doario.Data.Models.SaaS;

namespace Doario.Data.Models.Mail
{
    /// <summary>
    /// Tracks which documents have been opened by any admin in the tenant.
    /// One row per document per tenant — not per individual admin.
    /// When any admin opens a document it is marked as read for the whole tenant.
    /// Deleting this row marks the document as unread for everyone again.
    /// </summary>
    public class DocumentViewed
    {
        public Guid DocumentViewedId { get; set; }
        public Guid TenantId { get; set; }
        public Guid DocumentId { get; set; }

        /// <summary>Which admin first opened it — stored for audit trail only</summary>
        public Guid ViewedByStaffId { get; set; }

        public DateTime ViewedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Tenant Tenant { get; set; }
        public Document Document { get; set; }
        public ImportedStaff ViewedByStaff { get; set; }
    }
}