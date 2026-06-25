namespace Örnek.Models;

public class FirmaProfil
{
    public Adresse Adresse { get; set; } = new();

    public string? Steuernummer { get; set; }
    public string? UstIdNr { get; set; }

    public string? Kontoinhaber { get; set; }
    public string? IBAN { get; set; }
    public string? BIC { get; set; }

    public string? Zahlungsbedingungen { get; set; }

    public string? Angebotsbedingungen { get; set; }

    public string? AngebotEinleitungText { get; set; }
    public string? AngebotHaftungText { get; set; }
    public string? AngebotAuftragText { get; set; }
    public string? AngebotWiderrufText { get; set; }

    public string? LogoPath { get; set; }
}
