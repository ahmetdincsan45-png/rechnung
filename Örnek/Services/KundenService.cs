using System.Text.Json;
using System.IO;
using Örnek.Models;

namespace Örnek.Services;

public sealed class KundenService
{
    private static string AppFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Rechnung");

    private static string LegacyAppFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Örnek");

    private static string KundenPath => Path.Combine(AppFolder, "kunden.json");
    private static string LegacyKundenPath => Path.Combine(LegacyAppFolder, "kunden.json");

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
            var currentKunden = LoadFromPath(KundenPath);
            var legacyKunden = LoadFromPath(LegacyKundenPath);

            if (currentKunden.Count == 0 && legacyKunden.Count == 0)
            {
                var seeded = CreateDefaultKunden();
                Save(seeded);
                return seeded;
            }

            var merged = MergeKunden(currentKunden, legacyKunden);

            if (merged.Count != currentKunden.Count)
                Save(merged);

            return merged;
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

    private static List<Kunde> LoadFromPath(string path)
    {
        if (!File.Exists(path))
            return new List<Kunde>();

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<Kunde>>(json) ?? new List<Kunde>();
    }

    private static List<Kunde> MergeKunden(IEnumerable<Kunde> currentKunden, IEnumerable<Kunde> legacyKunden)
    {
        return currentKunden
            .Concat(legacyKunden)
            .Where(k => !string.IsNullOrWhiteSpace(k.Adresse.Firmenname))
            .GroupBy(k => BuildIdentity(k), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(k => k.Adresse.Firmenname, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static string BuildIdentity(Kunde kunde)
    {
        if (kunde.Id != Guid.Empty)
            return kunde.Id.ToString("N");

        return string.Join("|",
            kunde.Adresse.Firmenname?.Trim() ?? string.Empty,
            kunde.Adresse.Email?.Trim() ?? string.Empty,
            kunde.Adresse.Stadt?.Trim() ?? string.Empty);
    }
}
