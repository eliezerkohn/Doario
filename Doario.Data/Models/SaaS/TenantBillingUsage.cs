using Doario.Data.Models.Mail;

namespace Doario.Data.Models.SaaS;

public class TenantBillingUsage
{
    public Guid TenantBillingUsageId { get; set; }

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; }

    /// <summary>
    /// The document that triggered this billable event.
    /// </summary>
    public Guid DocumentId { get; set; }
    public Document Document { get; set; }

    /// <summary>
    /// UTC timestamp when this usage event was recorded locally.
    /// </summary>
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// True once this record has been successfully reported to Stripe.
    /// Background job sets this after a successful usage record API call.
    /// </summary>
    public bool ReportedToStripe { get; set; } = false;

    /// <summary>
    /// The Stripe usage record ID returned after reporting.
    /// Null until ReportedToStripe = true.
    /// </summary>
    public string StripeUsageRecordId { get; set; }

    /// <summary>
    /// UTC timestamp when this usage was reported to Stripe.
    /// Null until ReportedToStripe = true.
    /// </summary>
    public DateTime ReportedAt { get; set; } = DateTime.MinValue;

    /// <summary>
    /// The quantity reported. Always 1 per document.
    /// Stored for audit purposes.
    /// </summary>
    public int Quantity { get; set; } = 1;
}