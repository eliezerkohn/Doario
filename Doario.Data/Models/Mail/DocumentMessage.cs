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
    /// <summary>
    /// Audit log of every action taken on a document.
    /// Created when staff marks as actioned, adds a note, forwards,
    /// views in SharePoint, or when admin confirms/reassigns.
    /// Also created by system for automated actions.
    /// Uses Tenant.SystemStaffId for StaffId on system-generated rows.
    /// </summary>
    public class DocumentMessage
    {
        public Guid DocumentMessageId { get; set; }

        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; }

        public Guid DocumentId { get; set; }
        public Document Document { get; set; }

        /// <summary>
        /// Staff member who performed the action.
        /// Uses Tenant.SystemStaffId for system-generated messages
        /// so this column is never null.
        /// </summary>
        public Guid StaffId { get; set; }
        public ImportedStaff Staff { get; set; }

        public int MessageTypeId { get; set; }
        public MessageType MessageType { get; set; }

        /// <summary>
        /// Body of the message or note.
        /// Empty string for action types where no text is needed.
        /// Contains note text for Note type.
        /// Contains "Forwarded to {FullName}" for Forwarded type.
        /// </summary>
        [MaxLength(2000)]
        public string Body { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}