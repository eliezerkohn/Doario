namespace DoarioScan;

/// <summary>
/// System tray icon and context menu.
/// Runs silently in the background — right click for options.
/// </summary>
public class TrayApp : ApplicationContext
{
    private readonly SettingsService _settingsService;
    private readonly ScannerService _scannerService;
    private readonly LocalApiServer _apiServer;
    private NotifyIcon _trayIcon;
    private SettingsWindow _settingsWindow;

    public TrayApp(
        SettingsService settingsService,
        ScannerService scannerService,
        LocalApiServer apiServer)
    {
        _settingsService = settingsService;
        _scannerService = scannerService;
        _apiServer = apiServer;

        InitializeTray();
        StartServer();
        CheckFirstRun();
    }

    // ── Tray setup ────────────────────────────────────────────────

    private void InitializeTray()
    {
        var contextMenu = new ContextMenuStrip();

        // Header — not clickable, just shows app name
        var header = new ToolStripLabel("DoarioScan")
        {
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 45, 74),
        };
        contextMenu.Items.Add(header);
        contextMenu.Items.Add(new ToolStripSeparator());

        // Status item — shows if Bridge is ready
        var statusItem = new ToolStripLabel(GetStatusText())
        {
            ForeColor = Color.FromArgb(100, 120, 140),
            Font = new Font("Segoe UI", 8.5f),
        };
        contextMenu.Items.Add(statusItem);
        contextMenu.Items.Add(new ToolStripSeparator());

        // Settings
        var settingsItem = new ToolStripMenuItem("⚙️  Settings");
        settingsItem.Click += (s, e) => OpenSettings();
        contextMenu.Items.Add(settingsItem);

        // Test connection
        var testItem = new ToolStripMenuItem("🔗  Test Connection");
        testItem.Click += async (s, e) => await TestConnectionAsync(statusItem);
        contextMenu.Items.Add(testItem);

        // View logs
        var logsItem = new ToolStripMenuItem("📋  View Logs");
        logsItem.Click += (s, e) => OpenLogs();
        contextMenu.Items.Add(logsItem);

        contextMenu.Items.Add(new ToolStripSeparator());

        // Exit
        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (s, e) => ExitApp();
        contextMenu.Items.Add(exitItem);

        // Refresh status when menu opens
        contextMenu.Opening += (s, e) =>
        {
            statusItem.Text = GetStatusText();
        };

        // Build tray icon
        _trayIcon = new NotifyIcon
        {
            Text = "DoarioScan Bridge",
            Icon = CreateTrayIcon(),
            ContextMenuStrip = contextMenu,
            Visible = true,
        };

        // Double click opens settings
        _trayIcon.DoubleClick += (s, e) => OpenSettings();
    }

    // ── Server ────────────────────────────────────────────────────

    private void StartServer()
    {
        try
        {
            _apiServer.Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to start DoarioScan Bridge server:\n\n{ex.Message}\n\nTry restarting DoarioScan.",
                "DoarioScan — Startup Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    // ── First run ─────────────────────────────────────────────────

    private void CheckFirstRun()
    {
        var settings = _settingsService.Load();
        if (!_settingsService.IsConfigured(settings))
        {
            // Show settings window on first run so admin can configure
            OpenSettings();
            _trayIcon.ShowBalloonTip(
                3000,
                "DoarioScan",
                "Welcome! Please enter your API key and select your scanner to get started.",
                ToolTipIcon.Info);
        }
        else
        {
            _trayIcon.ShowBalloonTip(
                2000,
                "DoarioScan",
                "Bridge is running. Your scanner is ready.",
                ToolTipIcon.None);
        }
    }

    // ── Actions ───────────────────────────────────────────────────

    private void OpenSettings()
    {
        if (_settingsWindow != null && !_settingsWindow.IsDisposed)
        {
            _settingsWindow.BringToFront();
            return;
        }

        _settingsWindow = new SettingsWindow(_settingsService, _scannerService);
        _settingsWindow.FormClosed += (s, e) => _settingsWindow = null;
        _settingsWindow.Show();
    }

    private async Task TestConnectionAsync(ToolStripLabel statusItem)
    {
        statusItem.Text = "⟳ Testing...";

        var settings = _settingsService.Load();

        if (!_settingsService.IsConfigured(settings))
        {
            _trayIcon.ShowBalloonTip(
                3000,
                "DoarioScan",
                "Not configured. Open Settings and enter your API key.",
                ToolTipIcon.Warning);
            statusItem.Text = GetStatusText();
            return;
        }

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            client.DefaultRequestHeaders.Add("X-Api-Key", settings.ApiKey);

            var response = await client.GetAsync($"{settings.ApiBaseUrl}/api/ingest/health");

            if (response.IsSuccessStatusCode)
            {
                _trayIcon.ShowBalloonTip(
                    3000,
                    "DoarioScan",
                    "✅ Connected to Doario successfully.",
                    ToolTipIcon.Info);
            }
            else
            {
                _trayIcon.ShowBalloonTip(
                    3000,
                    "DoarioScan",
                    $"❌ Server returned {(int)response.StatusCode}. Check your API URL.",
                    ToolTipIcon.Error);
            }
        }
        catch
        {
            _trayIcon.ShowBalloonTip(
                3000,
                "DoarioScan",
                "❌ Could not connect. Check your API URL and internet connection.",
                ToolTipIcon.Error);
        }
        finally
        {
            statusItem.Text = GetStatusText();
        }
    }

    private void OpenLogs()
    {
        var logFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DoarioScan");

        if (Directory.Exists(logFolder))
            System.Diagnostics.Process.Start("explorer.exe", logFolder);
        else
            MessageBox.Show(
                "No log folder found yet.",
                "DoarioScan",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
    }

    private void ExitApp()
    {
        _apiServer.Stop();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        Application.Exit();
    }

    // ── Helpers ───────────────────────────────────────────────────

    private string GetStatusText()
    {
        var settings = _settingsService.Load();
        if (!_settingsService.IsConfigured(settings))
            return "⚠️  Not configured";

        var scanners = _scannerService.GetAvailableScanners();
        var scannerReady = scanners.Contains(settings.SelectedScanner);

        return scannerReady
            ? $"✅  Ready — {settings.SelectedScanner}"
            : $"⚠️  Scanner not found — {settings.SelectedScanner}";
    }

    private Icon CreateTrayIcon()
    {
        // Create a simple tray icon programmatically
        // Replace with a real .ico file in production
        var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.FromArgb(13, 148, 136));
        g.FillEllipse(Brushes.White, 3, 3, 10, 10);
        return Icon.FromHandle(bmp.GetHicon());
    }
}