using System.ComponentModel.DataAnnotations;

namespace Doario.Data.Models.SaaS;

public class TenantSubscription
{
    public Guid TenantSubscriptionId { get; set; }

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; }

    /// <summary>
    /// Reference to the plan this subscription was created from.
    /// Follow this FK to get all pricing — MonthlyPrice, IncludedDocuments, ExtraDocumentPrice etc.
    /// </summary>
    public Guid SubscriptionPlanId { get; set; }
    public SubscriptionPlan SubscriptionPlan { get; set; }

    public decimal DiscountPercent { get; set; } = 0;

    /// <summary>
    /// Stripe Plan Price ID — legacy, StripePriceId on SubscriptionPlan is source of truth.
    /// </summary>
    [MaxLength(100)]
    public string StripePlanId { get; set; }

    [MaxLength(100)]
    public string StripeSubscriptionId { get; set; }

    /// <summary>
    /// The Stripe Subscription Item ID for the metered usage line item.
    /// Required to report usage records via Stripe's metered billing API.
    /// </summary>
    [MaxLength(100)]
    public string StripeSubscriptionItemId { get; set; }

    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime EndDate { get; set; } = DateTime.MaxValue;
}