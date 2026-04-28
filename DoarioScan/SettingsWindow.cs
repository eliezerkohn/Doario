using DoarioScan.Models;

namespace DoarioScan;

/// <summary>
/// Settings window — opened from the system tray right-click menu.
/// Admin enters API URL, API key, and selects their scanner.
/// </summary>
public class SettingsWindow : Form
{
    private readonly SettingsService _settingsService;
    private readonly ScannerService _scannerService;

    // ── Controls ──────────────────────────────────────────────────
    private Label _lblTitle;
    private Label _lblApiUrl;
    private TextBox _txtApiUrl;
    private Label _lblApiKey;
    private TextBox _txtApiKey;
    private Label _lblScanner;
    private ComboBox _cboScanner;
    private Button _btnRefreshScanners;
    private Label _lblDpi;
    private ComboBox _cboDpi;
    private Label _lblColorMode;
    private ComboBox _cboColorMode;
    private Panel _statusPanel;
    private Label _lblStatus;
    private Button _btnTest;
    private Button _btnSave;
    private Button _btnCancel;

    public SettingsWindow(SettingsService settingsService, ScannerService scannerService)
    {
        _settingsService = settingsService;
        _scannerService = scannerService;

        InitializeForm();
        LoadSettings();
        LoadScanners();
    }

    // ── Form setup ────────────────────────────────────────────────

    private void InitializeForm()
    {
        Text = "DoarioScan — Settings";
        Size = new Size(460, 520);
        MinimumSize = new Size(460, 520);
        MaximumSize = new Size(460, 520);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(247, 249, 251);
        Font = new Font("Segoe UI", 9f);

        // ── Title ──
        _lblTitle = new Label
        {
            Text = "🖨  DoarioScan Settings",
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 45, 74),
            Location = new Point(20, 20),
            Size = new Size(420, 30),
        };

        // ── API URL ──
        _lblApiUrl = MakeLabel("Doario API URL", new Point(20, 68));
        _txtApiUrl = MakeTextBox(new Point(20, 88), 400);
        _txtApiUrl.PlaceholderText = "https://app.doario.com";

        // ── API Key ──
        _lblApiKey = MakeLabel("Tenant API Key", new Point(20, 128));
        _txtApiKey = MakeTextBox(new Point(20, 148), 400);
        _txtApiKey.PasswordChar = '•';
        _txtApiKey.PlaceholderText = "doa_live_...";

        // ── Scanner ──
        _lblScanner = MakeLabel("Scanner", new Point(20, 188));

        _cboScanner = new ComboBox
        {
            Location = new Point(20, 208),
            Size = new Size(306, 28),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat,
        };

        _btnRefreshScanners = new Button
        {
            Text = "⟳ Refresh",
            Location = new Point(334, 207),
            Size = new Size(86, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(13, 148, 136),
            Cursor = Cursors.Hand,
        };
        _btnRefreshScanners.FlatAppearance.BorderColor = Color.FromArgb(13, 148, 136);
        _btnRefreshScanners.Click += (s, e) => LoadScanners();

        // ── DPI ──
        _lblDpi = MakeLabel("Scan Resolution", new Point(20, 248));
        _cboDpi = new ComboBox
        {
            Location = new Point(20, 268),
            Size = new Size(190, 28),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat,
        };
        _cboDpi.Items.AddRange(new object[] { "150 DPI", "200 DPI", "300 DPI", "400 DPI", "600 DPI" });
        _cboDpi.SelectedIndex = 2; // 300 DPI default

        // ── Color Mode ──
        _lblColorMode = MakeLabel("Color Mode", new Point(230, 248));
        _cboColorMode = new ComboBox
        {
            Location = new Point(230, 268),
            Size = new Size(190, 28),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat,
        };
        _cboColorMode.Items.AddRange(new object[] { "Grayscale", "Color", "Black & White" });
        _cboColorMode.SelectedIndex = 0; // Grayscale default

        // ── Status panel ──
        _statusPanel = new Panel
        {
            Location = new Point(20, 316),
            Size = new Size(400, 60),
            BackColor = Color.FromArgb(240, 244, 248),
            Visible = false,
        };
        _statusPanel.Paint += (s, e) =>
        {
            var rect = new Rectangle(0, 0, _statusPanel.Width - 1, _statusPanel.Height - 1);
            e.Graphics.DrawRectangle(new Pen(Color.FromArgb(210, 220, 230)), rect);
        };

        _lblStatus = new Label
        {
            Location = new Point(10, 10),
            Size = new Size(380, 40),
            ForeColor = Color.FromArgb(15, 45, 74),
            Font = new Font("Segoe UI", 9f),
        };
        _statusPanel.Controls.Add(_lblStatus);

        // ── Buttons ──
        _btnTest = new Button
        {
            Text = "Test Connection",
            Location = new Point(20, 400),
            Size = new Size(130, 36),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(13, 148, 136),
            Cursor = Cursors.Hand,
        };
        _btnTest.FlatAppearance.BorderColor = Color.FromArgb(13, 148, 136);
        _btnTest.Click += async (s, e) => await TestConnectionAsync();

        _btnSave = new Button
        {
            Text = "Save",
            Location = new Point(264, 400),
            Size = new Size(74, 36),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(13, 148, 136),
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
        };
        _btnSave.FlatAppearance.BorderSize = 0;
        _btnSave.Click += (s, e) => SaveSettings();

        _btnCancel = new Button
        {
            Text = "Cancel",
            Location = new Point(346, 400),
            Size = new Size(74, 36),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(74, 100, 120),
            Cursor = Cursors.Hand,
        };
        _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(210, 220, 230);
        _btnCancel.Click += (s, e) => Close();

        // ── Add controls ──
        Controls.AddRange(new Control[]
        {
            _lblTitle,
            _lblApiUrl, _txtApiUrl,
            _lblApiKey, _txtApiKey,
            _lblScanner, _cboScanner, _btnRefreshScanners,
            _lblDpi, _cboDpi,
            _lblColorMode, _cboColorMode,
            _statusPanel,
            _btnTest, _btnSave, _btnCancel,
        });
    }

    // ── Load / Save ───────────────────────────────────────────────

    private void LoadSettings()
    {
        var settings = _settingsService.Load();

        _txtApiUrl.Text = settings.ApiBaseUrl;
        _txtApiKey.Text = settings.ApiKey;

        // DPI
        var dpiMap = new Dictionary<int, int> { { 150, 0 }, { 200, 1 }, { 300, 2 }, { 400, 3 }, { 600, 4 } };
        _cboDpi.SelectedIndex = dpiMap.TryGetValue(settings.Dpi, out var dpiIdx) ? dpiIdx : 2;

        // Color mode
        var colorMap = new Dictionary<string, int>
        {
            { "grayscale", 0 }, { "color", 1 }, { "blackwhite", 2 }
        };
        var colorKey = settings.ColorMode?.ToLower().Replace(" ", "") ?? "grayscale";
        _cboColorMode.SelectedIndex = colorMap.TryGetValue(colorKey, out var colorIdx) ? colorIdx : 0;
    }

    private void LoadScanners()
    {
        var settings = _settingsService.Load();
        var currentScanner = settings.SelectedScanner;

        _cboScanner.Items.Clear();
        _cboScanner.Items.Add("— Select scanner —");

        var scanners = _scannerService.GetAvailableScanners();
        foreach (var s in scanners)
            _cboScanner.Items.Add(s);

        // Reselect previously chosen scanner
        if (!string.IsNullOrWhiteSpace(currentScanner))
        {
            var idx = _cboScanner.Items.IndexOf(currentScanner);
            _cboScanner.SelectedIndex = idx >= 0 ? idx : 0;
        }
        else
        {
            _cboScanner.SelectedIndex = 0;
        }

        if (scanners.Count == 0)
            ShowStatus("⚠️ No scanners found. Check your scanner is connected and turned on.", false);
    }

    private void SaveSettings()
    {
        if (string.IsNullOrWhiteSpace(_txtApiUrl.Text))
        {
            ShowStatus("❌ Please enter the Doario API URL.", false);
            return;
        }

        if (string.IsNullOrWhiteSpace(_txtApiKey.Text))
        {
            ShowStatus("❌ Please enter your Tenant API Key.", false);
            return;
        }

        if (_cboScanner.SelectedIndex <= 0)
        {
            ShowStatus("❌ Please select a scanner.", false);
            return;
        }

        var dpiValues = new[] { 150, 200, 300, 400, 600 };
        var colorValues = new[] { "grayscale", "color", "blackwhite" };

        var settings = new BridgeSettings
        {
            ApiBaseUrl = _txtApiUrl.Text.Trim().TrimEnd('/'),
            ApiKey = _txtApiKey.Text.Trim(),
            SelectedScanner = _cboScanner.SelectedItem?.ToString() ?? string.Empty,
            Dpi = dpiValues[_cboDpi.SelectedIndex],
            ColorMode = colorValues[_cboColorMode.SelectedIndex],
            Port = 5100,
        };

        _settingsService.Save(settings);
        ShowStatus("✅ Settings saved successfully.", true);

        // Close after short delay so admin sees the success message
        Task.Delay(1200).ContinueWith(_ => Invoke(Close));
    }

    // ── Test connection ───────────────────────────────────────────

    private async Task TestConnectionAsync()
    {
        ShowStatus("⟳ Testing connection...", true);
        _btnTest.Enabled = false;

        try
        {
            var url = _txtApiUrl.Text.Trim().TrimEnd('/');
            var apiKey = _txtApiKey.Text.Trim();

            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(apiKey))
            {
                ShowStatus("❌ Enter API URL and key before testing.", false);
                return;
            }

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

            var response = await client.GetAsync($"{url}/api/ingest/health");

            if (response.IsSuccessStatusCode)
                ShowStatus("✅ Connected to Doario successfully.", true);
            else
                ShowStatus($"❌ Server returned {(int)response.StatusCode}. Check your API URL.", false);
        }
        catch (TaskCanceledException)
        {
            ShowStatus("❌ Connection timed out. Check your API URL.", false);
        }
        catch (Exception ex)
        {
            ShowStatus($"❌ {ex.Message}", false);
        }
        finally
        {
            _btnTest.Enabled = true;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────

    private void ShowStatus(string message, bool success)
    {
        _lblStatus.Text = message;
        _lblStatus.ForeColor = success
            ? Color.FromArgb(13, 148, 136)
            : Color.FromArgb(220, 53, 69);
        _statusPanel.Visible = true;
    }

    private Label MakeLabel(string text, Point location)
    {
        return new Label
        {
            Text = text,
            Location = location,
            Size = new Size(400, 18),
            ForeColor = Color.FromArgb(74, 100, 120),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
        };
    }

    private TextBox MakeTextBox(Point location, int width)
    {
        return new TextBox
        {
            Location = location,
            Size = new Size(width, 28),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White,
            Font = new Font("Segoe UI", 9.5f),
        };
    }
}