using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Örnek.Models;

public class EingangsrechnungEintrag : INotifyPropertyChanged
{
    private string _id = Guid.NewGuid().ToString("N");
    private string _lieferant = string.Empty;
    private string _rechnungsnummer = string.Empty;
    private DateTime _rechnungsdatum = DateTime.Today;
    private decimal _netto;
    private decimal _mwst;
    private decimal _brutto;
    private string _projekt = string.Empty;
    private string _kategorie = string.Empty;
    private string _zugeordneteRechnungNummer = string.Empty;
    private string _notiz = string.Empty;
    private string _dokumentPfad = string.Empty;
    private string _originalDateiname = string.Empty;
    private DateTime _erfasstAm = DateTime.Now;
    private string _ocrText = string.Empty;
    private string _erkannterLieferant = string.Empty;
    private string _erkannteRechnungsnummer = string.Empty;
    private bool _ocrVerarbeitet;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id
    {
        get => _id;
        set => Set(ref _id, value);
    }

    public string Lieferant
    {
        get => _lieferant;
        set => Set(ref _lieferant, value);
    }

    public string Rechnungsnummer
    {
        get => _rechnungsnummer;
        set => Set(ref _rechnungsnummer, value);
    }

    public DateTime Rechnungsdatum
    {
        get => _rechnungsdatum;
        set => Set(ref _rechnungsdatum, value);
    }

    public decimal Netto
    {
        get => _netto;
        set => Set(ref _netto, value);
    }

    public decimal MwSt
    {
        get => _mwst;
        set => Set(ref _mwst, value);
    }

    public decimal Brutto
    {
        get => _brutto;
        set => Set(ref _brutto, value);
    }

    public string Projekt
    {
        get => _projekt;
        set => Set(ref _projekt, value);
    }

    public string Kategorie
    {
        get => _kategorie;
        set => Set(ref _kategorie, value);
    }

    public string ZugeordneteRechnungNummer
    {
        get => _zugeordneteRechnungNummer;
        set => Set(ref _zugeordneteRechnungNummer, value);
    }

    public string Notiz
    {
        get => _notiz;
        set => Set(ref _notiz, value);
    }

    public string DokumentPfad
    {
        get => _dokumentPfad;
        set => Set(ref _dokumentPfad, value);
    }

    public string OriginalDateiname
    {
        get => _originalDateiname;
        set => Set(ref _originalDateiname, value);
    }

    public DateTime ErfasstAm
    {
        get => _erfasstAm;
        set => Set(ref _erfasstAm, value);
    }

    public string OcrText
    {
        get => _ocrText;
        set => Set(ref _ocrText, value);
    }

    public string ErkannterLieferant
    {
        get => _erkannterLieferant;
        set => Set(ref _erkannterLieferant, value);
    }

    public string ErkannteRechnungsnummer
    {
        get => _erkannteRechnungsnummer;
        set => Set(ref _erkannteRechnungsnummer, value);
    }

    public bool OcrVerarbeitet
    {
        get => _ocrVerarbeitet;
        set => Set(ref _ocrVerarbeitet, value);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        OnPropertyChanged(propertyName);
    }
}
