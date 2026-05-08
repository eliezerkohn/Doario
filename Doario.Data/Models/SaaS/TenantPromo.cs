namespace Doario.Data.Models.SaaS;

/// <summary>
/// Records that a tenant has redeemed a global PromoCode.
/// Created when the tenant enters a promo code in their Billing page.
/// Discount rules come from the linked PromoCode.
/// </summary>
public class TenantPromo
{
    public Guid TenantPromoId { get; set; }

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; }

    /// <summary>
    /// The global promo code that was redeemed.
    /// </summary>
    public Guid PromoCodeId { get; set; }
    public PromoCode PromoCode { get; set; }

    /// <summary>
    /// Soft toggle — operator can disable a tenant's promo without deleting.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// UTC timestamp when the tenant redeemed this promo.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}