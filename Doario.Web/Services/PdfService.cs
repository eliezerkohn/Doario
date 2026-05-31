using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;

namespace Doario.Web.Services;

/// <summary>
/// Builds a single multi-page PDF from a list of base64-encoded PNG images.
/// One image per page, each image scaled to fill an A4 page.
/// Saves to a temp file first to ensure a valid, complete PDF that Azure Document Intelligence accepts.
/// </summary>
public class PdfService
{
    /// <summary>
    /// Converts a list of base64 PNG strings into a PDF stream.
    /// Returns a valid PDF stream ready for upload and OCR.
    /// </summary>
    public Stream BuildPdfStream(List<string> base64Pages)
    {
        if (base64Pages == null || base64Pages.Count == 0)
            throw new ArgumentException("No pages provided.", nameof(base64Pages));

        var tempPath = Path.Combine(Path.GetTempPath(), $"doario_{Guid.NewGuid():N}.pdf");

        try
        {
            using (var document = new PdfDocument())
            {
                foreach (var base64 in base64Pages)
                {
                    if (string.IsNullOrWhiteSpace(base64)) continue;

                    var imageBytes = Convert.FromBase64String(base64);

                    try
                    {
                        using var xImage = XImage.FromStream(() => new MemoryStream(imageBytes));

                        // A4 at 72 dpi: 595 x 842 points
                        var page = document.AddPage();
                        page.Width = XUnit.FromPoint(595);
                        page.Height = XUnit.FromPoint(842);

                        using var gfx = XGraphics.FromPdfPage(page);

                        // Scale image to fit the page while preserving aspect ratio
                        double scaleX = page.Width.Point / xImage.PixelWidth;
                        double scaleY = page.Height.Point / xImage.PixelHeight;
                        double scale = Math.Min(scaleX, scaleY);

                        double drawW = xImage.PixelWidth * scale;
                        double drawH = xImage.PixelHeight * scale;
                        double drawX = (page.Width.Point - drawW) / 2;
                        double drawY = (page.Height.Point - drawH) / 2;

                        gfx.DrawImage(xImage, drawX, drawY, drawW, drawH);
                    }
                    finally
                    {
                        imageBytes = null;
                    }
                }

                // Save to temp file — ensures complete, valid PDF structure
                document.Save(tempPath);
            }

            // Read back into memory stream and return
            var output = new MemoryStream();
            using (var fs = File.OpenRead(tempPath))
            {
                fs.CopyTo(output);
            }
            output.Position = 0;
            return output;
        }
        finally
        {
            // Clean up temp file
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    /// <summary>
    /// Converts a list of base64 PNG strings into a PDF byte array.
    /// </summary>
    public byte[] BuildPdf(List<string> base64Pages)
    {
        using var stream = BuildPdfStream(base64Pages);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}