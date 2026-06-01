using Doario.Data;
using Doario.Data.Models.SaaS;
using Doario.Data.Repositories;
using Doario.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Doario.Web.Controllers;

[ApiController]
[Route("api/operator")]
[AllowAnonymous]
public class OperatorController : ControllerBase
{
    private readonly DoarioDataContext _db;
    private readonly StripeService _stripeService;
    private readonly ISubscriptionRepository _subRepo;
    private readonly ISubscriptionPlanRepository _planRepo;
    private readonly EmailDeliveryService _emailService;
    private readonly ILogger<OperatorController> _logger;

    public OperatorController(
        DoarioDataContext db,
        StripeService stripeService,
        ISubscriptionRepository subRepo,
        ISubscriptionPlanRepository planRepo,
        EmailDeliveryService emailService,
        ILogger<OperatorController> logger)
    {
        _db = db;
        _stripeService = stripeService;
        _subRepo = subRepo;
        _planRepo = planRepo;
        _emailService = emailService;
        _logger = logger;
    }

    // ── GET /api/operator/tenants ─────────────────────────────────────────────

    [HttpGet("tenants")]
    public async Task<IActionResult> GetTenants([FromQuery] string search = null)
    {
        var query = _db.Tenants.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim().ToLower();
            query = query.Where(t =>
                t.Name.ToLower().Contains(q) ||
                t.Domain.ToLower().Contains(q) ||
                t.MailboxAddress.ToLower().Contains(q));
        }

        var tenants = await query
            .OrderBy(t => t.Name)
            .Select(t => new
            {
                t.TenantId,
                t.Name,
                t.Domain,
                t.MailboxAddress,
                t.StripeCustomerId,
                t.StartDate,
                t.EndDate,
                IsActive = t.EndDate == DateTime.MaxValue,
            })
            .ToListAsync();

        var tenantIds = tenants.Select(t => t.TenantId).ToList();
        var subscriptions = await _db.TenantSubscriptions
            .Include(s => s.SubscriptionPlan)
            .Where(s => tenantIds.Contains(s.TenantId) && s.EndDate == DateTime.MaxValue)
            .ToListAsync();

        var subMap = subscriptions.ToDictionary(s => s.TenantId);

        var result = tenants.Select(t =>
        {
            subMap.TryGetValue(t.TenantId, out var sub);
            return new
            {
                t.TenantId,
                t.Name,
                t.Domain,
                t.MailboxAddress,
                t.StripeCustomerId,
                t.IsActive,
                t.StartDate,
                PlanName = sub?.SubscriptionPlan?.Name,
                MonthlyPrice = sub?.SubscriptionPlan?.MonthlyPrice,
                NegotiatedDiscount = sub?.DiscountPercent ?? 0,
                PaymentFailed = sub?.PaymentFailedAt != null,
                PaymentFailureCount = sub?.PaymentFailureCount ?? 0,
                LastPaymentAt = sub?.LastPaymentAt,
                HasStripeSubscription = !string.IsNullOrEmpty(sub?.StripeSubscriptionId),
            };
        });

        return Ok(result);
    }

    // ── GET /api/operator/tenants/{id} ────────────────────────────────────────

    [HttpGet("tenants/{id:guid}")]
    public async Task<IActionResult> GetTenant(Guid id)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        if (tenant == null) return NotFound();

        var sub = await _db.TenantSubscriptions
            .Include(s => s.SubscriptionPlan)
            .Where(s => s.TenantId == id && s.EndDate == DateTime.MaxValue)
            .FirstOrDefaultAsync();

        var docCount = await _db.Documents.CountAsync(d => d.TenantId == id);
        var staffCount = await _db.ImportedStaff.CountAsync(s => s.TenantId == id && s.IsActive);

        var periodStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var docsThisMonth = await _db.TenantBillingUsages
            .CountAsync(u => u.TenantId == id && u.RecordedAt >= periodStart);

        var activePromo = await _db.TenantPromos
            .Include(tp => tp.PromoCode)
            .Where(tp => tp.TenantId == id && tp.IsActive)
            .Select(tp => tp.PromoCode)
            .FirstOrDefaultAsync();

        return Ok(new
        {
            tenant.TenantId,
            tenant.Name,
            tenant.Domain,
            tenant.MailboxAddress,
            tenant.SharePointSiteUrl,
            tenant.StripeCustomerId,
            tenant.IsHipaaEnabled,
            IsActive = tenant.EndDate == DateTime.MaxValue,
            tenant.StartDate,
            TotalDocuments = docCount,
            ActiveStaff = staffCount,
            DocsThisMonth = docsThisMonth,
            ActivePromoCode = activePromo?.Code,
            ActivePromoDescription = activePromo?.Description,
            Subscription = sub == null ? null : new
            {
                sub.TenantSubscriptionId,
                PlanName = sub.SubscriptionPlan?.Name,
                MonthlyPrice = sub.SubscriptionPlan?.MonthlyPrice,
                IncludedDocuments = sub.SubscriptionPlan?.IncludedDocuments,
                ExtraDocumentPrice = sub.SubscriptionPlan?.ExtraDocumentPrice,
                sub.DiscountPercent,
                sub.StripeSubscriptionId,
                sub.StripeSubscriptionItemId,
                sub.StartDate,
                sub.LastPaymentAt,
                sub.PaymentFailedAt,
                sub.PaymentFailureCount,
            }
        });
    }

    // ── POST /api/operator/tenants ────────────────────────────────────────────

    [HttpPost("tenants")]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Name is required." });
        if (string.IsNullOrWhiteSpace(request.Domain))
            return BadRequest(new { error = "Domain is required." });
        if (string.IsNullOrWhiteSpace(request.MailboxAddress))
            return BadRequest(new { error = "Mailbox address is required." });

        var exists = await _db.Tenants.AnyAsync(t => t.Domain == request.Domain.Trim().ToLower());
        if (exists)
            return BadRequest(new { error = "A tenant with this domain already exists." });

        var tenant = new Tenant
        {
            TenantId = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Domain = request.Domain.Trim().ToLower(),
            MailboxAddress = request.MailboxAddress.Trim().ToLower(),
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.MaxValue,
        };

        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Operator created tenant {TenantId} {Name}", tenant.TenantId, tenant.Name);

        return Ok(new { tenant.TenantId, tenant.Name, tenant.Domain, tenant.MailboxAddress });
    }

    // ── POST /api/operator/tenants/{id}/assign-plan ───────────────────────────

    [HttpPost("tenants/{id:guid}/assign-plan")]
    public async Task<IActionResult> AssignPlan(Guid id, [FromBody] AssignPlanRequest request)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        if (tenant == null) return NotFound(new { error = "Tenant not found." });

        var plan = await _planRepo.GetByIdAsync(request.SubscriptionPlanId);
        if (plan == null) return NotFound(new { error = "Plan not found." });

        var sub = await _subRepo.SwitchPlanAsync(id, plan);

        if (!string.IsNullOrEmpty(plan.StripePriceId))
        {
            try
            {
                await _stripeService.CreateMeteredSubscriptionAsync(id, sub.TenantSubscriptionId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Stripe subscription creation failed for tenant {TenantId}.", id);
            }
        }

        _logger.LogInformation("Operator assigned plan {Plan} to tenant {TenantId}", plan.Name, id);
        return Ok(new { message = $"Assigned {plan.Name} to {tenant.Name}.", planName = plan.Name });
    }

    // ── PUT /api/operator/tenants/{id}/negotiated-discount ───────────────────

    [HttpPut("tenants/{id:guid}/negotiated-discount")]
    public async Task<IActionResult> SetNegotiatedDiscount(Guid id, [FromBody] NegotiatedDiscountRequest request)
    {
        var sub = await _db.TenantSubscriptions
            .FirstOrDefaultAsync(s => s.TenantId == id && s.EndDate == DateTime.MaxValue);

        if (sub == null)
            return NotFound(new { error = "No active subscription found for this tenant." });

        var discount = Math.Clamp(request.DiscountPercent, 0, 100);

        await _db.TenantSubscriptions
            .Where(s => s.TenantSubscriptionId == sub.TenantSubscriptionId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.DiscountPercent, discount));

        _logger.LogInformation(
            "Operator set negotiated discount {Discount}% for tenant {TenantId}", discount, id);

        return Ok(new { message = $"Negotiated discount set to {discount}%." });
    }

    // ── PUT /api/operator/tenants/{id}/hipaa ──────────────────────────────────

    [HttpPut("tenants/{id:guid}/hipaa")]
    public async Task<IActionResult> SetHipaa(Guid id, [FromBody] SetHipaaRequest request)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        if (tenant == null) return NotFound();

        await _db.Tenants
            .Where(t => t.TenantId == id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsHipaaEnabled, request.Enabled));

        _logger.LogInformation(
            "Operator {Action} HIPAA for tenant {TenantId}",
            request.Enabled ? "enabled" : "disabled", id);

        return Ok(new { message = $"HIPAA {(request.Enabled ? "enabled" : "disabled")}." });
    }

    // ── GET /api/operator/tenants/{id}/billing ────────────────────────────────

    [HttpGet("tenants/{id:guid}/billing")]
    public async Task<IActionResult> GetBilling(Guid id)
    {
        var summary = await _stripeService.GetBillingSummaryAsync(id);
        return Ok(summary);
    }

    // ── PUT /api/operator/tenants/{id}/deactivate ─────────────────────────────

    [HttpPut("tenants/{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateTenant(Guid id)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        if (tenant == null) return NotFound();

        await _db.Tenants
            .Where(t => t.TenantId == id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.EndDate, DateTime.UtcNow));

        _logger.LogWarning("Operator deactivated tenant {TenantId}", id);
        return Ok(new { message = "Tenant deactivated." });
    }

    // ── GET /api/operator/plans ───────────────────────────────────────────────

    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans()
    {
        var plans = await _planRepo.GetAllAsync();
        return Ok(plans.Select(p => new
        {
            p.SubscriptionPlanId,
            p.Name,
            p.Description,
            p.MonthlyPrice,
            p.IncludedDocuments,
            p.ExtraDocumentPrice,
            p.StripePriceId,
            p.IsActive,
            p.IsPublic,
            p.SortOrder,
            p.StartDate,
            p.EndDate,
            IsCurrentlyActive = p.EndDate.Year == 9999,
            NeedsStripeSync = string.IsNullOrEmpty(p.StripePriceId) && p.EndDate.Year == 9999,
        }));
    }

    // ── POST /api/operator/plans ──────────────────────────────────────────────

    [HttpPost("plans")]
    public async Task<IActionResult> CreatePlan([FromBody] CreatePlanRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Plan name is required." });
        if (request.MonthlyPrice <= 0)
            return BadRequest(new { error = "Monthly price must be greater than 0." });

        string stripePriceId = null;
        try
        {
            stripePriceId = await _stripeService.CreatePriceAsync(request.Name, request.MonthlyPrice);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create Stripe price for plan {Name}.", request.Name);
        }

        var plan = new SubscriptionPlan
        {
            SubscriptionPlanId = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            MonthlyPrice = request.MonthlyPrice,
            IncludedDocuments = request.IncludedDocuments,
            ExtraDocumentPrice = request.ExtraDocumentPrice,
            StripePriceId = stripePriceId ?? string.Empty,
            IsActive = true,
            IsPublic = request.IsPublic,
            SortOrder = request.SortOrder,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.MaxValue,
            CreatedAt = DateTime.UtcNow,
        };

        await _planRepo.CreateAsync(plan);

        _logger.LogInformation(
            "Operator created plan {Name} at ${Price}/mo with Stripe price {StripePriceId}",
            plan.Name, plan.MonthlyPrice, stripePriceId ?? "none");

        return Ok(new
        {
            plan.SubscriptionPlanId,
            plan.Name,
            plan.MonthlyPrice,
            plan.StripePriceId,
            message = stripePriceId != null
                ? $"Plan created and linked to Stripe price {stripePriceId}."
                : "Plan created — Stripe price could not be created automatically."
        });
    }

    // ── POST /api/operator/plans/{id}/sync-stripe ─────────────────────────────

    [HttpPost("plans/{id:guid}/sync-stripe")]
    public async Task<IActionResult> SyncPlanToStripe(Guid id)
    {
        var plan = await _planRepo.GetByIdAsync(id);
        if (plan == null) return NotFound(new { error = "Plan not found." });

        try
        {
            var priceId = await _stripeService.CreatePriceAsync(plan.Name, plan.MonthlyPrice);

            await _db.SubscriptionPlans
                .Where(p => p.SubscriptionPlanId == id)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.StripePriceId, priceId));

            _logger.LogInformation(
                "Operator synced plan {Name} to Stripe price {PriceId}", plan.Name, priceId);

            return Ok(new
            {
                message = $"Stripe price created for {plan.Name}.",
                stripePriceId = priceId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync plan {Name} to Stripe", plan.Name);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // ── GET /api/operator/stats ───────────────────────────────────────────────

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var totalTenants = await _db.Tenants.CountAsync(t => t.EndDate == DateTime.MaxValue);
        var totalDocs = await _db.Documents.CountAsync();
        var failedPayments = await _db.TenantSubscriptions
            .CountAsync(s => s.PaymentFailedAt != null && s.EndDate == DateTime.MaxValue);
        var periodStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var docsThisMonth = await _db.TenantBillingUsages
            .CountAsync(u => u.RecordedAt >= periodStart);

        return Ok(new
        {
            TotalTenants = totalTenants,
            TotalDocuments = totalDocs,
            DocsThisMonth = docsThisMonth,
            FailedPayments = failedPayments,
        });
    }

    // ── GET /api/operator/promos ──────────────────────────────────────────────

    [HttpGet("promos")]
    public async Task<IActionResult> GetPromos()
    {
        var promos = await _db.PromoCodes
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.PromoCodeId,
                p.Code,
                p.Description,
                p.BaseDiscountPercent,
                p.DiscountPercent,
                p.FlatDiscountPerDoc,
                p.FreeDocCount,
                p.MaxRedemptions,
                p.StartsAt,
                p.ExpiresAt,
                p.IsActive,
                p.CreatedAt,
                IsExpired = p.ExpiresAt <= DateTime.UtcNow,
                RedemptionCount = _db.TenantPromos.Count(tp => tp.PromoCodeId == p.PromoCodeId && tp.IsActive),
            })
            .ToListAsync();

        return Ok(promos);
    }

    // ── POST /api/operator/promos ─────────────────────────────────────────────

    [HttpPost("promos")]
    public async Task<IActionResult> CreatePromo([FromBody] CreatePromoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(new { error = "Promo code is required." });

        var normalised = request.Code.Trim().ToUpperInvariant();

        var exists = await _db.PromoCodes.AnyAsync(p => p.Code == normalised);
        if (exists)
            return BadRequest(new { error = "A promo code with this code already exists." });

        var promo = new PromoCode
        {
            PromoCodeId = Guid.NewGuid(),
            Code = normalised,
            Description = request.Description?.Trim() ?? string.Empty,
            BaseDiscountPercent = request.BaseDiscountPercent,
            DiscountPercent = request.DiscountPercent,
            FlatDiscountPerDoc = request.FlatDiscountPerDoc,
            FreeDocCount = request.FreeDocCount,
            MaxRedemptions = request.MaxRedemptions,
            StartsAt = request.StartsAt ?? DateTime.UtcNow,
            ExpiresAt = request.ExpiresAt ?? DateTime.MaxValue,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        _db.PromoCodes.Add(promo);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Operator created promo code {Code}", normalised);

        return Ok(new { promo.PromoCodeId, promo.Code, message = "Promo code created." });
    }

    // ── POST /api/operator/promos/{id}/assign ────────────────────────────────

    [HttpPost("promos/{id:guid}/assign")]
    public async Task<IActionResult> AssignPromo(Guid id, [FromBody] AssignPromoRequest request)
    {
        var promo = await _db.PromoCodes.FindAsync(id);
        if (promo == null) return NotFound(new { error = "Promo code not found." });

        if (!request.TenantIds.Any())
            return BadRequest(new { error = "No tenants selected." });

        var results = new List<object>();

        foreach (var tenantId in request.TenantIds)
        {
            try
            {
                await _stripeService.AssignPromoToTenantAsync(tenantId, id);

                if (request.SendEmail)
                {
                    var tenant = await _db.Tenants.FindAsync(tenantId);
                    if (tenant != null)
                    {
                        var (success, error) = await _emailService.SendPromoEmailAsync(
                            toEmail: tenant.MailboxAddress,
                            toName: tenant.Name,
                            tenantName: tenant.Name,
                            promoCode: promo.Code,
                            promoDescription: promo.Description,
                            discountPercent: promo.DiscountPercent > 0 ? promo.DiscountPercent : null,
                            flatDiscountPerDoc: promo.FlatDiscountPerDoc > 0 ? promo.FlatDiscountPerDoc : null,
                            freeDocCount: promo.FreeDocCount > 0 ? promo.FreeDocCount : null);

                        results.Add(new { tenantId, tenant.Name, assigned = true, emailSent = success, emailError = success ? null : error });
                        continue;
                    }
                }

                results.Add(new { tenantId, assigned = true, emailSent = false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to assign promo {Code} to tenant {TenantId}", promo.Code, tenantId);
                results.Add(new { tenantId, assigned = false, error = ex.Message });
            }
        }

        return Ok(new { message = $"Promo {promo.Code} assigned to {results.Count} tenant(s).", results });
    }

    // ── POST /api/operator/promos/{id}/send-email ─────────────────────────────

    [HttpPost("promos/{id:guid}/send-email")]
    public async Task<IActionResult> SendPromoEmail(Guid id, [FromBody] SendPromoEmailRequest request)
    {
        var promo = await _db.PromoCodes.FindAsync(id);
        if (promo == null) return NotFound(new { error = "Promo code not found." });

        if (!request.TenantIds.Any())
            return BadRequest(new { error = "No tenants selected." });

        var results = new List<object>();

        foreach (var tenantId in request.TenantIds)
        {
            var tenant = await _db.Tenants.FindAsync(tenantId);
            if (tenant == null) { results.Add(new { tenantId, success = false, error = "Tenant not found." }); continue; }

            var (success, error) = await _emailService.SendPromoEmailAsync(
                toEmail: tenant.MailboxAddress,
                toName: tenant.Name,
                tenantName: tenant.Name,
                promoCode: promo.Code,
                promoDescription: promo.Description,
                discountPercent: promo.DiscountPercent > 0 ? promo.DiscountPercent : null,
                flatDiscountPerDoc: promo.FlatDiscountPerDoc > 0 ? promo.FlatDiscountPerDoc : null,
                freeDocCount: promo.FreeDocCount > 0 ? promo.FreeDocCount : null);

            results.Add(new { tenantId, tenant.Name, success, error = success ? null : error });
        }

        return Ok(new { message = $"Emails sent to {results.Count} tenant(s).", results });
    }
}

// ── Request DTOs ──────────────────────────────────────────────────────────────

public class CreateTenantRequest
{
    public string Name { get; set; }
    public string Domain { get; set; }
    public string MailboxAddress { get; set; }
}

public class AssignPlanRequest
{
    public Guid SubscriptionPlanId { get; set; }
}

public class NegotiatedDiscountRequest
{
    public decimal DiscountPercent { get; set; }
}

public class SetHipaaRequest
{
    public bool Enabled { get; set; }
}

public class CreatePlanRequest
{
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal MonthlyPrice { get; set; }
    public int IncludedDocuments { get; set; }
    public decimal ExtraDocumentPrice { get; set; }
    public bool IsPublic { get; set; } = true;
    public int SortOrder { get; set; } = 100;
}

public class CreatePromoRequest
{
    public string Code { get; set; }
    public string Description { get; set; }
    public decimal BaseDiscountPercent { get; set; } = 0;
    public decimal DiscountPercent { get; set; } = 0;
    public decimal FlatDiscountPerDoc { get; set; } = 0;
    public int FreeDocCount { get; set; } = 0;
    public int MaxRedemptions { get; set; } = 0;
    public DateTime? StartsAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class AssignPromoRequest
{
    public List<Guid> TenantIds { get; set; } = new();
    public bool SendEmail { get; set; } = true;
}

public class SendPromoEmailRequest
{
    public List<Guid> TenantIds { get; set; } = new();
}