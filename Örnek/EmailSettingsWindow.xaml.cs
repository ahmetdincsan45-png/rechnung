using System.Windows;
using Örnek.Models;
using Örnek.Services;

namespace Örnek
{
    public partial class EmailSettingsWindow : Window
    {
        private readonly EmailSettingsService _service;
        private readonly LocalizationService _localization = LocalizationService.Instance;

        public EmailSettings Settings { get; }

        public EmailSettingsWindow(EmailSettings settings, EmailSettingsService service)
        {
            InitializeComponent();
            _service = service;
            Settings = settings;
            PasswordBox.Password = settings.Password ?? string.Empty;
            DataContext = this;
            ApplyLocalization();
        }

        private void ApplyLocalization()
        {
            Title = _localization["EmailSettings.Title"];
            UserLabel.Text = _localization["EmailSettings.User"];
            PasswordLabel.Text = _localization["EmailSettings.Password"];
            FromAddressLabel.Text = _localization["EmailSettings.FromAddress"];
            FromNameLabel.Text = _localization["EmailSettings.FromName"];
            NoteLabel.Text = _localization["EmailSettings.Note"];
            NoteText.Text = _localization["EmailSettings.NoteText"];
            SslCheckBox.Content = _localization.IsGerman ? "SSL aktivieren" : "SSL etkinleştir";
            CancelButton.Content = _localization["Common.Cancel"];
            SaveButton.Content = _localization["Common.Save"];
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Settings.Password = PasswordBox.Password;
            _service.Save(Settings);
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
