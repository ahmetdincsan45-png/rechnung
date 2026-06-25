using System.Text.Json;
using System.IO;
using Örnek.Models;

namespace Örnek.Services;

public sealed class ArtikelService
{
    private static string AppFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Rechnung");

    private static string ArtikelPath => Path.Combine(AppFolder, "artikel.json");

    private static List<Artikel> CreateDefaultArtikel() => new()
    {
        new Artikel
        {
            ArtikelNr = "A-1000",
            Bezeichnung = "Beratung",
            Einheit = "Std.",
            StandardPreis = 150m,
            StandardMwSt = 19m
        },
        new Artikel
        {
            ArtikelNr = "A-2000",
            Bezeichnung = "Montage",
            Einheit = "Std.",
            StandardPreis = 95m,
            StandardMwSt = 19m
        },
        new Artikel
        {
            ArtikelNr = "M-3000",
            Bezeichnung = "Materialpauschale",
            Einheit = "Pauschal",
            StandardPreis = 50m,
            StandardMwSt = 19m
        }
    };

    public List<Artikel> Load()
    {
        try
        {
            if (!File.Exists(ArtikelPath))
            {
                var seeded = CreateDefaultArtikel();
                Save(seeded);
                return seeded;
            }

            var json = File.ReadAllText(ArtikelPath);
            return JsonSerializer.Deserialize<List<Artikel>>(json) ?? new List<Artikel>();
        }
        catch
        {
            return new List<Artikel>();
        }
    }

    public void Save(List<Artikel> artikel)
    {
        EnsureArtikelNummern(artikel);
        Directory.CreateDirectory(AppFolder);
        var json = JsonSerializer.Serialize(artikel, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ArtikelPath, json);
    }

    public string SuggestNextArtikelNr(IEnumerable<Artikel> existing, string prefix = "A")
    {
        var used = new HashSet<string>(
            existing
                .Select(a => (a.ArtikelNr ?? string.Empty).Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x)),
            StringComparer.OrdinalIgnoreCase);

        var next = GetNextNumber(existing, prefix);
        while (true)
        {
            var candidate = $"{prefix}-{next:0000}";
            if (!used.Contains(candidate))
                return candidate;
            next++;
        }
    }

    private static void EnsureArtikelNummern(List<Artikel> artikel)
    {
        var used = new HashSet<string>(
            artikel
                .Select(a => (a.ArtikelNr ?? string.Empty).Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x)),
            StringComparer.OrdinalIgnoreCase);

        var next = GetNextNumber(artikel, prefix: "A");

        foreach (var a in artikel)
        {
            if (!string.IsNullOrWhiteSpace(a.ArtikelNr))
                continue;

            while (true)
            {
                var candidate = $"A-{next:0000}";
                next++;
                if (used.Add(candidate))
                {
                    a.ArtikelNr = candidate;
                    break;
                }
            }
        }
    }

    private static int GetNextNumber(IEnumerable<Artikel> artikel, string prefix)
    {
        var max = 0;
        foreach (var a in artikel)
        {
            var nr = (a.ArtikelNr ?? string.Empty).Trim();
            if (!nr.StartsWith(prefix + "-", StringComparison.OrdinalIgnoreCase))
                continue;

            var rest = nr[(prefix.Length + 1)..];
            if (int.TryParse(rest, out var n))
                max = Math.Max(max, n);
        }

        return max + 1;
    }
}