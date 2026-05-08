using Doario.Data;
using Doario.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace Doario.Web.Controllers;

[ApiController]
[Route("api/billing")]
[Authorize]
public class BillingController : ControllerBase
{
    private readonly DoarioDataContext _db;
    private readonly StripeService _stripeService;
    private readonly IConfiguration _config;
    private readonly ILogger<BillingController> _logger;
    private readonly TenantContext _tenant;

    public BillingController(
        DoarioDataContext db,
        StripeService stripeService,
        IConfiguration config,
        TenantContext tenant,
        ILogger<BillingController> logger)
    {
        _db = db;
        _stripeService = stripeService;
        _config = config;
        _tenant = tenant;
        _logger = logger;
    }

    // ── GET /api/billing/my-role ──────────────────────────────────────────────

    [HttpGet("my-role")]
    public IActionResult GetMyRole()
    {
        var isAdmin = User.IsInRole("DoarioAdmin");
        return Ok(new { isAdmin });
    }

    // ── GET /api/billing/summary ──────────────────────────────────────────────

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        if (!_tenant.IsResolved) return Unauthorized();
        var summary = await _stripeService.GetBillingSummaryAsync(_tenant.TenantId);
        return Ok(summary);
    }

    // ── GET /api/billing/usage ────────────────────────────────────────────────

    [HttpGet("usage")]
    public async Task<IActionResult> GetUsage([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (!_tenant.IsResolved) return Unauthorized();

        var periodStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = periodStart.AddMonths(1);

        var total = await _db.TenantBillingUsages
            .CountAsync(u => u.TenantId == _tenant.TenantId
                          && u.RecordedAt >= periodStart
                          && u.RecordedAt < periodEnd);

        var records = await _db.TenantBillingUsages
            .Where(u => u.TenantId == _tenant.TenantId
                     && u.RecordedAt >= periodStart
                     && u.RecordedAt < periodEnd)
            .OrderByDescending(u => u.RecordedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.TenantBillingUsageId,
                u.DocumentId,
                u.RecordedAt,
                u.ReportedToStripe,
                u.ReportedAt,
                u.Quantity
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, records });
    }

    // ── POST /api/billing/apply-promo ─────────────────────────────────────────

    [HttpPost("apply-promo")]
    public async Task<IActionResult> ApplyPromo([FromBody] ApplyPromoRequest request)
    {
        if (!_tenant.IsResolved) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(new { error = "Promo code is required." });

        try
        {
            var promo = await _stripeService.RedeemPromoCodeAsync(_tenant.TenantId, request.Code);
            return Ok(new
            {
                message = "Promo code applied successfully!",
                code = promo.Code,
                description = promo.Description,
                discountPercent = promo.DiscountPercent,
                flatDiscountPerDoc = promo.FlatDiscountPerDoc,
                freeDocCount = promo.FreeDocCount
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ── POST /api/billing/setup-customer ─────────────────────────────────────

    [HttpPost("setup-customer")]
    [Authorize(Roles = "DoarioAdmin")]
    public async Task<IActionResult> SetupCustomer()
    {
        if (!_tenant.IsResolved) return Unauthorized();
        var customerId = await _stripeService.EnsureCustomerAsync(_tenant.TenantId);
        return Ok(new { stripeCustomerId = customerId });
    }

    // ── POST /api/billing/setup-subscription ─────────────────────────────────

    [HttpPost("setup-subscription")]
    [Authorize(Roles = "DoarioAdmin")]
    public async Task<IActionResult> SetupSubscription([FromBody] SetupSubscriptionRequest request)
    {
        if (!_tenant.IsResolved) return Unauthorized();
        var subscriptionId = await _stripeService.CreateMeteredSubscriptionAsync(
            _tenant.TenantId, request.TenantSubscriptionId);
        return Ok(new { stripeSubscriptionId = subscriptionId });
    }

    // ── GET /api/billing/debug ────────────────────────────────────────────────

    [HttpGet("debug")]
    [AllowAnonymous]
    public IActionResult Debug()
    {
        return Ok(new
        {
            isResolved = _tenant.IsResolved,
            tenantId = _tenant.IsResolved ? _tenant.TenantId : Guid.Empty,
            isAuthenticated = User.Identity?.IsAuthenticated,
            claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList(),
            items = HttpContext.Items.Keys.Select(k => k.ToString()).ToList()
        });
    }

    // ── POST /api/billing/webhook ─────────────────────────────────────────────

    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        var webhookSecret = _config["Stripe:WebhookSecret"];
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                webhookSecret);

            _logger.LogInformation("Stripe webhook received: {Type}", stripeEvent.Type);

            switch (stripeEvent.Type)
            {
                case "invoice.payment_succeeded":
                    var invoice = stripeEvent.Data.Object as Invoice;
                    _logger.LogInformation(
                        "Stripe invoice paid: {InvoiceId} customer {CustomerId} amount {Amount}",
                        invoice?.Id, invoice?.CustomerId, invoice?.AmountPaid);
                    break;

                case "invoice.payment_failed":
                    var failedInvoice = stripeEvent.Data.Object as Invoice;
                    _logger.LogWarning(
                        "Stripe invoice payment FAILED: {InvoiceId} customer {CustomerId}",
                        failedInvoice?.Id, failedInvoice?.CustomerId);
                    break;

                case "customer.subscription.deleted":
                    var subscription = stripeEvent.Data.Object as Subscription;
                    _logger.LogWarning(
                        "Stripe subscription deleted: {SubId} customer {CustomerId}",
                        subscription?.Id, subscription?.CustomerId);

                    if (subscription != null)
                    {
                        await _db.TenantSubscriptions
                            .Where(s => s.StripeSubscriptionId == subscription.Id
                                     && s.EndDate == DateTime.MaxValue)
                            .ExecuteUpdateAsync(s => s
                                .SetProperty(x => x.EndDate, DateTime.UtcNow));
                    }
                    break;

                default:
                    _logger.LogDebug("Stripe webhook unhandled event type: {Type}", stripeEvent.Type);
                    break;
            }

            return Ok();
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook signature validation failed.");
            return BadRequest();
        }
    }
}

public class ApplyPromoRequest
{
    public string Code { get; set; }
}

public class SetupSubscriptionRequest
{
    public Guid TenantSubscriptionId { get; set; }
}