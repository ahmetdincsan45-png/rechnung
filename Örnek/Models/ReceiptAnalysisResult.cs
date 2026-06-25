namespace Örnek.Models;

public sealed class ReceiptAnalysisResult
{
    public string Supplier { get; init; } = string.Empty;
    public string InvoiceNumber { get; init; } = string.Empty;
    public string RawText { get; init; } = string.Empty;
    public string Hint { get; init; } = string.Empty;
}
