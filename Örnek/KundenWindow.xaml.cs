using System.Collections.ObjectModel;
using System.Windows;
using Örnek.Models;
using Örnek.Services;

namespace Örnek
{
    public partial class KundenWindow : Window
    {
        private readonly KundenService _service;
        private readonly LocalizationService _localization = LocalizationService.Instance;

        public ObservableCollection<Kunde> Kunden { get; }

        public Kunde? Selected { get; set; }

        public KundenWindow(KundenService service)
        {
            InitializeComponent();
            _service = service;
            Kunden = new ObservableCollection<Kunde>(_service.Load());
            KundenGrid.ItemsSource = Kunden;

            if (Kunden.Count > 0)
            {
                KundenGrid.SelectedIndex = 0;
                Selected = Kunden[0];
            }

            DataContext = this;
            ApplyLocalization();
        }

        private void ApplyLocalization()
        {
            Title = _localization["Customers.Title"];
            NewButton.Content = _localization["Common.New"];
            DeleteButton.Content = _localization["Common.Delete"];
            SaveButton.Content = _localization["Common.Save"];
            CloseButton.Content = _localization["Common.Close"];
            CompanyColumn.Header = _localization["Customers.Column.Company"];
            CityColumn.Header = _localization["Customers.Column.City"];
            EmailColumn.Header = _localization["Customers.Column.Email"];
            DetailGroupBox.Header = _localization["Customers.Detail"];
            CompanyLabel.Text = _localization["Customers.Label.Company"];
            StreetLabel.Text = _localization["Customers.Label.Street"];
            NumberLabel.Text = _localization["Customers.Label.Number"];
            PostalCodeLabel.Text = _localization["Customers.Label.PostalCode"];
            CityLabel.Text = _localization["Customers.Label.City"];
            CountryLabel.Text = _localization["Customers.Label.Country"];
            EmailLabel.Text = _localization["Customers.Label.Email"];
        }

        private void KundenGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            Selected = KundenGrid.SelectedItem as Kunde;
            DataContext = null;
            DataContext = this;
        }

        private void Yeni_Click(object sender, RoutedEventArgs e)
        {
            var k = new Kunde();
            Kunden.Add(k);
            KundenGrid.SelectedItem = k;
        }

        private void Sil_Click(object sender, RoutedEventArgs e)
        {
            if (KundenGrid.SelectedItem is Kunde k)
                Kunden.Remove(k);
        }

        private void Kaydet_Click(object sender, RoutedEventArgs e)
        {
            _service.Save(Kunden.ToList());
            MessageBox.Show(_localization["Common.Saved"], _localization["Common.Info"], MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Kapat_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
