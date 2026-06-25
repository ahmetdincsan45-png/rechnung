using System.Text.Json;
using System.IO;
using Örnek.Models;

namespace Örnek.Services;

public sealed class EmailSettingsService
{
    private static string AppFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Rechnung");

    private static string SettingsPath => Path.Combine(AppFolder, "email-settings.json");

    public EmailSettings LoadOrDefault()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new EmailSettings();

            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<EmailSettings>(json) ?? new EmailSettings();
        }
        catch
        {
            return new EmailSettings();
        }
    }

    public void Save(EmailSettings settings)
    {
        Directory.CreateDirectory(AppFolder);
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }
}
