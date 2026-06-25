namespace Örnek.Models;

public sealed class ArchivFilter
{
    public string Suchtext { get; set; } = string.Empty;
    public DokumentTyp? DokumentTyp { get; set; }
    public DokumentStatus? Status { get; set; }
    public DateTime? VonDatum { get; set; }
    public DateTime? BisDatum { get; set; }

    public bool HasAnyCriteria() =>
        !string.IsNullOrWhiteSpace(Suchtext) ||
        DokumentTyp.HasValue ||
        Status.HasValue ||
        VonDatum.HasValue ||
        BisDatum.HasValue;
}
