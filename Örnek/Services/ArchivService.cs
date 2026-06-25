using System.Text.Json;
using System.IO;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using Örnek.Models;

namespace Örnek.Services;

public sealed class ArchivService
{
    private readonly ArchivSettingsService _settingsService;
    private readonly EingangsrechnungService _eingangsrechnungService;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly string[] IgnoredDirectoryNames = [".vs", "bin", "obj", ".git", ".github", "node_modules"];

    public ArchivService(ArchivSettingsService settingsService)
    {
        _settingsService = settingsService;
        _eingangsrechnungService = new EingangsrechnungService(settingsService);
    }

    public string? GetArchivOrdner() => _settingsService.LoadOrDefault().ArchivOrdner;

    public bool IsConfigured()
    {
        var root = GetArchivOrdner();
        return !string.IsNullOrWhiteSpace(root) && Directory.Exists(root);
    }

    public ArchivEintrag? Archivieren(Rechnung rechnung, byte[] pdfBytes)
    {
        var root = GetArchivOrdner();
        if (string.IsNullOrWhiteSpace(root))
            return null;

        Directory.CreateDirectory(root);

        var (pdfPath, jsonPath) = ResolveArchivePaths(rechnung, root);
        return WriteArchiveFiles(rechnung, pdfBytes, pdfPath, jsonPath);
    }

    public ArchivEintrag? ArchivEintragAktualisieren(Rechnung rechnung, byte[] pdfBytes)
    {
        var root = GetArchivOrdner();
        if (string.IsNullOrWhiteSpace(root))
            return null;

        var (pdfPath, jsonPath) = ResolveArchivePaths(rechnung, root);
        return WriteArchiveFiles(rechnung, pdfBytes, pdfPath, jsonPath);
    }

    public List<ArchivEintrag> ListeLaden()
    {
        var result = new List<ArchivEintrag>();
        var seenJsonPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var archiveRoot = GetArchivOrdner();
        if (!string.IsNullOrWhiteSpace(archiveRoot) && Directory.Exists(archiveRoot))
        {
            foreach (var jsonPath in EnumerateArchiveJsonFiles(archiveRoot))
            {
                if (!seenJsonPaths.Add(jsonPath))
                    continue;

                if (TryLoadEntryFromArchiveJson(archiveRoot, jsonPath, out var entry))
                {
                    entry.AngehaengteBelegAnzahl = GetAttachedReceiptCount(entry.DokumentNummer);
                    result.Add(entry);
                }
            }
        }

        AddLoosePdfEntries(result, seenJsonPaths, archiveRoot);

        return result
            .GroupBy(GetDocumentIdentity, StringComparer.OrdinalIgnoreCase)
            .Select(SelectPreferredEntry)
            .OrderByDescending(x => x.Datum)
            .ThenByDescending(x => x.DokumentNummer)
            .ToList();
    }

    public List<ArchivEintrag> Suche(ArchivFilter filter)
    {
        var items = ListeLaden().AsEnumerable();

        if (filter.DokumentTyp.HasValue)
            items = items.Where(x => x.DokumentTyp == filter.DokumentTyp.Value);

        if (filter.Status.HasValue)
            items = items.Where(x => x.Status == filter.Status.Value);

        if (filter.VonDatum.HasValue)
            items = items.Where(x => x.Datum.Date >= filter.VonDatum.Value.Date);

        if (filter.BisDatum.HasValue)
            items = items.Where(x => x.Datum.Date <= filter.BisDatum.Value.Date);

        if (!string.IsNullOrWhiteSpace(filter.Suchtext))
        {
            var search = filter.Suchtext.Trim();
            items = items.Where(x =>
                x.DokumentNummer.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                x.Kunde.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                x.LetzteAktionText.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        return items.ToList();
    }

    public Rechnung? LoadRechnung(ArchivEintrag eintrag)
    {
        if (!string.IsNullOrWhiteSpace(eintrag.JsonPath) && File.Exists(eintrag.JsonPath))
        {
            var json = File.ReadAllText(eintrag.JsonPath);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("rechnung", out var rechnungElement))
                return rechnungElement.Deserialize<Rechnung>();
        }

        var recovered = TryBuildRechnungFromPdf(eintrag);
        if (recovered != null)
            PersistRecoveredRechnung(eintrag, recovered);

        return recovered;
    }

    public bool UpdateStatus(ArchivEintrag eintrag, DokumentStatus status, string? actionText = null)
    {
        var jsonPath = ResolveJsonPath(eintrag);
        if (string.IsNullOrWhiteSpace(jsonPath))
            return false;

        var rechnung = LoadRechnung(new ArchivEintrag
        {
            JsonPath = jsonPath
        }) ?? CreateFallbackRechnung(eintrag);

        var actionTime = DateTime.Now;

        eintrag.Status = status;
        eintrag.LetzteAktionAm = actionTime;
        eintrag.LetzteAktionText = actionText ?? string.Empty;
        eintrag.JsonPath = jsonPath;

        rechnung.Status = status;
        rechnung.LetzteAktionAm = actionTime;
        rechnung.LetzteAktionText = eintrag.LetzteAktionText;

        var json = JsonSerializer.Serialize(new { eintrag, rechnung }, JsonOptions);
        File.WriteAllText(jsonPath, json);
        return true;
    }

    private static (string PdfPath, string JsonPath) ResolveArchivePaths(Rechnung rechnung, string root)
    {
        if (!string.IsNullOrWhiteSpace(rechnung.ArchivPdfPath) && !string.IsNullOrWhiteSpace(rechnung.ArchivJsonPath))
            return (rechnung.ArchivPdfPath, rechnung.ArchivJsonPath);

        var kunde = SanitizeFolder(rechnung.Empfänger?.Firmenname ?? "Unbekannt");
        if (string.IsNullOrWhiteSpace(kunde))
            kunde = "Unbekannt";

        var year = rechnung.Rechnungsdatum.Year.ToString();
        var typ = rechnung.DokumentTyp.ToString();
        var docFolder = Path.Combine(root, typ, year, kunde);
        var fileBase = SanitizeFileName($"{typ}_{rechnung.Rechnungsnummer}");
        if (string.IsNullOrWhiteSpace(fileBase))
            fileBase = $"{typ}_{DateTime.Now:yyyyMMdd_HHmmss}";

        return (Path.Combine(docFolder, fileBase + ".pdf"), Path.Combine(docFolder, fileBase + ".json"));
    }

    private ArchivEintrag WriteArchiveFiles(Rechnung rechnung, byte[] pdfBytes, string pdfPath, string jsonPath)
    {
        var folder = Path.GetDirectoryName(pdfPath);
        if (!string.IsNullOrWhiteSpace(folder))
            Directory.CreateDirectory(folder);

        File.WriteAllBytes(pdfPath, pdfBytes);

        var eintrag = new ArchivEintrag
        {
            DokumentTyp = rechnung.DokumentTyp,
            DokumentNummer = rechnung.Rechnungsnummer,
            AngehaengteBelegAnzahl = CountAttachedReceipts(rechnung.Rechnungsnummer),
            Status = rechnung.Status,
            Datum = rechnung.Rechnungsdatum,
            LetzteAktionAm = rechnung.LetzteAktionAm,
            LetzteAktionText = rechnung.LetzteAktionText ?? string.Empty,
            Kunde = rechnung.Empfänger?.Firmenname ?? string.Empty,
            PdfPath = pdfPath,
            JsonPath = jsonPath
        };

        var json = JsonSerializer.Serialize(new { eintrag, rechnung }, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(jsonPath, json);

        rechnung.ArchivPdfPath = pdfPath;
        rechnung.ArchivJsonPath = jsonPath;

        return eintrag;
    }

    private IEnumerable<string> GetSearchRoots()
    {
        var settings = _settingsService.LoadOrDefault();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in new[] { settings.ArchivOrdner, settings.DefaultSaveOrdner })
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                continue;

            var fullPath = Path.GetFullPath(path);
            if (seen.Add(fullPath))
                yield return fullPath;
        }
    }

    private static bool TryLoadEntryFromArchiveJson(string root, string jsonPath, out ArchivEintrag entry)
    {
        entry = null!;

        try
        {
            var json = File.ReadAllText(jsonPath);
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("eintrag", out var archiveEntryElement))
                return false;

            var loadedEntry = archiveEntryElement.Deserialize<ArchivEintrag>();
            if (loadedEntry == null)
                return false;

            loadedEntry.JsonPath = jsonPath;
            if (string.IsNullOrWhiteSpace(loadedEntry.PdfPath))
            {
                var pdfCandidate = Path.ChangeExtension(jsonPath, ".pdf");
                if (File.Exists(pdfCandidate))
                    loadedEntry.PdfPath = pdfCandidate;
            }

            if (string.IsNullOrWhiteSpace(loadedEntry.Kunde))
            {
                loadedEntry.Kunde = TryReadCustomerFromRechnung(doc.RootElement) ??
                                    InferCustomerFromPath(root, loadedEntry.PdfPath, jsonPath);
            }

            entry = loadedEntry;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void AddLoosePdfEntries(List<ArchivEintrag> result, HashSet<string> seenJsonPaths, string? archiveRoot)
    {
        var seenPdfPaths = new HashSet<string>(result
            .Where(x => !string.IsNullOrWhiteSpace(x.PdfPath))
            .Select(x => Path.GetFullPath(x.PdfPath)), StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(archiveRoot) || !Directory.Exists(archiveRoot))
            return;

        foreach (var pdfPath in EnumerateLooseArchivePdfFiles(archiveRoot))
        {
            if (IsIncomingInvoicePath(pdfPath, archiveRoot))
                continue;

            var fullPdfPath = Path.GetFullPath(pdfPath);
            if (!seenPdfPaths.Add(fullPdfPath))
                continue;

            var jsonCandidate = Path.ChangeExtension(fullPdfPath, ".json");
            if (seenJsonPaths.Contains(jsonCandidate))
                continue;

            var fileName = Path.GetFileNameWithoutExtension(fullPdfPath);
            var docType = InferDocumentType(fileName);
            var documentNumber = InferDocumentNumber(fileName);

            result.Add(new ArchivEintrag
            {
                DokumentTyp = docType,
                DokumentNummer = documentNumber,
                AngehaengteBelegAnzahl = GetAttachedReceiptCount(documentNumber),
                Status = DokumentStatus.Archiviert,
                Datum = File.GetLastWriteTime(fullPdfPath),
                LetzteAktionAm = File.GetLastWriteTime(fullPdfPath),
                LetzteAktionText = "PDF vorhanden",
                Kunde = InferCustomerFromPath(archiveRoot, fullPdfPath, jsonCandidate),
                PdfPath = fullPdfPath,
                JsonPath = jsonCandidate
            });
        }
    }

    private static bool IsIncomingInvoicePath(string pdfPath, string archiveRoot)
    {
        var incomingRoot = Path.Combine(Path.GetFullPath(archiveRoot), "Eingangsrechnungen");
        var fullPdfPath = Path.GetFullPath(pdfPath);

        return fullPdfPath.StartsWith(incomingRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || string.Equals(fullPdfPath, incomingRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveJsonPath(ArchivEintrag eintrag)
    {
        if (!string.IsNullOrWhiteSpace(eintrag.JsonPath))
            return eintrag.JsonPath;

        if (!string.IsNullOrWhiteSpace(eintrag.PdfPath))
            return Path.ChangeExtension(eintrag.PdfPath, ".json");

        return string.Empty;
    }

    private static string? TryReadCustomerFromRechnung(JsonElement rootElement)
    {
        if (!rootElement.TryGetProperty("rechnung", out var rechnungElement) ||
            !rechnungElement.TryGetProperty("Empfänger", out var empfaengerElement) ||
            !empfaengerElement.TryGetProperty("Firmenname", out var firmennameElement))
            return null;

        var firmenname = firmennameElement.GetString();
        return string.IsNullOrWhiteSpace(firmenname) ? null : firmenname.Trim();
    }

    private static string InferCustomerFromPath(string root, string? pdfPath, string? jsonPath)
    {
        var sourcePath = !string.IsNullOrWhiteSpace(pdfPath) ? pdfPath : jsonPath;
        if (string.IsNullOrWhiteSpace(sourcePath))
            return string.Empty;

        var folder = Path.GetDirectoryName(sourcePath);
        if (string.IsNullOrWhiteSpace(folder))
            return string.Empty;

        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var fullFolder = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (string.Equals(fullFolder, fullRoot, StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        var candidate = Path.GetFileName(fullFolder)?.Trim();
        if (string.IsNullOrWhiteSpace(candidate) ||
            candidate.All(char.IsDigit) ||
            string.Equals(candidate, "Rechnung", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(candidate, "Angebot", StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        return candidate;
    }

    private static Rechnung CreateFallbackRechnung(ArchivEintrag entry)
    {
        return new Rechnung
        {
            DokumentTyp = entry.DokumentTyp,
            Rechnungsnummer = entry.DokumentNummer,
            Status = entry.Status,
            Rechnungsdatum = entry.Datum == default ? DateTime.Now : entry.Datum,
            Leistungsdatum = entry.DokumentTyp == DokumentTyp.Rechnung ? entry.Datum : null,
            LetzteAktionAm = entry.LetzteAktionAm,
            LetzteAktionText = entry.LetzteAktionText,
            Empfänger = new Adresse
            {
                Firmenname = entry.Kunde
            }
        };
    }

    private static Rechnung? TryBuildRechnungFromPdf(ArchivEintrag entry)
    {
        if (string.IsNullOrWhiteSpace(entry.PdfPath) || !File.Exists(entry.PdfPath))
            return null;

        try
        {
            var text = ExtractPdfText(entry.PdfPath);
            var rechnung = CreateFallbackRechnung(entry);

            var customer = ExtractCustomerFromPdfText(text);
            if (!string.IsNullOrWhiteSpace(customer))
                rechnung.Empfänger.Firmenname = customer;

            var total = ExtractTotalAmount(text);
            if (total.HasValue && total.Value > 0)
            {
                rechnung.Positionen.Add(new Rechnungsposition
                {
                    Nummer = 1,
                    Beschreibung = entry.DokumentTyp == DokumentTyp.Angebot ? "Angebot gesamt" : "Rechnung gesamt",
                    Menge = 1,
                    Einheit = "Pauschal",
                    EinzelPreis = total.Value,
                    Steuersatz = 0m
                });
            }

            return rechnung;
        }
        catch
        {
            return CreateFallbackRechnung(entry);
        }
    }

    private static string ExtractPdfText(string pdfPath)
    {
        var builder = new StringBuilder();
        using var document = PdfDocument.Open(pdfPath);
        foreach (var page in document.GetPages())
            builder.AppendLine(page.Text);

        return builder.ToString();
    }

    private static string? ExtractCustomerFromPdfText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        foreach (var line in lines)
        {
            var lowered = line.ToLowerInvariant();
            if (lowered.Contains("rechnung") || lowered.Contains("angebot") || lowered.Contains("rechnungsnummer") || lowered.Contains("angebotsnummer"))
                continue;

            if (Regex.IsMatch(line, "[A-Za-zÄÖÜäöüß]{3,}"))
                return line;
        }

        return null;
    }

    private void PersistRecoveredRechnung(ArchivEintrag entry, Rechnung rechnung)
    {
        var jsonPath = ResolveJsonPath(entry);
        if (string.IsNullOrWhiteSpace(jsonPath))
            return;

        if (string.IsNullOrWhiteSpace(entry.Kunde) && !string.IsNullOrWhiteSpace(rechnung.Empfänger?.Firmenname))
            entry.Kunde = rechnung.Empfänger.Firmenname;

        entry.JsonPath = jsonPath;
        entry.Status = rechnung.Status;
        entry.LetzteAktionAm = rechnung.LetzteAktionAm;
        entry.LetzteAktionText = rechnung.LetzteAktionText ?? entry.LetzteAktionText;
        entry.Datum = rechnung.Rechnungsdatum;

        var directory = Path.GetDirectoryName(jsonPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(new { eintrag = entry, rechnung }, JsonOptions);
        File.WriteAllText(jsonPath, json);
    }

    private static decimal? ExtractTotalAmount(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines.Reverse())
        {
            if (!line.Contains("Gesamtbetrag", StringComparison.OrdinalIgnoreCase) &&
                !line.Contains("Gesamt", StringComparison.OrdinalIgnoreCase) &&
                !line.Contains("Brutto", StringComparison.OrdinalIgnoreCase) &&
                !line.Contains("Summe", StringComparison.OrdinalIgnoreCase))
                continue;

            var matches = Regex.Matches(line, @"(?<!\d)(\d{1,3}(?:[\.\s]\d{3})*(?:,\d{2})|\d+(?:[\.,]\d{2}))(?!\d)");
            foreach (Match match in matches.Cast<Match>().Reverse())
            {
                var normalized = match.Value.Replace(" ", string.Empty).Replace(".", string.Empty).Replace(',', '.');
                if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
                    return amount;
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateArchiveJsonFiles(string root)
    {
        foreach (var directory in EnumerateDirectories(root))
        {
            IEnumerable<string> files;

            try
            {
                files = Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly);
            }
            catch
            {
                continue;
            }

            foreach (var file in files)
            {
                if (IsSupportedArchiveJsonFile(file))
                    yield return file;
            }
        }
    }

    private static IEnumerable<string> EnumerateLooseArchivePdfFiles(string root)
    {
        foreach (var directory in EnumerateDirectories(root))
        {
            IEnumerable<string> files;

            try
            {
                files = Directory.EnumerateFiles(directory, "*.pdf", SearchOption.TopDirectoryOnly);
            }
            catch
            {
                continue;
            }

            foreach (var file in files)
            {
                if (IsSupportedArchivePdfFile(file))
                    yield return file;
            }
        }
    }

    private static IEnumerable<string> EnumerateDirectories(string root)
    {
        var pending = new Stack<string>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            var current = pending.Pop();
            yield return current;

            IEnumerable<string> subDirectories;
            try
            {
                subDirectories = Directory.EnumerateDirectories(current, "*", SearchOption.TopDirectoryOnly);
            }
            catch
            {
                continue;
            }

            foreach (var subDirectory in subDirectories)
            {
                if (ShouldSkipDirectory(subDirectory))
                    continue;

                pending.Push(subDirectory);
            }
        }
    }

    private static bool ShouldSkipDirectory(string directoryPath)
    {
        var name = Path.GetFileName(directoryPath);
        return IgnoredDirectoryNames.Any(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsSupportedArchiveJsonFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        return fileName.StartsWith("Rechnung_", StringComparison.OrdinalIgnoreCase) ||
               fileName.StartsWith("Angebot_", StringComparison.OrdinalIgnoreCase) ||
               fileName.StartsWith("RE-", StringComparison.OrdinalIgnoreCase) ||
               fileName.StartsWith("AN-", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSupportedArchivePdfFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        return fileName.StartsWith("Rechnung_", StringComparison.OrdinalIgnoreCase) ||
               fileName.StartsWith("Angebot_", StringComparison.OrdinalIgnoreCase) ||
               fileName.StartsWith("RE-", StringComparison.OrdinalIgnoreCase) ||
               fileName.StartsWith("AN-", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetDocumentIdentity(ArchivEintrag entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.DokumentNummer))
            return $"{entry.DokumentTyp}:{entry.DokumentNummer.Trim()}";

        if (!string.IsNullOrWhiteSpace(entry.JsonPath))
            return $"json:{Path.GetFullPath(entry.JsonPath)}";

        if (!string.IsNullOrWhiteSpace(entry.PdfPath))
            return $"pdf:{Path.GetFullPath(entry.PdfPath)}";

        return Guid.NewGuid().ToString("N");
    }

    private static ArchivEintrag SelectPreferredEntry(IGrouping<string, ArchivEintrag> group)
    {
        return group
            .OrderByDescending(x => !string.IsNullOrWhiteSpace(x.JsonPath))
            .ThenByDescending(x => !string.IsNullOrWhiteSpace(x.Kunde))
            .ThenByDescending(x => x.Status)
            .ThenByDescending(x => x.LetzteAktionAm ?? x.Datum)
            .First();
    }

    private static DokumentTyp InferDocumentType(string fileName)
    {
        if (fileName.StartsWith("AN-", StringComparison.OrdinalIgnoreCase) ||
            fileName.StartsWith("Angebot_", StringComparison.OrdinalIgnoreCase))
            return DokumentTyp.Angebot;

        return DokumentTyp.Rechnung;
    }

    private static string InferDocumentNumber(string fileName)
    {
        if (fileName.StartsWith("Rechnung_", StringComparison.OrdinalIgnoreCase))
            return fileName["Rechnung_".Length..];

        if (fileName.StartsWith("Angebot_", StringComparison.OrdinalIgnoreCase))
            return fileName["Angebot_".Length..];

        return fileName;
    }

    private static string SanitizeFolder(string name) => string.Join("_", name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();

    private static string SanitizeFileName(string name) => SanitizeFolder(name);

    private int CountAttachedReceipts(string? invoiceNumber)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            return 0;

        return _eingangsrechnungService.LoadByAssignedInvoice(invoiceNumber).Count;
    }

    private int GetAttachedReceiptCount(string? invoiceNumber)
    {
        return CountAttachedReceipts(invoiceNumber);
    }

    public bool Loeschen(ArchivEintrag eintrag)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(eintrag.PdfPath) && File.Exists(eintrag.PdfPath))
                File.Delete(eintrag.PdfPath);
        }
        catch
        {
            return false;
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(eintrag.JsonPath) && File.Exists(eintrag.JsonPath))
                File.Delete(eintrag.JsonPath);
        }
        catch
        {
            return false;
        }

        try
        {
            var folder = Path.GetDirectoryName(eintrag.JsonPath ?? eintrag.PdfPath);
            if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder) && !Directory.EnumerateFileSystemEntries(folder).Any())
                Directory.Delete(folder);
        }
        catch
        {
            // ignore cleanup issues
        }

        return true;
    }
}
