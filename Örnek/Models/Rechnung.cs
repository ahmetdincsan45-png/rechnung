using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Örnek.Models
{
    public class Rechnung : INotifyPropertyChanged
    {
        private ObservableCollection<Rechnungsposition> _positionen = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public Rechnung()
        {
            AttachPositionHandlers(_positionen);
        }

        public DokumentTyp DokumentTyp { get; set; } = DokumentTyp.Rechnung;

        public bool IstKleinunternehmer { get; set; }

        public string? AbsenderLogoPath { get; set; }

        // Angebot-spezifisch
        public DateTime? GueltigBis { get; set; }
        public string? Lieferzeit { get; set; }

        public string? ProjektObjektNr { get; set; }
        public string? Objektname { get; set; }

        public string Rechnungsnummer { get; set; } = string.Empty;
        public DokumentStatus Status { get; set; } = DokumentStatus.Entwurf;
        public DateTime? LetzteAktionAm { get; set; }
        public string? LetzteAktionText { get; set; }
        public DateTime Rechnungsdatum { get; set; } = DateTime.Now;
        public DateTime? Leistungsdatum { get; set; }
        public DateTime? Fälligkeitsdatum { get; set; }
        
        public Adresse Absender { get; set; } = new();
        public Adresse Empfänger { get; set; } = new();
        
        public string? Steuernummer { get; set; }
        public string? UstIdNr { get; set; }
        public string? Geschäftregisternummer { get; set; }
        
        public ObservableCollection<Rechnungsposition> Positionen
        {
            get => _positionen;
            set
            {
                if (ReferenceEquals(_positionen, value))
                    return;

                DetachPositionHandlers(_positionen);
                _positionen = value ?? new ObservableCollection<Rechnungsposition>();
                AttachPositionHandlers(_positionen);
                RaiseTotalsChanged();
                OnPropertyChanged(nameof(Positionen));
            }
        }
        
        public string? Zahlungsbedingungen { get; set; }
        public string? Angebotsbedingungen { get; set; }

        public string? AngebotEinleitungText { get; set; }
        public string? AngebotHaftungText { get; set; }
        public string? AngebotAuftragText { get; set; }
        public string? AngebotWiderrufText { get; set; }
        public bool ProfessionellerRechnungstextAktiv { get; set; }
        public string? Bankverbindung { get; set; }
        public string? Kontoinhaber { get; set; }
        public string? IBAN { get; set; }
        public string? BIC { get; set; }
        
        public string? Notizen { get; set; }

        [JsonIgnore]
        public string? ArchivPdfPath { get; set; }

        [JsonIgnore]
        public string? ArchivJsonPath { get; set; }

        [JsonIgnore]
        public string? SavePdfPath { get; set; }

        [JsonIgnore]
        public string? SaveJsonPath { get; set; }

        // Berechnungen
        public decimal GesamtNettoBetrag => Positionen.Sum(p => p.Betrag);
        public decimal GesamtSteuer => Positionen.Sum(p => p.Steuer);
        public decimal GesamtBruttoBetrag => GesamtNettoBetrag + GesamtSteuer;

        public IEnumerable<IGrouping<decimal, Rechnungsposition>> PositionenNachSteuersatz =>
            Positionen.GroupBy(p => p.Steuersatz);

        private void AttachPositionHandlers(ObservableCollection<Rechnungsposition> positionen)
        {
            positionen.CollectionChanged += Positionen_CollectionChanged;

            foreach (var position in positionen)
                position.PropertyChanged += Position_PropertyChanged;
        }

        private void DetachPositionHandlers(ObservableCollection<Rechnungsposition> positionen)
        {
            positionen.CollectionChanged -= Positionen_CollectionChanged;

            foreach (var position in positionen)
                position.PropertyChanged -= Position_PropertyChanged;
        }

        private void Positionen_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems is not null)
            {
                foreach (var position in e.OldItems.OfType<Rechnungsposition>())
                    position.PropertyChanged -= Position_PropertyChanged;
            }

            if (e.NewItems is not null)
            {
                foreach (var position in e.NewItems.OfType<Rechnungsposition>())
                    position.PropertyChanged += Position_PropertyChanged;
            }

            RaiseTotalsChanged();
        }

        private void Position_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            RaiseTotalsChanged();
        }

        private void RaiseTotalsChanged()
        {
            OnPropertyChanged(nameof(GesamtNettoBetrag));
            OnPropertyChanged(nameof(GesamtSteuer));
            OnPropertyChanged(nameof(GesamtBruttoBetrag));
            OnPropertyChanged(nameof(PositionenNachSteuersatz));
        }

        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
