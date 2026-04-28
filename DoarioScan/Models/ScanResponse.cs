namespace DoarioScan.Models;

public class ScanResponse
{
    /// <summary>
    /// Whether the scan succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Scanned pages as base64 encoded PNG images.
    /// One entry per page scanned.
    /// Portal displays these as the preview.
    /// </summary>
    public List<string> Pages { get; set; } = new();

    /// <summary>
    /// Error message if Success = false.
    /// </summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// Total pages scanned.
    /// </summary>
    public int PageCount => Pages.Count;
}