using System.Windows;
using Örnek.Models;
using Örnek.Services;

namespace Örnek
{
    public partial class EmailSendWindow : Window
    {
        private readonly EmailService _email;
        private readonly EmailSettings _settings;
        private readonly byte[] _pdfBytes;
        private readonly string _attachmentName;
        private readonly LocalizationService _localization = LocalizationService.Instance;

        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;

        public EmailSendWindow(EmailService email, EmailSettings settings, byte[] pdfBytes, string attachmentName)
        {
            InitializeComponent();
            _email = email;
            _settings = settings;
            _pdfBytes = pdfBytes;
            _attachmentName = attachmentName;
            DataContext = this;
            ApplyLocalization();
        }

        private void ApplyLocalization()
        {
            Title = _localization["EmailSend.Title"];
            ToLabel.Text = _localization["EmailSend.To"];
            SubjectLabel.Text = _localization["EmailSend.Subject"];
            MessageLabel.Text = _localization["EmailSend.Message"];
            CancelButton.Content = _localization["Common.Cancel"];
            SendButton.Content = _localization["Common.Send"];
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _email.SendPdf(_settings, To, Subject, Body, _pdfBytes, _attachmentName);
                MessageBox.Show(_localization["EmailSend.Success"], _localization["Common.Info"], MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _localization["Common.Error"], MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
