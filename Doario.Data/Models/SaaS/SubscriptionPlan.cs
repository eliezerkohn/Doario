using System.ComponentModel.DataAnnotations;

namespace Doario.Data.Models.SaaS;

public class SubscriptionPlan
{
    public Guid SubscriptionPlanId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(200)]
    public string Description { get; set; }

    public decimal MonthlyPrice { get; set; }
    public int IncludedDocuments { get; set; }
    public decimal ExtraDocumentPrice { get; set; }

    /// <summary>
    /// Stripe Price ID for this plan.
    /// Used when creating a subscription via Stripe on Day 18.
    /// </summary>
    [MaxLength(100)]
    public string StripePriceId { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsPublic { get; set; } = true;

    public int SortOrder { get; set; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime EndDate { get; set; } = DateTime.MaxValue;
}