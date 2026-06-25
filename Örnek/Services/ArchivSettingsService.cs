using System.Text.Json;
using System.IO;
using Örnek.Models;

namespace Örnek.Services;

public sealed class ArchivSettingsService
{
    private static string AppFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Rechnung");

    private static string SettingsPath => Path.Combine(AppFolder, "archiv-settings.json");

    public ArchivSettings LoadOrDefault()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new ArchivSettings();

            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<ArchivSettings>(json) ?? new ArchivSettings();
        }
        catch
        {
            return new ArchivSettings();
        }
    }

    public void Save(ArchivSettings settings)
    {
        Directory.CreateDirectory(AppFolder);
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }
}
