using Örnek.Models;

namespace Örnek.Services;

public sealed class InvoiceMatchService
{
    private readonly ArchivService _archivService;

    public InvoiceMatchService(ArchivService archivService)
    {
        _archivService = archivService;
    }

    public List<InvoiceMatchSuggestion> Suggest(EingangsrechnungEintrag entry)
    {
        var allInvoices = _archivService.ListeLaden()
            .Where(x => x.DokumentTyp == DokumentTyp.Rechnung)
            .ToList();

        var suggestions = new List<InvoiceMatchSuggestion>();
        foreach (var invoice in allInvoices)
        {
            var reason = GetReason(entry, invoice);
            if (string.IsNullOrWhiteSpace(reason))
                continue;

            suggestions.Add(new InvoiceMatchSuggestion
            {
                InvoiceNumber = invoice.DokumentNummer,
                Customer = invoice.Kunde,
                Date = invoice.Datum,
                Reason = reason
            });
        }

        return suggestions
            .OrderByDescending(x => x.Reason.Contains("Nummer", StringComparison.OrdinalIgnoreCase) || x.Reason.Contains("numara", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(x => x.Date)
            .Take(5)
            .ToList();
    }

    private static string GetReason(EingangsrechnungEintrag entry, ArchivEintrag invoice)
    {
        if (!string.IsNullOrWhiteSpace(entry.ZugeordneteRechnungNummer) &&
            string.Equals(entry.ZugeordneteRechnungNummer, invoice.DokumentNummer, StringComparison.OrdinalIgnoreCase))
            return "Bereits zugeordnet";

        if (!string.IsNullOrWhiteSpace(entry.Rechnungsnummer) &&
            string.Equals(entry.Rechnungsnummer, invoice.DokumentNummer, StringComparison.OrdinalIgnoreCase))
            return "Nummer stimmt überein";

        if (!string.IsNullOrWhiteSpace(entry.Projekt) &&
            invoice.Kunde.Contains(entry.Projekt, StringComparison.OrdinalIgnoreCase))
            return "Projekt/Kunde ähnlich";

        if (!string.IsNullOrWhiteSpace(entry.Lieferant) &&
            invoice.Kunde.Contains(entry.Lieferant, StringComparison.OrdinalIgnoreCase))
            return "Lieferant ähnelt Kunde";

        return string.Empty;
    }
}
