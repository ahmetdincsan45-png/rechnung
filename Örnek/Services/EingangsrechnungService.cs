using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Örnek.Models;

namespace Örnek.Services;

public sealed class EingangsrechnungService
{
    private readonly ArchivSettingsService _archivSettingsService;

    private sealed class LoadedEntry
    {
        public required EingangsrechnungEintrag Entry { get; init; }
        public required string JsonPath { get; init; }
    }

    public EingangsrechnungService(ArchivSettingsService archivSettingsService)
    {
        _archivSettingsService = archivSettingsService;
    }

    public string GetRootFolder()
    {
        var configured = _archivSettingsService.LoadOrDefault().ArchivOrdner;
        if (!string.IsNullOrWhiteSpace(configured))
            return Path.Combine(configured, "Eingangsrechnungen");

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Rechnung",
            "Archiv",
            "Eingangsrechnungen");
    }

    public List<EingangsrechnungEintrag> LoadAll()
    {
        var root = GetRootFolder();
        if (!Directory.Exists(root))
            return new List<EingangsrechnungEintrag>();

        var loadedEntries = new List<LoadedEntry>();

        foreach (var jsonPath in Directory.EnumerateFiles(root, "*.json", SearchOption.AllDirectories))
        {
            try
            {
                var json = File.ReadAllText(jsonPath);
                var item = JsonSerializer.Deserialize<EingangsrechnungEintrag>(json);
                if (item == null)
                    continue;

                if (string.IsNullOrWhiteSpace(item.DokumentPfad))
                {
                    var folder = Path.GetDirectoryName(jsonPath);
                    if (!string.IsNullOrWhiteSpace(folder))
                    {
                        var doc = Directory.EnumerateFiles(folder)
                            .FirstOrDefault(f => !string.Equals(Path.GetExtension(f), ".json", StringComparison.OrdinalIgnoreCase));
                        item.DokumentPfad = doc ?? string.Empty;
                    }
                }

                loadedEntries.Add(new LoadedEntry
                {
                    Entry = item,
                    JsonPath = jsonPath
                });
            }
            catch
            {
                // ignore broken entries
            }
        }

        return loadedEntries
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Entry.Id) ? x.JsonPath : x.Entry.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderByDescending(x => File.GetLastWriteTimeUtc(x.JsonPath))
                .ThenByDescending(x => x.Entry.Rechnungsdatum)
                .ThenByDescending(x => x.Entry.ErfasstAm)
                .First()
                .Entry)
            .OrderByDescending(x => x.Rechnungsdatum)
            .ThenByDescending(x => x.ErfasstAm)
            .ToList();
    }

    public List<EingangsrechnungEintrag> LoadByAssignedInvoice(string rechnungsnummer)
    {
        if (string.IsNullOrWhiteSpace(rechnungsnummer))
            return new List<EingangsrechnungEintrag>();

        return LoadAll()
            .Where(x => string.Equals(x.ZugeordneteRechnungNummer, rechnungsnummer, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.Rechnungsdatum)
            .ThenByDescending(x => x.ErfasstAm)
            .ToList();
    }

    public void AssignToInvoice(EingangsrechnungEintrag entry, string? rechnungsnummer)
    {
        entry.ZugeordneteRechnungNummer = rechnungsnummer?.Trim() ?? string.Empty;
        Save(entry);
    }

    public EingangsrechnungEintrag ImportDocument(string sourceFile)
    {
        var originalFileName = Path.GetFileName(sourceFile);
        return ImportDocument(sourceFile, originalFileName);
    }

    public EingangsrechnungEintrag ImportDocument(string sourceFile, string? originalFileName)
    {
        var root = GetRootFolder();
        Directory.CreateDirectory(root);

        var now = DateTime.Now;
        var yearFolder = Path.Combine(root, now.Year.ToString());
        Directory.CreateDirectory(yearFolder);

        var id = Guid.NewGuid().ToString("N");
        var entryFolder = Path.Combine(yearFolder, id);
        Directory.CreateDirectory(entryFolder);

        var ext = Path.GetExtension(sourceFile);
        var fileName = SanitizeFileName(Path.GetFileNameWithoutExtension(sourceFile));
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = "beleg";

        var destFile = Path.Combine(entryFolder, fileName + ext);
        File.Copy(sourceFile, destFile, true);

        var entry = new EingangsrechnungEintrag
        {
            Id = id,
            DokumentPfad = destFile,
            OriginalDateiname = string.IsNullOrWhiteSpace(originalFileName) ? Path.GetFileName(sourceFile) : originalFileName,
            Rechnungsdatum = now.Date,
            ErfasstAm = now
        };

        Save(entry);
        return entry;
    }

    public void Save(EingangsrechnungEintrag entry)
    {
        var folder = EnsureEntryFolder(entry);
        var jsonPath = Path.Combine(folder, "meta.json");
        var json = JsonSerializer.Serialize(entry, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(jsonPath, json);
        DeleteDuplicateEntryFolders(entry, folder);
    }

    public bool Delete(EingangsrechnungEintrag entry)
    {
        try
        {
            var deletedAnyFolder = false;

            foreach (var folder in GetEntryFolders(entry))
            {
                if (!Directory.Exists(folder))
                    continue;

                Directory.Delete(folder, true);
                deletedAnyFolder = true;
            }

            if (deletedAnyFolder)
                return true;

            if (!string.IsNullOrWhiteSpace(entry.DokumentPfad) && File.Exists(entry.DokumentPfad))
                File.Delete(entry.DokumentPfad);
        }
        catch
        {
            return false;
        }

        return true;
    }

    public void OpenDocument(EingangsrechnungEintrag entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.DokumentPfad) && File.Exists(entry.DokumentPfad))
            Process.Start(new ProcessStartInfo(entry.DokumentPfad) { UseShellExecute = true });
    }

    public void OpenFolder(EingangsrechnungEintrag entry)
    {
        var folder = GetEntryFolder(entry);
        if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder))
            Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
    }

    private string EnsureEntryFolder(EingangsrechnungEintrag entry)
    {
        if (string.IsNullOrWhiteSpace(entry.Id))
            entry.Id = Guid.NewGuid().ToString("N");

        var year = entry.Rechnungsdatum.Year <= 1 ? DateTime.Now.Year : entry.Rechnungsdatum.Year;
        var root = GetRootFolder();
        var folder = Path.Combine(root, year.ToString(), entry.Id);

        var currentFolder = GetEntryFolders(entry)
            .FirstOrDefault(existingFolder => Directory.Exists(existingFolder));

        if (!string.IsNullOrWhiteSpace(currentFolder) &&
            !string.Equals(currentFolder, folder, StringComparison.OrdinalIgnoreCase))
        {
            MoveEntryFolder(currentFolder, folder, entry);
            return folder;
        }

        Directory.CreateDirectory(folder);

        if (!string.IsNullOrWhiteSpace(entry.DokumentPfad))
        {
            var fileName = Path.GetFileName(entry.DokumentPfad);
            if (!string.IsNullOrWhiteSpace(fileName))
                entry.DokumentPfad = Path.Combine(folder, fileName);
        }

        return folder;
    }

    private string GetEntryFolder(EingangsrechnungEintrag entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.DokumentPfad))
        {
            var byDoc = Path.GetDirectoryName(entry.DokumentPfad);
            if (!string.IsNullOrWhiteSpace(byDoc))
                return byDoc;
        }

        var year = entry.Rechnungsdatum.Year <= 1 ? DateTime.Now.Year : entry.Rechnungsdatum.Year;
        return Path.Combine(GetRootFolder(), year.ToString(), entry.Id ?? string.Empty);
    }

    private IEnumerable<string> GetEntryFolders(EingangsrechnungEintrag entry)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var directFolder = GetEntryFolder(entry);
        if (!string.IsNullOrWhiteSpace(directFolder) && seen.Add(directFolder))
            yield return directFolder;

        if (string.IsNullOrWhiteSpace(entry.Id))
            yield break;

        var root = GetRootFolder();
        if (!Directory.Exists(root))
            yield break;

        foreach (var folder in Directory.EnumerateDirectories(root, entry.Id, SearchOption.AllDirectories))
        {
            if (seen.Add(folder))
                yield return folder;
        }
    }

    private static void MoveEntryFolder(string sourceFolder, string targetFolder, EingangsrechnungEintrag entry)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(targetFolder)!);

        if (Directory.Exists(targetFolder))
            Directory.Delete(targetFolder, true);

        Directory.Move(sourceFolder, targetFolder);

        if (!string.IsNullOrWhiteSpace(entry.DokumentPfad))
        {
            var fileName = Path.GetFileName(entry.DokumentPfad);
            if (!string.IsNullOrWhiteSpace(fileName))
                entry.DokumentPfad = Path.Combine(targetFolder, fileName);
        }
    }

    private void DeleteDuplicateEntryFolders(EingangsrechnungEintrag entry, string canonicalFolder)
    {
        foreach (var folder in GetEntryFolders(entry))
        {
            if (!Directory.Exists(folder) || string.Equals(folder, canonicalFolder, StringComparison.OrdinalIgnoreCase))
                continue;

            Directory.Delete(folder, true);
        }
    }

    private static string SanitizeFileName(string name) =>
        string.Join("_", name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();
}
