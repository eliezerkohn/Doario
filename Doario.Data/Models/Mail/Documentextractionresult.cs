using System.ComponentModel.DataAnnotations;
using Doario.Data.Models.SaaS;

namespace Doario.Data.Models.Mail;

/// <summary>
/// Stores individual AI-extracted field values for a document.
/// One row per extracted field — e.g. "Annual Income" = "$32,000".
/// BoundingBox is nullable — populated later when PDF highlight viewer is wired up.
/// </summary>
public class DocumentExtractionResult
{
    public Guid DocumentExtractionResultId { get; set; }

    public Guid DocumentId { get; set; }
    public Document Document { get; set; }

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; }

    /// <summary>
    /// The extraction field name — e.g. "Annual Income", "Applicant Name".
    /// </summary>
    [Required, MaxLength(200)]
    public string FieldName { get; set; }

    /// <summary>
    /// The value extracted by AI — e.g. "$32,000", "John Smith".
    /// </summary>
    [MaxLength(2000)]
    public string FieldValue { get; set; }

    /// <summary>
    /// Page number in the PDF where this value was found (1-based).
    /// Null until PDF highlight viewer is wired up.
    /// </summary>
    public int? PageNumber { get; set; }

    /// <summary>
    /// Bounding box coordinates on the page: "x,y,width,height" in points.
    /// Null until PDF highlight viewer is wired up.
    /// </summary>
    [MaxLength(100)]
    public string BoundingBox { get; set; }

    /// <summary>
    /// Whether staff has confirmed this value is correct.
    /// Null = not yet reviewed, true = confirmed, false = corrected.
    /// </summary>
    public bool? IsConfirmed { get; set; }

    /// <summary>
    /// Staff-corrected value if IsConfirmed = false.
    /// </summary>
    [MaxLength(2000)]
    public string CorrectedValue { get; set; }

    public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;
}