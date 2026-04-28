using System.ComponentModel.DataAnnotations;

namespace Doario.Data.Models.SaaS;

public class TenantSubscription
{
    public Guid TenantSubscriptionId { get; set; }

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; }

    /// <summary>
    /// Reference to the plan catalog entry this subscription was created from.
    /// Nullable — subscriptions created before the plan catalog won't have this.
    /// </summary>
    public Guid? SubscriptionPlanId { get; set; }
    public SubscriptionPlan SubscriptionPlan { get; set; }

    [Required, MaxLength(50)]
    public string PlanName { get; set; }

    public decimal MonthlyPrice { get; set; }
    public int IncludedDocuments { get; set; }
    public decimal ExtraDocumentPrice { get; set; }
    public decimal DiscountPercent { get; set; } = 0;

    /// <summary>
    /// Price per staff member per month.
    /// Used when billing model is per-staff rather than per-document.
    /// 0 if using document-based pricing.
    /// </summary>
    public decimal PricePerStaff { get; set; } = 0;

    /// <summary>
    /// Documents included per staff member per month — pooled across tenant.
    /// e.g. 50 means a 10-person tenant gets 500 docs/month total pool.
    /// 0 if using document-based pricing.
    /// </summary>
    public int DocsPerStaff { get; set; } = 0;

    [Required, MaxLength(100)]
    public string StripePlanId { get; set; }

    [Required, MaxLength(100)]
    public string StripeSubscriptionId { get; set; }

    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime EndDate { get; set; } = DateTime.MaxValue;
}