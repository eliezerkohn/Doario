namespace DoarioScan.Models;

public class ScanRequest
{
    /// <summary>
    /// Scanner to use — if empty uses the configured default.
    /// </summary>
    public string Scanner { get; set; } = string.Empty;

    /// <summary>
    /// Scan resolution — if 0 uses the configured default.
    /// </summary>
    public int Dpi { get; set; } = 0;

    /// <summary>
    /// Color mode — "grayscale" or "color"
    /// If empty uses the configured default.
    /// </summary>
    public string ColorMode { get; set; } = string.Empty;
}