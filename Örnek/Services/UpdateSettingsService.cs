using System.Text.Json;
using System.IO;

namespace Örnek.Services;

public sealed class UpdateSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private static string AppFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Rechnung");

    private static string SettingsPath => Path.Combine(AppFolder, "update-settings.json");
    private static string BundledSettingsPath => Path.Combine(AppContext.BaseDirectory, "update-settings.json");

    public string? LoadManifestUrl()
    {
        try
        {
            var userSetting = LoadManifestUrlFrom(SettingsPath);
            if (!string.IsNullOrWhiteSpace(userSetting))
                return userSetting;

            return LoadManifestUrlFrom(BundledSettingsPath);
        }
        catch
        {
            return null;
        }
    }

    public void SaveManifestUrl(string manifestUrl)
    {
        Directory.CreateDirectory(AppFolder);
        var json = JsonSerializer.Serialize(new UpdateSettings { ManifestUrl = manifestUrl }, JsonOptions);
        File.WriteAllText(SettingsPath, json);
    }

    private static string? LoadManifestUrlFrom(string path)
    {
        if (!File.Exists(path))
            return null;

        var json = File.ReadAllText(path);
        var settings = JsonSerializer.Deserialize<UpdateSettings>(json, JsonOptions);
        return string.IsNullOrWhiteSpace(settings?.ManifestUrl) ? null : settings.ManifestUrl;
    }

    private sealed class UpdateSettings
    {
        public string ManifestUrl { get; set; } = string.Empty;
    }
}
