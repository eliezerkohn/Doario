using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Doario.Data.Models.Mail
{
    public class TenantExtractionField
    {
        public Guid TenantExtractionFieldId { get; set; }
        public Guid TenantId { get; set; }

        [Required]
        public string FieldName { get; set; }

        public string FieldDescription { get; set; }

        public int SortOrder { get; set; }
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime EndDate { get; set; } = DateTime.MaxValue;

        // Navigation
        [Required]
        public SaaS.Tenant Tenant { get; set; }
    }
}
