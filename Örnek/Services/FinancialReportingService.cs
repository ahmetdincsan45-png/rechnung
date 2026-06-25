using Örnek.Models;

namespace Örnek.Services;

public sealed class FinancialReportingService
{
    private readonly ArchivService _archivService;
    private readonly EingangsrechnungService _incomingService;

    public FinancialReportingService(ArchivService archivService, EingangsrechnungService incomingService)
    {
        _archivService = archivService;
        _incomingService = incomingService;
    }

    public FinancialSummary BuildSummary()
    {
        var outgoing = _archivService.ListeLaden();
        var incoming = _incomingService.LoadAll();

        var invoices = outgoing.Where(x => x.DokumentTyp == DokumentTyp.Rechnung).ToList();
        var offers = outgoing.Where(x => x.DokumentTyp == DokumentTyp.Angebot).ToList();
        var paidInvoices = invoices.Where(x => x.Status == DokumentStatus.Bezahlt).ToList();
        var openInvoices = invoices.Where(x => x.Status != DokumentStatus.Bezahlt).ToList();
        var archivedInvoices = invoices.Where(x => x.Status == DokumentStatus.Archiviert || x.Status == DokumentStatus.Bezahlt).ToList();

        var totalInvoiceAmount = SumOutgoingInvoiceTotals(invoices);
        var totalPrincipalAmount = SumOutgoingInvoiceNetTotals(invoices);
        var revenueTotal = SumOutgoingInvoiceTotals(openInvoices);
        var paidRevenueTotal = SumOutgoingInvoiceTotals(paidInvoices);
        var expenseTotal = incoming.Sum(x => x.Brutto);
        var openReceivables = revenueTotal;
        var linkedReceiptTotal = incoming.Where(x => !string.IsNullOrWhiteSpace(x.ZugeordneteRechnungNummer)).Sum(x => x.Brutto);

        return new FinancialSummary
        {
            InvoiceCount = invoices.Count,
            OfferCount = offers.Count,
            IncomingInvoiceCount = incoming.Count,
            PaidInvoiceCount = paidInvoices.Count,
            ArchivedInvoiceCount = archivedInvoices.Count,
            TotalInvoiceAmount = totalInvoiceAmount,
            RevenueTotal = revenueTotal,
            PaidRevenueTotal = paidRevenueTotal,
            PaidRevenueTaxEstimate = Math.Max(0, paidRevenueTotal) * 0.20m,
            TotalPrincipalTaxEstimate = Math.Max(0, totalPrincipalAmount) * 0.20m,
            ExpenseTotal = expenseTotal,
            OpenReceivables = openReceivables,
            ProfitEstimate = revenueTotal - expenseTotal,
            TaxReserveEstimate = Math.Max(0, revenueTotal - expenseTotal) * 0.20m,
            AverageInvoiceValue = openInvoices.Count == 0 ? 0 : revenueTotal / openInvoices.Count,
            LinkedReceiptTotal = linkedReceiptTotal,
            RecentOutgoingDocuments = outgoing.OrderByDescending(x => x.Datum).Take(8).ToList(),
            RecentIncomingDocuments = incoming.OrderByDescending(x => x.Rechnungsdatum).Take(8).ToList()
        };
    }

    private decimal SumOutgoingInvoiceTotals(List<ArchivEintrag> entries)
    {
        decimal total = 0;
        foreach (var entry in entries)
        {
            try
            {
                var rechnung = _archivService.LoadRechnung(entry);
                if (rechnung != null)
                    total += rechnung.GesamtBruttoBetrag;
            }
            catch
            {
                // ignore malformed entries in summary
            }
        }

        return total;
    }

    private decimal SumOutgoingInvoiceNetTotals(List<ArchivEintrag> entries)
    {
        decimal total = 0;
        foreach (var entry in entries)
        {
            try
            {
                var rechnung = _archivService.LoadRechnung(entry);
                if (rechnung != null)
                    total += rechnung.GesamtNettoBetrag;
            }
            catch
            {
                // ignore malformed entries in summary
            }
        }

        return total;
    }
}
