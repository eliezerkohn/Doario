using System;
using System.ComponentModel.DataAnnotations;

using Doario.Data.Models.SaaS;

namespace Doario.Data.Models.Mail
{
    public class ImportedStaff
    {
        public Guid ImportedStaffId { get; set; }

        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; }

        [Required, MaxLength(100)]
        public string FirstName { get; set; }

        [Required, MaxLength(100)]
        public string LastName { get; set; }

        [Required, MaxLength(200)]
        public string Email { get; set; }

        [MaxLength(200)]
        public string JobTitle { get; set; }

        [MaxLength(200)]
        public string Department { get; set; }

        /// <summary>
        /// "Manual" | "M365Sync" | "CSVImport"
        /// </summary>
        [MaxLength(50)]
        public string Source { get; set; } = "Manual";

        public bool IsActive { get; set; } = true;

        public bool IsAdmin { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}