using System.ComponentModel.DataAnnotations;

namespace Doario.Data.Models.SaaS;

/// <summary>
/// Global promo codes created by the Doario operator.
/// Tenants redeem these by entering the code in their Billing page.
/// A redeemed code creates a TenantPromo row linking the tenant to this code.
/// </summary>
public class PromoCode
{
    public Guid PromoCodeId { get; set; }

    /// <summary>
    /// The code the client enters. Case-insensitive.
    /// e.g. WELCOME50, PARTNER2026
    /// </summary>
    [Required, MaxLength(50)]
    public string Code { get; set; }

    /// <summary>
    /// Internal description of what this promo is for.
    /// e.g. "50% off for first 3 months — new clients Q2 2026"
    /// </summary>
    [MaxLength(500)]
    public string Description { get; set; }

    /// <summary>
    /// Percentage discount applied to the monthly base price.
    /// 0 = no base price discount.
    /// e.g. 20 = 20% off the monthly plan price.
    /// </summary>
    public decimal BaseDiscountPercent { get; set; } = 0;

    /// <summary>
    /// Percentage discount applied to the per-document charge.
    /// 0 = no percentage discount.
    /// Takes priority over FlatDiscountPerDoc.
    /// </summary>
    public decimal DiscountPercent { get; set; } = 0;

    /// <summary>
    /// Flat dollar amount discounted per document charged.
    /// 0 = no flat discount.
    /// Only used if DiscountPercent = 0.
    /// </summary>
    public decimal FlatDiscountPerDoc { get; set; } = 0;

    /// <summary>
    /// Number of free documents granted per billing period.
    /// Added on top of the plan's IncludedDocuments.
    /// 0 = no free document bonus.
    /// </summary>
    public int FreeDocCount { get; set; } = 0;

    /// <summary>
    /// Maximum number of tenants that can redeem this code.
    /// 0 = unlimited.
    /// </summary>
    public int MaxRedemptions { get; set; } = 0;

    /// <summary>
    /// UTC date this promo becomes active.
    /// </summary>
    public DateTime StartsAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// UTC date this promo expires. DateTime.MaxValue = never expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; } = DateTime.MaxValue;

    /// <summary>
    /// Soft toggle — operator can disable without deleting.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<TenantPromo> Redemptions { get; set; } = new List<TenantPromo>();
}