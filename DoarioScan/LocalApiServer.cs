using System.Net;
using System.Text;
using System.Text.Json;
using DoarioScan.Models;

namespace DoarioScan;

public class LocalApiServer
{
    private readonly ScannerService _scannerService;
    private readonly SettingsService _settingsService;
    private HttpListener _listener;
    private CancellationTokenSource _cts;
    private Task _serverTask;

    // Scanner cache — avoids calling TWAIN on every request
    private List<string> _cachedScanners = new();
    private DateTime _cacheTime = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);
    private readonly object _cacheLock = new();

    public LocalApiServer(ScannerService scannerService, SettingsService settingsService)
    {
        _scannerService = scannerService;
        _settingsService = settingsService;
    }

    public void Start()
    {
        var settings = _settingsService.Load();
        var port = settings.Port > 0 ? settings.Port : 5100;

        _cts = new CancellationTokenSource();
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        _listener.Start();
        _serverTask = Task.Run(() => ListenAsync(_cts.Token));
    }

    public void Stop()
    {
        try { _cts?.Cancel(); _listener?.Stop(); }
        catch { }
    }

    private async Task ListenAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(() => HandleAsync(context), ct);
            }
            catch (HttpListenerException) { break; }
            catch { }
        }
    }

    private async Task HandleAsync(HttpListenerContext context)
    {
        var req = context.Request;
        var res = context.Response;

        res.Headers.Add("Access-Control-Allow-Origin", "*");
        res.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
        res.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

        if (req.HttpMethod == "OPTIONS")
        {
            res.StatusCode = 200;
            res.Close();
            return;
        }

        var path = req.Url?.AbsolutePath?.ToLower() ?? string.Empty;

        try
        {
            switch (path)
            {
                case "/health": await HandleHealth(req, res); break;
                case "/scanners": await HandleScanners(req, res); break;
                case "/scan": await HandleScan(req, res); break;
                case "/upload": await HandleUpload(req, res); break;
                case "/confirm": await HandleConfirm(req, res); break;
                case "/rescan": await HandleRescan(req, res); break;
                default: await WriteJson(res, 404, new { error = "Not found" }); break;
            }
        }
        catch (Exception ex)
        {
            await WriteJson(res, 500, new { error = ex.Message });
        }
        finally
        {
            res.Close();
        }
    }

    // ── Scanner cache helper ──────────────────────────────────────────────────

    /// <summary>
    /// Returns cached scanner list.
    /// Forces a fresh TWAIN scan if cache is stale or refresh=true is passed.
    /// </summary>
    private List<string> GetScanners(bool forceRefresh = false)
    {
        lock (_cacheLock)
        {
            if (forceRefresh || DateTime.UtcNow - _cacheTime > CacheDuration)
            {
                try
                {
                    _cachedScanners = _scannerService.GetAvailableScanners();
                    _cacheTime = DateTime.UtcNow;
                }
                catch
                {
                    // If TWAIN fails keep old cache
                }
            }
            return _cachedScanners;
        }
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private async Task HandleHealth(HttpListenerRequest req, HttpListenerResponse res)
    {
        var settings = _settingsService.Load();
        var isConfigured = _settingsService.IsConfigured(settings);

        // Health check uses cache — no TWAIN call unless stale
        var scanners = GetScanners();
        var scannerReady = scanners.Contains(settings.SelectedScanner);

        await WriteJson(res, 200, new
        {
            status = "ok",
            version = "1.0.0",
            isConfigured,
            scannerReady,
            selectedScanner = settings.SelectedScanner,
        });
    }

    private async Task HandleScanners(HttpListenerRequest req, HttpListenerResponse res)
    {
        if (req.HttpMethod != "GET")
        {
            await WriteJson(res, 405, new { error = "Method not allowed" });
            return;
        }

        // ?refresh=true forces a fresh TWAIN scan immediately
        var forceRefresh = req.QueryString["refresh"] == "true";
        var scanners = GetScanners(forceRefresh);

        await WriteJson(res, 200, new { scanners });
    }

    private async Task HandleScan(HttpListenerRequest req, HttpListenerResponse res)
    {
        if (req.HttpMethod != "POST")
        {
            await WriteJson(res, 405, new { error = "Method not allowed" });
            return;
        }

        ScanRequest scanRequest;
        try
        {
            using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
            var body = await reader.ReadToEndAsync();
            scanRequest = string.IsNullOrWhiteSpace(body)
                ? new ScanRequest()
                : JsonSerializer.Deserialize<ScanRequest>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                  ?? new ScanRequest();
        }
        catch { scanRequest = new ScanRequest(); }

        var response = await _scannerService.ScanAsync(scanRequest);
        var statusCode = response.Success ? 200 : 500;
        await WriteJson(res, statusCode, response);
    }

    private async Task HandleUpload(HttpListenerRequest req, HttpListenerResponse res)
    {
        if (req.HttpMethod != "POST")
        {
            await WriteJson(res, 405, new { error = "Method not allowed" });
            return;
        }
        await ForwardToBackend(req, res, "api/ingest/scan-batch");
    }

    private async Task HandleConfirm(HttpListenerRequest req, HttpListenerResponse res)
    {
        if (req.HttpMethod != "POST")
        {
            await WriteJson(res, 405, new { error = "Method not allowed" });
            return;
        }
        await ForwardToBackend(req, res, "api/ingest/scan-confirm");
    }

    private async Task HandleRescan(HttpListenerRequest req, HttpListenerResponse res)
    {
        if (req.HttpMethod != "POST")
        {
            await WriteJson(res, 405, new { error = "Method not allowed" });
            return;
        }
        await ForwardToBackend(req, res, "api/ingest/scan-replace");
    }

    private async Task ForwardToBackend(
        HttpListenerRequest req,
        HttpListenerResponse res,
        string backendPath)
    {
        var settings = _settingsService.Load();

        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            await WriteJson(res, 401, new { error = "API key not configured. Open DoarioScan settings." });
            return;
        }

        if (string.IsNullOrWhiteSpace(settings.ApiBaseUrl))
        {
            await WriteJson(res, 400, new { error = "API URL not configured. Open DoarioScan settings." });
            return;
        }

        try
        {
            using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
            var body = await reader.ReadToEndAsync();

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
            client.DefaultRequestHeaders.Add("X-Api-Key", settings.ApiKey);

            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(
                $"{settings.ApiBaseUrl.TrimEnd('/')}/{backendPath}", content);

            var responseBody = await response.Content.ReadAsStringAsync();
            var bytes = Encoding.UTF8.GetBytes(responseBody);

            res.StatusCode = (int)response.StatusCode;
            res.ContentType = "application/json";
            res.ContentLength64 = bytes.Length;
            await res.OutputStream.WriteAsync(bytes);
        }
        catch (Exception ex)
        {
            await WriteJson(res, 500, new { error = ex.Message });
        }
    }

    private async Task WriteJson(HttpListenerResponse res, int statusCode, object data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });
        var bytes = Encoding.UTF8.GetBytes(json);

        res.StatusCode = statusCode;
        res.ContentType = "application/json";
        res.ContentLength64 = bytes.Length;

        await res.OutputStream.WriteAsync(bytes);
    }
}