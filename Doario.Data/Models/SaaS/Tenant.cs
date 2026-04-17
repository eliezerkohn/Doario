using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doario.Data.Models.SaaS
{
    public class Tenant
    {
        public Guid TenantId { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(100)]
        public string Domain { get; set; }

        public string SharePointSiteUrl { get; set; }
        public string SharePointSiteId { get; set; }
        public string AzureTenantId { get; set; }
        public string AzureClientId { get; set; }

        public Guid UnknownSenderId { get; set; }
        public Guid UnknownSenderTypeId { get; set; }
        public Guid SystemStaffId { get; set; }

        /// <summary>
        /// The mailbox Doario sends delivery emails from.
        /// e.g. mailroom@eislaasois.com
        /// Must have Mail.Send permission granted in Microsoft 365.
        /// </summary>
        [MaxLength(200)]
        public string MailboxAddress { get; set; }

        /// <summary>
        /// True if this tenant requires HIPAA-compliant handling.
        /// Enables: EmailContentLevel=LinkOnly by default,
        /// enhanced audit logging, BAA required before processing.
        /// </summary>
        public bool IsHipaaEnabled { get; set; } = false;

        /// <summary>
        /// Reference number or filename of the signed BAA document.
        /// Actual document stored securely outside the database.
        /// e.g. BAA_EisLaasois_Signed_20260415.pdf
        /// </summary>
        [MaxLength(200)]
        public string BAAReference { get; set; }

        /// <summary>
        /// Date the BAA was signed by both parties.
        /// </summary>
        public DateTime BAASignedDate { get; set; }

        /// <summary>
        /// Date the BAA expires — typically annual renewal.
        /// </summary>
        public DateTime BAAExpiryDate { get; set; }

        /// <summary>
        /// Dedicated inbound scan email address for this tenant.
        /// e.g. scan-eislaasois@mail.doario.com
        /// Doario monitors this mailbox via Microsoft Graph.
        /// When an email arrives with a PDF attachment it is automatically
        /// processed as a new document — no portal visit needed.
        /// Generated at tenant onboarding.
        /// </summary>
        [MaxLength(200)]
        public string ScanInboxAddress { get; set; }

        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime EndDate { get; set; } = DateTime.MaxValue;

        // Navigation
        public ICollection<TenantSubscription> Subscriptions { get; set; } = new List<TenantSubscription>();
        public ICollection<TenantConnection> Connections { get; set; } = new List<TenantConnection>();
    }
}