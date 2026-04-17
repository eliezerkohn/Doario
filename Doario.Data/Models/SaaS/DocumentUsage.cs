using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Doario.Data.Models.Lookups;

namespace Doario.Data.Models.SaaS
{
    public class DocumentUsage
    {
        public Guid DocumentUsageId { get; set; }

        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; }

        public int SystemStatusId { get; set; } = 7;
        public SystemStatus SystemStatus { get; set; }

        public DateTime BillingMonth { get; set; }

        public int TotalDocuments { get; set; }
        public int IncludedDocuments { get; set; }
        public int ExtraDocuments { get; set; }

        public decimal MonthlyPrice { get; set; }
        public decimal ExtraCharges { get; set; }
        public decimal TotalCharged { get; set; }

        [MaxLength(200)]
        public string StripeInvoiceId { get; set; }

        /// <summary>
        /// Number of active staff members this billing month.
        /// Used to calculate included document pool for per-staff pricing.
        /// e.g. 10 staff x 50 docs = 500 doc pool for this month.
        /// </summary>
        public int ActiveStaffCount { get; set; }

        public DateTime BilledAt { get; set; }
    }
}