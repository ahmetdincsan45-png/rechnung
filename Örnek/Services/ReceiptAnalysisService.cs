using System.Text.RegularExpressions;
using Örnek.Models;

namespace Örnek.Services;

public sealed class ReceiptAnalysisService
{
    private readonly LocalOcrService _ocrService = new();

    public string GetOcrDataFolder() => _ocrService.GetTessdataPath();

    public void EnsureOcrDataFolder() => _ocrService.EnsureTessdataFolder();

    public bool HasOcrLanguageData() => _ocrService.HasLanguageData();

    public ReceiptAnalysisResult Analyze(EingangsrechnungEintrag entry)
    {
        var text = BuildRawText(entry);
        var supplier = !string.IsNullOrWhiteSpace(entry.Lieferant)
            ? entry.Lieferant
            : ExtractSupplier(text);
        var invoiceNumber = !string.IsNullOrWhiteSpace(entry.Rechnungsnummer)
            ? entry.Rechnungsnummer
            : ExtractInvoiceNumber(text);

        return new ReceiptAnalysisResult
        {
            Supplier = supplier,
            InvoiceNumber = invoiceNumber,
            RawText = text,
            Hint = string.IsNullOrWhiteSpace(text)
                ? "ManualOnly"
                : "Heuristic"
        };
    }

    public async Task<ReceiptAnalysisResult> AnalyzeAsync(EingangsrechnungEintrag entry)
    {
        if (string.IsNullOrWhiteSpace(entry.OcrText) &&
            !string.IsNullOrWhiteSpace(entry.DokumentPfad))
        {
            try
            {
                entry.OcrText = await _ocrService.ExtractTextAsync(entry.DokumentPfad);
            }
            catch
            {
                // keep fallback heuristic flow
            }
        }

        return Analyze(entry);
    }

    private static string BuildRawText(EingangsrechnungEintrag entry)
    {
        var parts = new[]
        {
            entry.OriginalDateiname,
            entry.Notiz,
            entry.Lieferant,
            entry.Rechnungsnummer,
            entry.Projekt,
            entry.Kategorie,
            entry.OcrText
        };

        return string.Join(Environment.NewLine, parts.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static string FirstMeaningfulLine(string text)
    {
        return text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .FirstOrDefault(x => x.Length >= 3) ?? string.Empty;
    }

    private static string ExtractSupplier(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var blacklist = new[]
        {
            "rechnung", "invoice", "fatura", "datum", "date", "ust", "mwst", "vat", "brutto", "netto", "total", "summe"
        };

        foreach (var line in text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()))
        {
            if (line.Length < 3)
                continue;

            var lowered = line.ToLowerInvariant();
            if (blacklist.Any(lowered.Contains))
                continue;

            if (Regex.IsMatch(line, @"[A-Za-zÄÖÜäöüß]{3,}") && !Regex.IsMatch(line, @"^\d+$"))
                return line;
        }

        return FirstMeaningfulLine(text);
    }

    private static string ExtractInvoiceNumber(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var patterns = new[]
        {
            @"\b(RE|AN)-\d{8}-\d{4}\b",
            @"\b(?:Rechnung|Invoice|Fatura)[\s:#-]*([A-Z0-9\-/]{4,})",
            @"\b[A-Z]{1,4}[\-/]\d{3,}\b"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (!match.Success)
                continue;

            return match.Groups.Count > 1 && !string.IsNullOrWhiteSpace(match.Groups[1].Value)
                ? match.Groups[1].Value.Trim()
                : match.Value.Trim();
        }

        return string.Empty;
    }

    public DateTime? ExtractDate(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var patterns = new[]
        {
            @"\b(?<day>\d{2})\.(?<month>\d{2})\.(?<year>\d{4})\b",
            @"\b(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})\b",
            @"\b(?<day>\d{1,2})/(?<month>\d{1,2})/(?<year>\d{4})\b"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (!match.Success)
                continue;

            if (int.TryParse(match.Groups["day"].Value, out var day) &&
                int.TryParse(match.Groups["month"].Value, out var month) &&
                int.TryParse(match.Groups["year"].Value, out var year))
            {
                try
                {
                    return new DateTime(year, month, day);
                }
                catch
                {
                    return null;
                }
            }
        }

        return null;
    }

    public string ExtractCategory(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var checks = new (string Keyword, string Category)[]
        {
            ("material", "Material"),
            ("baustoff", "Baustoff"),
            ("werkzeug", "Werkzeug"),
            ("tank", "Fahrtkosten"),
            ("kraftstoff", "Fahrtkosten"),
            ("büro", "Bürobedarf"),
            ("software", "Software"),
            ("lizenz", "Software"),
            ("strom", "Betriebskosten"),
            ("miete", "Miete")
        };

        foreach (var (keyword, category) in checks)
        {
            if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return category;
        }

        return string.Empty;
    }

    public string ExtractProject(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var patterns = new[]
        {
            @"(?:Projekt|Objekt|Baustelle)\s*[:\-]\s*(?<value>.+)",
            @"(?:Project|Site)\s*[:\-]\s*(?<value>.+)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups["value"].Value.Trim();
        }

        return string.Empty;
    }

    public decimal? ExtractAmount(string text, params string[] keywords)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        foreach (var line in text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (keywords.Length > 0 && !keywords.Any(keyword => line.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                continue;

            var amountMatches = Regex.Matches(line, @"(?<!\d)(\d{1,3}(?:[\.\s]\d{3})*(?:,\d{2})|\d+(?:[\.,]\d{2}))(?!\d)");
            if (amountMatches.Count == 0)
                continue;

            foreach (Match amountMatch in amountMatches.Cast<Match>().OrderByDescending(x => x.Index))
            {
                var normalized = amountMatch.Value.Replace(" ", string.Empty).Replace(".", string.Empty).Replace(',', '.');
                if (decimal.TryParse(normalized, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var amount))
                    return amount;
            }
        }

        return null;
    }
}
