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
    public class ImportedStaff
    {
        public Guid ImportedStaffId { get; set; }

        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; }

        public int SourceTypeId { get; set; }
        public SourceType SourceType { get; set; }

        [MaxLength(200)]
        public string ExternalId { get; set; }

        [Required, MaxLength(200)]
        public string FirstName { get; set; }

        [Required, MaxLength(200)]
        public string LastName { get; set; }

        [Required, MaxLength(200)]
        public string Email { get; set; }

        [MaxLength(200)]
        public string JobTitle { get; set; }

        [MaxLength(200)]
        public string Department { get; set; }

        [MaxLength(200)]
        public string OfficeLocation { get; set; }

        [MaxLength(100)]
        public string EmployeeId { get; set; }

        /// <summary>
        /// True if this staff member is a Doario admin for this tenant.
        /// Populated from Azure AD App Role (DoarioAdmin) during Graph sync.
        /// Can be manually overridden by tenant admin — see IsAdminOverridden.
        /// </summary>
        public bool IsAdmin { get; set; } = false;

        /// <summary>
        /// True if IsAdmin was manually set by the tenant admin
        /// and should not be overwritten by the next Graph sync.
        /// False means IsAdmin is driven by Azure AD App Role.
        /// </summary>
        public bool IsAdminOverridden { get; set; } = false;

        public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime EndDate { get; set; } = DateTime.MaxValue;

        public static ImportedStaff CreateSystemUser(Guid tenantId)
        {
            return new ImportedStaff
            {
                ImportedStaffId = Guid.NewGuid(),
                TenantId = tenantId,
                SourceTypeId = 1,
                ExternalId = "SYSTEM",
                FirstName = "System",
                LastName = "",
                Email = "system@doario.com",
                JobTitle = "Automated Process",
                Department = "System",
                IsAdmin = false,
                IsAdminOverridden = true,
                LastSyncedAt = DateTime.UtcNow,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.MaxValue
            };
        }
    }
}