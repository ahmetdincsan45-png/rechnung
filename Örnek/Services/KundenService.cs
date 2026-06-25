using System.Text.Json;
using System.IO;
using Örnek.Models;

namespace Örnek.Services;

public sealed class KundenService
{
    private static string AppFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Rechnung");

    private static string KundenPath => Path.Combine(AppFolder, "kunden.json");

    private static List<Kunde> CreateDefaultKunden() => new()
    {
        new Kunde
        {
            Adresse = new Adresse
            {
                Firmenname = "Beispiel AG",
                Strasse = "Kundenstrasse",
                Hausnummer = "456",
                Postleitzahl = "50667",
                Stadt = "Köln",
                Land = "Deutschland",
                Email = "info@beispiel-ag.de"
            }
        }
    };

    public List<Kunde> Load()
    {
        try
        {
            if (!File.Exists(KundenPath))
            {
                var seeded = CreateDefaultKunden();
                Save(seeded);
                return seeded;
            }

            var json = File.ReadAllText(KundenPath);
            return JsonSerializer.Deserialize<List<Kunde>>(json) ?? new List<Kunde>();
        }
        catch
        {
            return new List<Kunde>();
        }
    }

    public void Save(List<Kunde> kunden)
    {
        Directory.CreateDirectory(AppFolder);
        var json = JsonSerializer.Serialize(kunden, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(KundenPath, json);
    }
}
