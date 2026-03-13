// Services/AppSettings.cs
using System.Text.Json;

namespace WarcraftPulseUploader.Services;

public sealed class AppSettings
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WarcraftPulseUploader",
        "settings.json"
    );

    // Stored encrypted on disk; decrypted in memory via Windows DPAPI.
    // Anyone who can read settings.json cannot use the token without the same Windows user account.
    [System.Text.Json.Serialization.JsonIgnore]
    public string ApiToken { get; set; } = string.Empty;

    // Serialized field — encrypted bytes as Base64
    public string ApiTokenEncrypted
    {
        get => ApiToken.Length == 0 ? "" :
            Convert.ToBase64String(
                System.Security.Cryptography.ProtectedData.Protect(
                    System.Text.Encoding.UTF8.GetBytes(ApiToken),
                    null,
                    System.Security.Cryptography.DataProtectionScope.CurrentUser));
        set
        {
            if (string.IsNullOrEmpty(value)) { ApiToken = ""; return; }
            try
            {
                ApiToken = System.Text.Encoding.UTF8.GetString(
                    System.Security.Cryptography.ProtectedData.Unprotect(
                        Convert.FromBase64String(value),
                        null,
                        System.Security.Cryptography.DataProtectionScope.CurrentUser));
            }
            catch { ApiToken = ""; }  // encrypted by different user or corrupted — reset
        }
    }

    public string ServerUrl { get; set; } = "https://warcraftpulse.com";
    public string WowLogDirectory { get; set; } = string.Empty;
    public bool AutoUpload { get; set; } = true;
    public bool MinimizeToTray { get; set; } = true;
    public bool StartWithWindows { get; set; } = true;

    public static AppSettings Load()
    {
        AppSettings settings;

        if (!File.Exists(SettingsPath))
        {
            settings = new AppSettings();
        }
        else
        {
            try
            {
                var json = File.ReadAllText(SettingsPath);
                settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                settings = new AppSettings();
            }
        }

        // Auto-detect WoW log directory on first run — not persisted until the user saves settings
        if (string.IsNullOrEmpty(settings.WowLogDirectory))
            settings.WowLogDirectory = WowDirectoryDetector.Detect() ?? string.Empty;

        return settings;
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}
