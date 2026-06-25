using System.ComponentModel;

namespace Örnek.Models
{
    public class Rechnungsposition : INotifyPropertyChanged
    {
        private int _nummer;
        private string _beschreibung = string.Empty;
        private string? _notiz;
        private decimal _menge;
        private string _einheit = "Stück";
        private decimal _einzelPreis;
        private decimal _steuersatz = 19m;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int Nummer
        {
            get => _nummer;
            set
            {
                if (_nummer == value) return;
                _nummer = value;
                OnPropertyChanged(nameof(Nummer));
            }
        }

        public string Beschreibung
        {
            get => _beschreibung;
            set
            {
                if (_beschreibung == value) return;
                _beschreibung = value;
                OnPropertyChanged(nameof(Beschreibung));
            }
        }

        public string? Notiz
        {
            get => _notiz;
            set
            {
                if (_notiz == value) return;
                _notiz = value;
                OnPropertyChanged(nameof(Notiz));
            }
        }

        public decimal Menge
        {
            get => _menge;
            set
            {
                if (_menge == value) return;
                _menge = value;
                OnPropertyChanged(nameof(Menge));
                RaiseCalculated();
            }
        }

        public string Einheit
        {
            get => _einheit;
            set
            {
                if (_einheit == value) return;
                _einheit = value;
                OnPropertyChanged(nameof(Einheit));
            }
        }

        public decimal EinzelPreis
        {
            get => _einzelPreis;
            set
            {
                if (_einzelPreis == value) return;
                _einzelPreis = value;
                OnPropertyChanged(nameof(EinzelPreis));
                RaiseCalculated();
            }
        }

        public decimal Steuersatz
        {
            get => _steuersatz;
            set
            {
                if (_steuersatz == value) return;
                _steuersatz = value;
                OnPropertyChanged(nameof(Steuersatz));
                RaiseCalculated();
            }
        }

        public decimal Betrag => Menge * EinzelPreis;
        public decimal SteuerbaresMenge => Betrag;
        public decimal Steuer => SteuerbaresMenge * (Steuersatz / 100);
        public decimal GesamtBetrag => Betrag + Steuer;

        private void RaiseCalculated()
        {
            OnPropertyChanged(nameof(Betrag));
            OnPropertyChanged(nameof(SteuerbaresMenge));
            OnPropertyChanged(nameof(Steuer));
            OnPropertyChanged(nameof(GesamtBetrag));
        }

        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
