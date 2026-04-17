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
    public class DocumentAssignment
    {
        public Guid DocumentAssignmentId { get; set; }

        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; }

        public Guid DocumentId { get; set; }
        public Document Document { get; set; }

        public int AssignmentTypeId { get; set; }
        public AssignmentType AssignmentType { get; set; }

        public Guid AssignedToStaffId { get; set; }
        public ImportedStaff AssignedToStaff { get; set; }

        public Guid AssignedByStaffId { get; set; }

        [Required, MaxLength(200)]
        public string AssignedToEmail { get; set; }

        public bool AssignedByAI { get; set; } = false;
        public decimal AIConfidence { get; set; }
        public bool AIConfirmedByAdmin { get; set; } = false;

        [MaxLength(200)]
        public string AISuggestedEmail { get; set; }

        [MaxLength(1000)]
        public string Note { get; set; }

        /// <summary>
        /// Secure token included in staff email URLs.
        /// Authenticates Mark as Actioned, Forward, Add Note
        /// without portal login. Generated using a cryptographically
        /// secure random string. Expires 30 days after AssignedAt.
        /// </summary>
        [MaxLength(100)]
        public string StaffAccessToken { get; set; }

        public DateTime StaffAccessTokenExpiresAt { get; set; }

        /// <summary>
        /// Secure token included in admin notification email URLs.
        /// Authenticates Confirm, Reassign, Mark Urgent actions
        /// without portal login.
        /// </summary>
        [MaxLength(100)]
        public string AdminAccessToken { get; set; }

        public DateTime AdminAccessTokenExpiresAt { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}