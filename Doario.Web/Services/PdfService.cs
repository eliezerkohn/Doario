using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;
using System.IO;

namespace Doario.Web.Services;

/// <summary>
/// Builds a single multi-page PDF from a list of base64-encoded PNG images.
/// One image per page, each image scaled to fill an A4 page.
/// </summary>
public class PdfService
{
    /// <summary>
    /// Converts a list of base64 PNG strings into a PDF byte array.
    /// </summary>
    public byte[] BuildPdf(List<string> base64Pages)
    {
        if (base64Pages == null || base64Pages.Count == 0)
            throw new ArgumentException("No pages provided.", nameof(base64Pages));

        using var document = new PdfDocument();

        foreach (var base64 in base64Pages)
        {
            if (string.IsNullOrWhiteSpace(base64)) continue;

            var imageBytes = Convert.FromBase64String(base64);

            using var ms = new MemoryStream(imageBytes);
            var xImage = XImage.FromStream(() => new MemoryStream(imageBytes));

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

        using var output = new MemoryStream();
        document.Save(output, false);
        return output.ToArray();
    }

    /// <summary>
    /// Builds the PDF and returns it as a stream ready for upload.
    /// </summary>
    public Stream BuildPdfStream(List<string> base64Pages)
    {
        var bytes = BuildPdf(base64Pages);
        return new MemoryStream(bytes);
    }
}