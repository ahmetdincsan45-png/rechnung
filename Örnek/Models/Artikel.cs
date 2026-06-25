namespace Örnek.Models;

public class Artikel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string ArtikelNr { get; set; } = string.Empty;
    public string Bezeichnung { get; set; } = string.Empty;
    public string? Beschreibung { get; set; }

    public string Einheit { get; set; } = "Stück";
    public decimal StandardPreis { get; set; }
    public decimal StandardMwSt { get; set; } = 19m;

    public bool Aktiv { get; set; } = true;

    public string DisplayName => string.IsNullOrWhiteSpace(ArtikelNr)
        ? Bezeichnung
        : $"{ArtikelNr} - {Bezeichnung}";
}