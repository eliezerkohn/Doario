namespace DoarioScan.Models;

public class BridgeSettings
{
    /// <summary>
    /// Doario backend API URL.
    /// e.g. https://app.doario.com or http://localhost:5006 for dev
    /// </summary>
    public string ApiBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Tenant API key — authenticates this Bridge with the Doario backend.
    /// Stored encrypted on disk via DPAPI — never in plain text.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Selected scanner name as returned by TWAIN.
    /// e.g. "Fujitsu fi-7160"
    /// </summary>
    public string SelectedScanner { get; set; } = string.Empty;

    /// <summary>
    /// Port the Bridge HTTP server listens on.
    /// Default 5100 — portal calls http://localhost:5100
    /// </summary>
    public int Port { get; set; } = 5100;

    /// <summary>
    /// Scan resolution in DPI.
    /// 300 is standard for documents — enough for OCR, not too large.
    /// </summary>
    public int Dpi { get; set; } = 300;

    /// <summary>
    /// Color mode for scanning.
    /// "grayscale" for most documents, "color" for photos or color forms.
    /// </summary>
    public string ColorMode { get; set; } = "grayscale";
}