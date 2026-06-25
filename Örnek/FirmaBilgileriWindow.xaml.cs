using System.Windows;
using System.Windows.Media.Imaging;
using System.IO;
using Microsoft.Win32;
using Örnek.Models;
using Örnek.Services;

namespace Örnek
{
    public partial class FirmaBilgileriWindow : Window
    {
        private readonly FirmaProfilService _service;
        private readonly LocalizationService _localization = LocalizationService.Instance;

        public FirmaProfil Profil { get; }

        public BitmapImage? LogoPreview { get; private set; }

        public FirmaBilgileriWindow(FirmaProfil profil, FirmaProfilService service)
        {
            InitializeComponent();
            _service = service;
            Profil = profil;

            DataContext = this;
            ApplyLocalization();
            RefreshPreview();
        }

        private void ApplyLocalization()
        {
            Title = _localization["Company.Title"];
            HeaderText.Text = _localization["Company.Header"];
            CompanyNameLabel.Text = _localization["Company.Label.Name"];
            StreetLabel.Text = _localization["Company.Label.Street"];
            NumberLabel.Text = _localization["Company.Label.Number"];
            PostalCodeLabel.Text = _localization["Company.Label.PostalCode"];
            CityLabel.Text = _localization["Company.Label.City"];
            CountryLabel.Text = _localization["Company.Label.Country"];
            PhoneLabel.Text = _localization["Company.Label.Phone"];
            EmailLabel.Text = _localization["Company.Label.Email"];
            WebLabel.Text = _localization["Company.Label.Web"];
            TaxNumberLabel.Text = _localization["Company.Label.TaxNumber"];
            VatIdLabel.Text = _localization["Company.Label.VatId"];
            LogoLabel.Text = _localization["Company.Label.Logo"];
            SelectLogoButton.Content = _localization["Company.SelectLogo"];
            PaymentTermsLabel.Text = _localization["Company.Label.PaymentTerms"];
            CancelButton.Content = _localization["Common.Cancel"];
            SaveButton.Content = _localization["Common.Save"];
        }

        private void RefreshPreview()
        {
            if (!string.IsNullOrWhiteSpace(Profil.LogoPath) && File.Exists(Profil.LogoPath))
            {
                var img = new BitmapImage();
                img.BeginInit();
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.UriSource = new Uri(Profil.LogoPath);
                img.EndInit();
                LogoPreview = img;
            }
            else
            {
                LogoPreview = null;
            }

            // refresh bindings
            DataContext = null;
            DataContext = this;
        }

        private void LogoSec_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Images (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All files (*.*)|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                Profil.LogoPath = _service.SaveLogoCopy(dlg.FileName);
                RefreshPreview();
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            _service.Save(Profil);
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
