namespace DoarioScan;

/// <summary>
/// DoarioScan Bridge — entry point.
/// Starts the system tray app and local API server.
/// Runs silently on Windows startup — no window shown.
/// </summary>
internal static class Program
{
    [STAThread]
    static void Main()
    {
        // Required for Windows Forms + modern visual styles
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        ApplicationConfiguration.Initialize();

        // Prevent multiple instances running at the same time
        const string mutexName = "DoarioScan_SingleInstance";
        using var mutex = new Mutex(true, mutexName, out var isNewInstance);

        if (!isNewInstance)
        {
            MessageBox.Show(
                "DoarioScan is already running.\n\nCheck the system tray (bottom right of your screen).",
                "DoarioScan",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        // Wire up services
        var settingsService = new SettingsService();
        var scannerService = new ScannerService(settingsService);
        var apiServer = new LocalApiServer(scannerService, settingsService);

        // Handle unhandled exceptions — log and show message
        Application.ThreadException += (s, e) =>
        {
            LogError(e.Exception);
            MessageBox.Show(
                $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nDoarioScan will continue running.",
                "DoarioScan — Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        };

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                LogError(ex);
        };

        // Start the tray app — this blocks until Exit is clicked
        Application.Run(new TrayApp(settingsService, scannerService, apiServer));
    }

    // ── Error logging ─────────────────────────────────────────────

    private static void LogError(Exception ex)
    {
        try
        {
            var logFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DoarioScan");

            Directory.CreateDirectory(logFolder);

            var logFile = Path.Combine(logFolder, "error.log");
            var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n\n";

            File.AppendAllText(logFile, entry);
        }
        catch
        {
            // If logging fails, silently ignore — don't crash the app
        }
    }
}