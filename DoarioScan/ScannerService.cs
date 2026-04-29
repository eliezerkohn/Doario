using NTwain;
using NTwain.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using DoarioScan.Models;

namespace DoarioScan;

public class ScannerService
{
    private readonly SettingsService _settingsService;

    // A page is blank if this fraction of sampled pixels are "light"
    // 0.98 = 98% of sampled pixels must be near-white
    private const double BlankThreshold = 0.98;

    // Pixel brightness threshold — 0-255, anything above this is "white"
    // 240 catches off-white scanner backgrounds
    private const int WhiteLevel = 240;

    // Sample every Nth pixel for performance — no need to check every pixel
    private const int SampleStep = 10;

    public ScannerService(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public List<string> GetAvailableScanners()
    {
        var scanners = new List<string>();
        try
        {
            var session = CreateSession();
            if (session.Open() == ReturnCode.Success)
            {
                foreach (var source in session.GetSources())
                    scanners.Add(source.Name);
                session.Close();
            }
        }
        catch { }
        return scanners;
    }

    public async Task<ScanResponse> ScanAsync(ScanRequest request)
    {
        var response = new ScanResponse();
        var settings = _settingsService.Load();

        var scannerName = string.IsNullOrWhiteSpace(request.Scanner)
            ? settings.SelectedScanner : request.Scanner;

        var dpi = request.Dpi > 0 ? request.Dpi : settings.Dpi;

        var colorMode = string.IsNullOrWhiteSpace(request.ColorMode)
            ? settings.ColorMode : request.ColorMode;

        if (string.IsNullOrWhiteSpace(scannerName))
        {
            response.Success = false;
            response.Error = "No scanner configured. Open DoarioScan settings and select a scanner.";
            return response;
        }

        var pages = new List<string>();
        var tcs = new TaskCompletionSource<bool>();
        var scanError = string.Empty;

        try
        {
            var session = CreateSession();

            session.TransferReady += (s, e) =>
            {
                e.CancelAll = false;
            };

            session.DataTransferred += (s, e) =>
            {
                try
                {
                    if (e.NativeData != IntPtr.Zero)
                    {
                        var dibHandle = e.NativeData;
                        var ptr = GlobalLock(dibHandle);
                        try
                        {
                            var infoHeader = Marshal.PtrToStructure<BITMAPINFOHEADER>(ptr);
                            int width = infoHeader.biWidth;
                            int height = Math.Abs(infoHeader.biHeight);
                            int bitCount = infoHeader.biBitCount;
                            int stride = ((width * bitCount + 31) / 32) * 4;

                            int clrUsed = (int)infoHeader.biClrUsed;
                            if (clrUsed == 0 && bitCount <= 8)
                                clrUsed = 1 << bitCount;
                            int colorBytes = clrUsed * 4;
                            int headerSize = Marshal.SizeOf<BITMAPINFOHEADER>();
                            var pixelPtr = IntPtr.Add(ptr, headerSize + colorBytes);

                            var pixelFormat = bitCount == 24
                                ? PixelFormat.Format24bppRgb
                                : PixelFormat.Format8bppIndexed;

                            using var bmp = new Bitmap(width, height, pixelFormat);
                            var bmpData = bmp.LockBits(
                                new Rectangle(0, 0, width, height),
                                ImageLockMode.WriteOnly,
                                pixelFormat);

                            // DIB is bottom-up — flip rows
                            for (int row = 0; row < height; row++)
                            {
                                var src = IntPtr.Add(pixelPtr, (height - 1 - row) * stride);
                                var dest = IntPtr.Add(bmpData.Scan0, row * bmpData.Stride);
                                RtlMoveMemory(dest, src, (uint)stride);
                            }

                            bmp.UnlockBits(bmpData);

                            if (bitCount == 8)
                            {
                                var palette = bmp.Palette;
                                for (int i = 0; i < 256; i++)
                                    palette.Entries[i] = Color.FromArgb(i, i, i);
                                bmp.Palette = palette;
                            }

                            // ── Blank page detection ──────────────────────────
                            // Sample pixels — if nearly all are near-white, treat
                            // this page as a blank separator and add empty string.
                            // Empty string = blank page signal to the backend splitter.
                            if (IsBlankPage(bmp))
                            {
                                lock (pages) { pages.Add(string.Empty); }
                                return;
                            }

                            using var ms = new MemoryStream();
                            bmp.Save(ms, ImageFormat.Png);
                            var base64 = Convert.ToBase64String(ms.ToArray());
                            lock (pages) { pages.Add(base64); }
                        }
                        finally
                        {
                            GlobalUnlock(dibHandle);
                        }
                    }
                }
                catch (Exception ex)
                {
                    scanError = ex.Message;
                }
            };

            session.SourceDisabled += (s, e) => tcs.TrySetResult(true);

            session.TransferError += (s, e) =>
            {
                scanError = e.Exception?.Message ?? "Transfer error during scan.";
                tcs.TrySetResult(false);
            };

            if (session.Open() != ReturnCode.Success)
            {
                response.Success = false;
                response.Error = "Failed to open TWAIN session.";
                return response;
            }

            var source = session.GetSources()
                .FirstOrDefault(s => s.Name == scannerName);

            if (source == null)
            {
                session.Close();
                response.Success = false;
                response.Error = $"Scanner '{scannerName}' not found. Check it is connected and turned on.";
                return response;
            }

            if (source.Open() != ReturnCode.Success)
            {
                session.Close();
                response.Success = false;
                response.Error = "Failed to open scanner. Check it is connected and not in use.";
                return response;
            }

            ConfigureScanner(source, dpi, colorMode);

            source.Enable(SourceEnableMode.NoUI, false, IntPtr.Zero);

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(120_000));

            source.Close();
            session.Close();

            if (completed != tcs.Task)
            {
                response.Success = false;
                response.Error = "Scan timed out after 120 seconds.";
                return response;
            }

            if (!string.IsNullOrWhiteSpace(scanError))
            {
                response.Success = false;
                response.Error = scanError;
                return response;
            }

            response.Success = true;
            response.Pages = pages;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Error = $"Scan failed: {ex.Message}";
        }

        return response;
    }

    // ── Blank page detection ──────────────────────────────────────────────────
    // Samples every SampleStep-th pixel. Converts to grayscale brightness.
    // If >= BlankThreshold of sampled pixels are above WhiteLevel, it's blank.

    private static bool IsBlankPage(Bitmap bmp)
    {
        try
        {
            int total = 0;
            int light = 0;

            for (int y = 0; y < bmp.Height; y += SampleStep)
            {
                for (int x = 0; x < bmp.Width; x += SampleStep)
                {
                    var pixel = bmp.GetPixel(x, y);
                    // Perceived brightness (standard luminance weights)
                    var brightness = (int)(pixel.R * 0.299 + pixel.G * 0.587 + pixel.B * 0.114);
                    total++;
                    if (brightness >= WhiteLevel)
                        light++;
                }
            }

            if (total == 0) return true;

            return (double)light / total >= BlankThreshold;
        }
        catch
        {
            // If pixel analysis fails for any reason, don't treat it as blank
            return false;
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private TwainSession CreateSession()
    {
        var appId = TWIdentity.CreateFromAssembly(
            DataGroups.Image,
            Assembly.GetExecutingAssembly());
        return new TwainSession(appId);
    }

    private void ConfigureScanner(DataSource source, int dpi, string colorMode)
    {
        Try(() => source.Capabilities.ICapXResolution.SetValue((TWFix32)dpi));
        Try(() => source.Capabilities.ICapYResolution.SetValue((TWFix32)dpi));

        var pixelType = colorMode.ToLower() == "color"
            ? PixelType.RGB : PixelType.Gray;

        Try(() => source.Capabilities.ICapPixelType.SetValue(pixelType));
        Try(() => source.Capabilities.CapDuplexEnabled.SetValue(BoolType.True));
        Try(() => source.Capabilities.CapFeederEnabled.SetValue(BoolType.True));
        Try(() => source.Capabilities.ICapXferMech.SetValue(XferMech.Native));
    }

    private void Try(Action action)
    {
        try { action(); }
        catch { }
    }

    // ── P/Invoke ──────────────────────────────────────────────────────────────

    [DllImport("kernel32.dll")]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll")]
    private static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
    private static extern void RtlMoveMemory(IntPtr dest, IntPtr src, uint len);
}

// ── Native structs ────────────────────────────────────────────────────────────

[StructLayout(LayoutKind.Sequential)]
public struct BITMAPINFOHEADER
{
    public uint biSize;
    public int biWidth;
    public int biHeight;
    public ushort biPlanes;
    public ushort biBitCount;
    public uint biCompression;
    public uint biSizeImage;
    public int biXPelsPerMeter;
    public int biYPelsPerMeter;
    public uint biClrUsed;
    public uint biClrImportant;
}