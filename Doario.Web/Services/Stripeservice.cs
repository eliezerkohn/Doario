using Doario.Data;
using Doario.Data.Models.SaaS;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Billing;

namespace Doario.Web.Services;

/// <summary>
/// Wraps all Stripe API interactions for Doario metered billing.
/// </summary>
public class StripeService
{
    private readonly DoarioDataContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<StripeService> _logger;
    private readonly string _meteredPriceId;

    public StripeService(DoarioDataContext db, IConfiguration config, ILogger<StripeService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;

        StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
        _meteredPriceId = _config["Stripe:MeteredPriceId"];
    }

    // -------------------------------------------------------------------------
    // CUSTOMER
    // -------------------------------------------------------------------------

    public async Task<string> EnsureCustomerAsync(Guid tenantId)
    {
        var tenant = await _db.Tenants.FindAsync(tenantId)
            ?? throw new InvalidOperationException($"Tenant {tenantId} not found.");

        if (!string.IsNullOrEmpty(tenant.StripeCustomerId))
            return tenant.StripeCustomerId;

        var service = new CustomerService();
        var customer = await service.CreateAsync(new CustomerCreateOptions
        {
            Name = tenant.Name,
            Email = tenant.MailboxAddress,
            Metadata = new Dictionary<string, string>
            {
                { "TenantId", tenantId.ToString() },
                { "Domain", tenant.Domain ?? "" }
            }
        });

        await _db.Tenants
            .Where(t => t.TenantId == tenantId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.StripeCustomerId, customer.Id));

        _logger.LogInformation("Created Stripe customer {CustomerId} for tenant {TenantId}", customer.Id, tenantId);
        return customer.Id;
    }

    // -------------------------------------------------------------------------
    // SUBSCRIPTION
    // -------------------------------------------------------------------------

    public async Task<string> CreateMeteredSubscriptionAsync(Guid tenantId, Guid tenantSubscriptionId)
    {
        var tenantSubscription = await _db.TenantSubscriptions
            .FirstOrDefaultAsync(s => s.TenantSubscriptionId == tenantSubscriptionId)
            ?? throw new InvalidOperationException($"TenantSubscription {tenantSubscriptionId} not found.");

        var customerId = await EnsureCustomerAsync(tenantId);

        var service = new SubscriptionService();
        var subscription = await service.CreateAsync(new SubscriptionCreateOptions
        {
            Customer = customerId,
            Items = new List<SubscriptionItemOptions>
            {
                new SubscriptionItemOptions { Price = _meteredPriceId }
            },
            Metadata = new Dictionary<string, string>
            {
                { "TenantId", tenantId.ToString() },
                { "TenantSubscriptionId", tenantSubscriptionId.ToString() }
            }
        });

        var itemId = subscription.Items.Data.First().Id;

        await _db.TenantSubscriptions
            .Where(s => s.TenantSubscriptionId == tenantSubscriptionId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.StripeSubscriptionId, subscription.Id)
                .SetProperty(x => x.StripeSubscriptionItemId, itemId));

        _logger.LogInformation(
            "Created Stripe subscription {SubId} item {ItemId} for tenant {TenantId}",
            subscription.Id, itemId, tenantId);

        return subscription.Id;
    }

    // -------------------------------------------------------------------------
    // USAGE REPORTING
    // -------------------------------------------------------------------------

    public async Task ReportUsageAsync(Guid tenantBillingUsageId)
    {
        var usage = await _db.TenantBillingUsages
            .Include(u => u.Tenant)
                .ThenInclude(t => t.Subscriptions)
            .FirstOrDefaultAsync(u => u.TenantBillingUsageId == tenantBillingUsageId)
            ?? throw new InvalidOperationException($"TenantBillingUsage {tenantBillingUsageId} not found.");

        if (usage.ReportedToStripe)
        {
            _logger.LogWarning("Usage {Id} already reported to Stripe — skipping.", tenantBillingUsageId);
            return;
        }

        var activeSubscription = usage.Tenant.Subscriptions
            .FirstOrDefault(s => s.EndDate == DateTime.MaxValue && !string.IsNullOrEmpty(s.StripeSubscriptionItemId))
            ?? throw new InvalidOperationException($"No active Stripe subscription found for tenant {usage.TenantId}.");

        var service = new SubscriptionItemUsageRecordService();
        await service.CreateAsync(
            activeSubscription.StripeSubscriptionItemId,
            new SubscriptionItemUsageRecordCreateOptions
            {
                Quantity = usage.Quantity,
                Timestamp = usage.RecordedAt,
                Action = "increment"
            });

        await _db.TenantBillingUsages
            .Where(u => u.TenantBillingUsageId == tenantBillingUsageId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.ReportedToStripe, true)
                .SetProperty(u => u.StripeUsageRecordId, "reported")
                .SetProperty(u => u.ReportedAt, DateTime.UtcNow));

        _logger.LogInformation(
            "Reported usage to Stripe for tenant {TenantId}", usage.TenantId);
    }

    public async Task FlushPendingUsageAsync(Guid tenantId)
    {
        var pending = await _db.TenantBillingUsages
            .Where(u => u.TenantId == tenantId && !u.ReportedToStripe)
            .ToListAsync();

        if (!pending.Any()) return;

        _logger.LogInformation("Flushing {Count} pending usage records for tenant {TenantId}", pending.Count, tenantId);

        foreach (var usage in pending)
        {
            try { await ReportUsageAsync(usage.TenantBillingUsageId); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to report usage {Id} to Stripe", usage.TenantBillingUsageId);
            }
        }
    }

    public async Task FlushAllPendingUsageAsync()
    {
        var tenantIds = await _db.TenantBillingUsages
            .Where(u => !u.ReportedToStripe)
            .Select(u => u.TenantId)
            .Distinct()
            .ToListAsync();

        foreach (var tenantId in tenantIds)
            await FlushPendingUsageAsync(tenantId);
    }

    // -------------------------------------------------------------------------
    // PROMOS
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the active redeemed promo for a tenant if one exists and is valid.
    /// Joins TenantPromo → PromoCode to get discount rules.
    /// </summary>
    public async Task<PromoCode> GetActivePromoAsync(Guid tenantId)
    {
        var now = DateTime.UtcNow;

        return await _db.TenantPromos
            .Include(tp => tp.PromoCode)
            .Where(tp => tp.TenantId == tenantId
                      && tp.IsActive
                      && tp.PromoCode.IsActive
                      && tp.PromoCode.StartsAt <= now
                      && tp.PromoCode.ExpiresAt > now)
            .OrderByDescending(tp => tp.CreatedAt)
            .Select(tp => tp.PromoCode)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Validates and redeems a promo code for a tenant.
    /// Returns the PromoCode if successful, throws if invalid.
    /// </summary>
    public async Task<PromoCode> RedeemPromoCodeAsync(Guid tenantId, string code)
    {
        var now = DateTime.UtcNow;
        var normalised = code.Trim().ToUpperInvariant();

        var promoCode = await _db.PromoCodes
            .FirstOrDefaultAsync(p => p.Code == normalised
                                   && p.IsActive
                                   && p.StartsAt <= now
                                   && p.ExpiresAt > now)
            ?? throw new InvalidOperationException("Promo code not found or expired.");

        // Check max redemptions
        if (promoCode.MaxRedemptions > 0)
        {
            var redemptionCount = await _db.TenantPromos
                .CountAsync(tp => tp.PromoCodeId == promoCode.PromoCodeId && tp.IsActive);

            if (redemptionCount >= promoCode.MaxRedemptions)
                throw new InvalidOperationException("This promo code has reached its maximum number of uses.");
        }

        // Check if tenant already redeemed this code
        var alreadyRedeemed = await _db.TenantPromos
            .AnyAsync(tp => tp.TenantId == tenantId && tp.PromoCodeId == promoCode.PromoCodeId);

        if (alreadyRedeemed)
            throw new InvalidOperationException("You have already redeemed this promo code.");

        // Deactivate any existing active promos for this tenant
        await _db.TenantPromos
            .Where(tp => tp.TenantId == tenantId && tp.IsActive)
            .ExecuteUpdateAsync(s => s.SetProperty(tp => tp.IsActive, false));

        // Create redemption
        _db.TenantPromos.Add(new TenantPromo
        {
            TenantPromoId = Guid.NewGuid(),
            TenantId = tenantId,
            PromoCodeId = promoCode.PromoCodeId,
            IsActive = true,
            CreatedAt = now
        });

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Tenant {TenantId} redeemed promo code {Code}", tenantId, normalised);

        return promoCode;
    }

    /// <summary>
    /// Calculates the effective per-document charge after applying any active promo.
    /// </summary>
    public async Task<decimal> GetEffectiveDocPriceAsync(Guid tenantId)
    {
        var subscription = await _db.TenantSubscriptions
            .Where(s => s.TenantId == tenantId && s.EndDate == DateTime.MaxValue)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync();

        if (subscription == null) return 0;

        var basePrice = subscription.ExtraDocumentPrice;
        var promo = await GetActivePromoAsync(tenantId);

        if (promo == null) return basePrice;
        if (promo.DiscountPercent > 0) return Math.Max(0, basePrice * (1 - promo.DiscountPercent / 100));
        if (promo.FlatDiscountPerDoc > 0) return Math.Max(0, basePrice - promo.FlatDiscountPerDoc);

        return basePrice;
    }

    public async Task<int> GetPromoFreeDocCountAsync(Guid tenantId)
    {
        var promo = await GetActivePromoAsync(tenantId);
        return promo?.FreeDocCount ?? 0;
    }

    // -------------------------------------------------------------------------
    // BILLING SUMMARY
    // -------------------------------------------------------------------------

    public async Task<BillingSummary> GetBillingSummaryAsync(Guid tenantId)
    {
        var subscription = await _db.TenantSubscriptions
            .Where(s => s.TenantId == tenantId && s.EndDate == DateTime.MaxValue)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync();

        if (subscription == null)
            return new BillingSummary { HasSubscription = false };

        var periodStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = periodStart.AddMonths(1);

        var totalDocsThisPeriod = await _db.TenantBillingUsages
            .CountAsync(u => u.TenantId == tenantId
                          && u.RecordedAt >= periodStart
                          && u.RecordedAt < periodEnd);

        var unreportedCount = await _db.TenantBillingUsages
            .CountAsync(u => u.TenantId == tenantId && !u.ReportedToStripe);

        var promo = await GetActivePromoAsync(tenantId);
        var promoFreeExtra = promo?.FreeDocCount ?? 0;
        var includedDocs = subscription.IncludedDocuments + promoFreeExtra;
        var billableDocs = Math.Max(0, totalDocsThisPeriod - includedDocs);
        var effectivePrice = await GetEffectiveDocPriceAsync(tenantId);
        var estimatedCharge = subscription.MonthlyPrice + (billableDocs * effectivePrice);

        return new BillingSummary
        {
            HasSubscription = true,
            PlanName = subscription.PlanName,
            MonthlyPrice = subscription.MonthlyPrice,
            IncludedDocuments = subscription.IncludedDocuments,
            PromoFreeDocCount = promoFreeExtra,
            TotalDocsThisPeriod = totalDocsThisPeriod,
            BillableDocs = billableDocs,
            EffectiveDocPrice = effectivePrice,
            EstimatedCharge = estimatedCharge,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            UnreportedCount = unreportedCount,
            ActivePromoCode = promo?.Code,
            ActivePromoDescription = promo?.Description,
            StripeSubscriptionId = subscription.StripeSubscriptionId
        };
    }
}

public class BillingSummary
{
    public bool HasSubscription { get; set; }
    public string PlanName { get; set; }
    public decimal MonthlyPrice { get; set; }
    public int IncludedDocuments { get; set; }
    public int PromoFreeDocCount { get; set; }
    public int TotalDocsThisPeriod { get; set; }
    public int BillableDocs { get; set; }
    public decimal EffectiveDocPrice { get; set; }
    public decimal EstimatedCharge { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int UnreportedCount { get; set; }
    public string ActivePromoCode { get; set; }
    public string ActivePromoDescription { get; set; }
    public string StripeSubscriptionId { get; set; }
}