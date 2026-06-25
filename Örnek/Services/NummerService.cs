using System.Text.Json;
using Örnek.Models;
using System.IO;

namespace Örnek.Services;

public sealed class NummerService
{
    private static readonly string AppFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Rechnung");

    private static readonly string SettingsPath = Path.Combine(AppFolder, "nummer.json");

    public NummerSettings LoadOrDefault()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new NummerSettings();

            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<NummerSettings>(json) ?? new NummerSettings();
        }
        catch
        {
            return new NummerSettings();
        }
    }

    public void Save(NummerSettings settings)
    {
        Directory.CreateDirectory(AppFolder);
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }

    public static string Build(DokumentTyp typ, DateTime date, int seq)
    {
        var prefix = typ == DokumentTyp.Angebot ? "AN" : "RE";
        return $"{prefix}-{date:yyyyMMdd}-{seq}";
    }

    public string PreviewNext(DokumentTyp typ, DateTime date)
    {
        var settings = LoadOrDefault();
        var nextSeq = typ == DokumentTyp.Angebot ? settings.LastAngebotSeq + 1 : settings.LastRechnungSeq + 1;
        return Build(typ, date, nextSeq);
    }

    public static bool TryParseSeq(DokumentTyp typ, string? nummer, out int seq)
    {
        seq = 0;
        if (string.IsNullOrWhiteSpace(nummer))
            return false;

        var prefix = typ == DokumentTyp.Angebot ? "AN-" : "RE-";
        if (!nummer.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var parts = nummer.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 3)
            return false;

        return int.TryParse(parts[^1], out seq);
    }

    public string Next(DokumentTyp typ, DateTime date)
    {
        var settings = LoadOrDefault();
        var nextSeq = typ == DokumentTyp.Angebot ? settings.LastAngebotSeq + 1 : settings.LastRechnungSeq + 1;

        if (typ == DokumentTyp.Angebot)
            settings.LastAngebotSeq = nextSeq;
        else
            settings.LastRechnungSeq = nextSeq;

        Save(settings);
        return Build(typ, date, nextSeq);
    }

    public void EnsureAtLeast(DokumentTyp typ, int seq)
    {
        var settings = LoadOrDefault();
        var changed = false;

        if (typ == DokumentTyp.Angebot)
        {
            if (settings.LastAngebotSeq < seq)
            {
                settings.LastAngebotSeq = seq;
                changed = true;
            }
        }
        else
        {
            if (settings.LastRechnungSeq < seq)
            {
                settings.LastRechnungSeq = seq;
                changed = true;
            }
        }

        if (changed)
            Save(settings);
    }

    public void ForceBaseline(DokumentTyp typ, int lastSeq)
    {
        var settings = LoadOrDefault();
        var changed = false;

        if (typ == DokumentTyp.Angebot)
        {
            if (settings.LastAngebotSeq != lastSeq)
            {
                settings.LastAngebotSeq = lastSeq;
                changed = true;
            }
        }
        else
        {
            if (settings.LastRechnungSeq != lastSeq)
            {
                settings.LastRechnungSeq = lastSeq;
                changed = true;
            }
        }

        if (changed)
            Save(settings);
    }
}
