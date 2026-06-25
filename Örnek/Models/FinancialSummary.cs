namespace Örnek.Models;

public sealed class FinancialSummary
{
    public int InvoiceCount { get; init; }
    public int OfferCount { get; init; }
    public int IncomingInvoiceCount { get; init; }
    public int PaidInvoiceCount { get; init; }
    public int ArchivedInvoiceCount { get; init; }
    public decimal TotalInvoiceAmount { get; init; }
    public decimal RevenueTotal { get; init; }
    public decimal PaidRevenueTotal { get; init; }
    public decimal PaidRevenueTaxEstimate { get; init; }
    public decimal TotalPrincipalTaxEstimate { get; init; }
    public decimal ExpenseTotal { get; init; }
    public decimal OpenReceivables { get; init; }
    public decimal ProfitEstimate { get; init; }
    public decimal TaxReserveEstimate { get; init; }
    public decimal AverageInvoiceValue { get; init; }
    public decimal LinkedReceiptTotal { get; init; } // Bağlı gider faturaları toplamı
    public List<ArchivEintrag> RecentOutgoingDocuments { get; init; } = new();
    public List<EingangsrechnungEintrag> RecentIncomingDocuments { get; init; } = new();
}
