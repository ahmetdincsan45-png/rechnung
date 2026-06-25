using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Örnek.Models
{
    public class Adresse : INotifyPropertyChanged
    {
        private string _firmenname = string.Empty;
        private string _strasse = string.Empty;
        private string _hausnummer = string.Empty;
        private string _postleitzahl = string.Empty;
        private string _stadt = string.Empty;
        private string _land = "Deutschland";
        private string? _telefon;
        private string? _email;
        private string? _webseite;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Firmenname
        {
            get => _firmenname;
            set => Set(ref _firmenname, value);
        }

        public string Strasse
        {
            get => _strasse;
            set => Set(ref _strasse, value);
        }

        public string Hausnummer
        {
            get => _hausnummer;
            set => Set(ref _hausnummer, value);
        }

        public string Postleitzahl
        {
            get => _postleitzahl;
            set => Set(ref _postleitzahl, value);
        }

        public string Stadt
        {
            get => _stadt;
            set => Set(ref _stadt, value);
        }

        public string Land
        {
            get => _land;
            set => Set(ref _land, value);
        }

        public string? Telefon
        {
            get => _telefon;
            set => Set(ref _telefon, value);
        }

        public string? Email
        {
            get => _email;
            set => Set(ref _email, value);
        }

        public string? Webseite
        {
            get => _webseite;
            set => Set(ref _webseite, value);
        }

        public override string ToString()
        {
            return $"{Firmenname}\n{Strasse} {Hausnummer}\n{Postleitzahl} {Stadt}\n{Land}";
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
}
