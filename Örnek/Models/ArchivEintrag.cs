namespace Örnek.Models;

public class ArchivEintrag
{
    public DokumentTyp DokumentTyp { get; set; }
    public string DokumentNummer { get; set; } = string.Empty;
    public int AngehaengteBelegAnzahl { get; set; }
    public DokumentStatus Status { get; set; } = DokumentStatus.Unbekannt;
    public DateTime Datum { get; set; }
    public DateTime? LetzteAktionAm { get; set; }
    public string LetzteAktionText { get; set; } = string.Empty;

    public string Kunde { get; set; } = string.Empty;

    public string PdfPath { get; set; } = string.Empty;
    public string JsonPath { get; set; } = string.Empty;
}
