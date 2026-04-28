using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DoarioScan.Models;

namespace DoarioScan;

/// <summary>
/// Reads and writes BridgeSettings to disk.
/// ApiKey is encrypted using Windows DPAPI so it is never stored in plain text.
/// All other fields are stored as plain JSON.
/// </summary>
public class SettingsService
{
    private static readonly string SettingsFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DoarioScan");

    private static readonly string SettingsFile =
        Path.Combine(SettingsFolder, "settings.json");

    private static readonly string KeyFile =
        Path.Combine(SettingsFolder, "key.dat");

    // ── Public API ────────────────────────────────────────────────

    /// <summary>
    /// Loads settings from disk.
    /// Returns default settings if no file exists yet.
    /// </summary>
    public BridgeSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsFile))
                return new BridgeSettings();

            var json = File.ReadAllText(SettingsFile);
            var settings = JsonSerializer.Deserialize<BridgeSettings>(json)
                           ?? new BridgeSettings();

            // Decrypt API key if stored
            settings.ApiKey = LoadApiKey();

            return settings;
        }
        catch
        {
            return new BridgeSettings();
        }
    }

    /// <summary>
    /// Saves settings to disk.
    /// ApiKey is encrypted separately via DPAPI.
    /// </summary>
    public void Save(BridgeSettings settings)
    {
        EnsureFolder();

        // Encrypt and save API key separately
        SaveApiKey(settings.ApiKey);

        // Save everything else as plain JSON — ApiKey excluded
        var toSave = new BridgeSettings
        {
            ApiBaseUrl = settings.ApiBaseUrl,
            ApiKey = string.Empty, // never write raw key to JSON
            SelectedScanner = settings.SelectedScanner,
            Port = settings.Port,
            Dpi = settings.Dpi,
            ColorMode = settings.ColorMode,
        };

        var json = JsonSerializer.Serialize(toSave, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(SettingsFile, json);
    }

    /// <summary>
    /// Returns true if settings have been configured —
    /// API URL and key are both present.
    /// </summary>
    public bool IsConfigured(BridgeSettings settings)
    {
        return !string.IsNullOrWhiteSpace(settings.ApiBaseUrl)
            && !string.IsNullOrWhiteSpace(settings.ApiKey)
            && !string.IsNullOrWhiteSpace(settings.SelectedScanner);
    }

    // ── DPAPI encryption ──────────────────────────────────────────

    private void SaveApiKey(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            if (File.Exists(KeyFile))
                File.Delete(KeyFile);
            return;
        }

        EnsureFolder();

        var plainBytes = Encoding.UTF8.GetBytes(apiKey);
        var encryptedBytes = ProtectedData.Protect(
            plainBytes,
            null,
            DataProtectionScope.CurrentUser);

        File.WriteAllBytes(KeyFile, encryptedBytes);
    }

    private string LoadApiKey()
    {
        try
        {
            if (!File.Exists(KeyFile))
                return string.Empty;

            var encryptedBytes = File.ReadAllBytes(KeyFile);
            var plainBytes = ProtectedData.Unprotect(
                encryptedBytes,
                null,
                DataProtectionScope.CurrentUser);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {
            return string.Empty;
        }
    }

    private void EnsureFolder()
    {
        if (!Directory.Exists(SettingsFolder))
            Directory.CreateDirectory(SettingsFolder);
    }
}