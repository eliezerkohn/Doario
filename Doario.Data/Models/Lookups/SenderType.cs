using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Doario.Data.Models.SaaS;

namespace Doario.Data.Models.Lookups
{
    public class SenderType
    {
        public Guid SenderTypeId { get; set; }

        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(200)]
        public string TableName { get; set; }

        [MaxLength(200)]
        public string IdColumn { get; set; }

        [MaxLength(200)]
        public string DisplayNameColumn { get; set; }

        public int SortOrder { get; set; }

        /// <summary>
        /// Controls how much document content is included in delivery emails.
        /// 1=Full — full OCR text in email body
        /// 2=SummaryOnly — first paragraph only
        /// 3=LinkOnly — no OCR in email, staff clicks link to view in SharePoint
        /// For HIPAA tenants this is always overridden to 3 regardless of this value.
        /// </summary>
        public int EmailContentLevel { get; set; } = 1;

        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime EndDate { get; set; } = DateTime.MaxValue;

        public static IEnumerable<SenderType> CreateDefaults(Guid tenantId)
        {
            var now = DateTime.UtcNow;
            var never = DateTime.MaxValue;
            return new[]
            {
                new SenderType { SenderTypeId = Guid.NewGuid(), TenantId = tenantId, Name = "Unknown",    SortOrder = 100, EmailContentLevel = 1, StartDate = now, EndDate = never },
                new SenderType { SenderTypeId = Guid.NewGuid(), TenantId = tenantId, Name = "Student",    SortOrder = 200, EmailContentLevel = 1, StartDate = now, EndDate = never },
                new SenderType { SenderTypeId = Guid.NewGuid(), TenantId = tenantId, Name = "Parent",     SortOrder = 300, EmailContentLevel = 1, StartDate = now, EndDate = never },
                new SenderType { SenderTypeId = Guid.NewGuid(), TenantId = tenantId, Name = "Vendor",     SortOrder = 400, EmailContentLevel = 1, StartDate = now, EndDate = never },
                new SenderType { SenderTypeId = Guid.NewGuid(), TenantId = tenantId, Name = "Government", SortOrder = 500, EmailContentLevel = 1, StartDate = now, EndDate = never },
            };
        }
    }
}