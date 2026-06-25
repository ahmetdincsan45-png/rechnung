namespace Örnek.Models;

public sealed class InvoiceMatchSuggestion
{
    public string InvoiceNumber { get; init; } = string.Empty;
    public string Customer { get; init; } = string.Empty;
    public DateTime Date { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string DisplayText => $"{InvoiceNumber} | {Customer} | {Date:dd.MM.yyyy} | {Reason}";
}
