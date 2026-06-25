using System.Windows;
using System.ComponentModel;
using Örnek.Models;
using Örnek.Services;

namespace Örnek
{
    public partial class FinanzDashboardWindow : Window, INotifyPropertyChanged
    {
        private string _linkedReceiptTotalTitle = string.Empty;
        public string LinkedReceiptTotalTitle
        {
            get => _linkedReceiptTotalTitle;
            set { _linkedReceiptTotalTitle = value; OnPropertyChanged(nameof(LinkedReceiptTotalTitle)); }
        }
        private readonly LocalizationService _localization = LocalizationService.Instance;
        private readonly ArchivService _archivService;
        private decimal _selectedInvoiceAmount;
        private decimal _selectedInvoiceTaxAmount;
        private string _selectedInvoiceCustomer = string.Empty;
        private string _selectedInvoiceNumber = string.Empty;
        private string _selectedInvoiceDate = string.Empty;
        private Visibility _selectedInvoiceTaxVisibility = Visibility.Collapsed;

        public FinancialSummary Summary { get; private set; }
        public decimal SelectedInvoiceAmount
        {
            get => _selectedInvoiceAmount;
            private set
            {
                if (_selectedInvoiceAmount == value)
                    return;

                _selectedInvoiceAmount = value;
                OnPropertyChanged(nameof(SelectedInvoiceAmount));
            }
        }

        public string SelectedInvoiceCustomer
        {
            get => _selectedInvoiceCustomer;
            private set
            {
                if (_selectedInvoiceCustomer == value)
                    return;

                _selectedInvoiceCustomer = value;
                OnPropertyChanged(nameof(SelectedInvoiceCustomer));
            }
        }

        public string SelectedInvoiceNumber
        {
            get => _selectedInvoiceNumber;
            private set
            {
                if (_selectedInvoiceNumber == value)
                    return;

                _selectedInvoiceNumber = value;
                OnPropertyChanged(nameof(SelectedInvoiceNumber));
            }
        }

        public string SelectedInvoiceDate
        {
            get => _selectedInvoiceDate;
            private set
            {
                if (_selectedInvoiceDate == value)
                    return;

                _selectedInvoiceDate = value;
                OnPropertyChanged(nameof(SelectedInvoiceDate));
            }
        }

        public decimal SelectedInvoiceTaxAmount
        {
            get => _selectedInvoiceTaxAmount;
            private set
            {
                if (_selectedInvoiceTaxAmount == value)
                    return;

                _selectedInvoiceTaxAmount = value;
                OnPropertyChanged(nameof(SelectedInvoiceTaxAmount));
            }
        }

        public Visibility SelectedInvoiceTaxVisibility
        {
            get => _selectedInvoiceTaxVisibility;
            private set
            {
                if (_selectedInvoiceTaxVisibility == value)
                    return;

                _selectedInvoiceTaxVisibility = value;
                OnPropertyChanged(nameof(SelectedInvoiceTaxVisibility));
            }
        }

        public FinanzDashboardWindow(FinancialSummary summary, ArchivService archivService)
        {
            InitializeComponent();
            Summary = summary;
            _archivService = archivService;
            DataContext = this;
            LinkedReceiptTotalTitle = _localization["Finance.LinkedReceiptTotal"];
            ApplyLocalization();
            ResetSelectedInvoiceDisplay();
        }

        private void ApplyLocalization()
        {
            Title = _localization["Finance.Title"];
            HeaderText.Text = _localization["Finance.Title"];
            LinkedReceiptTotalTitle = _localization["Finance.LinkedReceiptTotal"];
            if (FindName("TotalInvoiceAmountTitleText") is System.Windows.Controls.TextBlock totalInvoiceAmountTitleText)
                totalInvoiceAmountTitleText.Text = _localization["Finance.TotalInvoiceAmount"];
            OpenReceivablesTitleText.Text = _localization["Finance.OpenReceivables"];
            if (FindName("PaidRevenueTitleText") is System.Windows.Controls.TextBlock paidRevenueTitleText)
                paidRevenueTitleText.Text = _localization["Finance.PaidRevenue"];
            if (FindName("PaidRevenueTaxTitleText") is System.Windows.Controls.TextBlock paidRevenueTaxTitleText)
                paidRevenueTaxTitleText.Text = _localization["Finance.PaidRevenueTax"];
            if (FindName("TotalPrincipalTaxTitleText") is System.Windows.Controls.TextBlock totalPrincipalTaxTitleText)
                totalPrincipalTaxTitleText.Text = _localization["Finance.TotalPrincipalTax"];
            if (FindName("SelectedInvoiceTitleText") is System.Windows.Controls.TextBlock selectedInvoiceTitleText)
                selectedInvoiceTitleText.Text = _localization["Finance.SelectedInvoice"];

            if (FindName("SelectedCustomerLabelText") is System.Windows.Controls.TextBlock selectedCustomerLabelText)
                selectedCustomerLabelText.Text = _localization["Finance.SelectedInvoice.Customer"];

            if (FindName("SelectedDateLabelText") is System.Windows.Controls.TextBlock selectedDateLabelText)
                selectedDateLabelText.Text = _localization["Finance.SelectedInvoice.Date"];

            if (FindName("SelectedNumberLabelText") is System.Windows.Controls.TextBlock selectedNumberLabelText)
                selectedNumberLabelText.Text = _localization["Finance.SelectedInvoice.Number"];

            if (FindName("SelectedAmountLabelText") is System.Windows.Controls.TextBlock selectedAmountLabelText)
                selectedAmountLabelText.Text = _localization["Finance.SelectedInvoice.Amount"];

            if (FindName("SelectedInvoiceTaxTitleText") is System.Windows.Controls.TextBlock selectedInvoiceTaxTitleText)
                selectedInvoiceTaxTitleText.Text = _localization["Finance.SelectedInvoice.Tax"];

            if (FindName("SelectedInvoiceAmountCardTitleText") is System.Windows.Controls.TextBlock selectedInvoiceAmountCardTitleText)
                selectedInvoiceAmountCardTitleText.Text = _localization["Finance.SelectedInvoice.Amount"];

            InvoiceCountTitleText.Text = _localization["Finance.InvoiceCount"];
            PaidCountTitleText.Text = _localization["Finance.PaidCount"];
            IncomingCountTitleText.Text = _localization["Finance.IncomingCount"];
            MarkOpenButton.Content = _localization["Finance.MarkOpen"];
            MarkPaidButton.Content = _localization["Finance.MarkPaid"];
            OutgoingGroupBox.Header = _localization["Finance.OutgoingDocuments"];
            IncomingGroupBox.Header = _localization["Finance.IncomingDocuments"];
            OutgoingTypeColumn.Header = _localization["Archive.Column.Type"];
            OutgoingNumberColumn.Header = _localization["Archive.Column.Number"];
            OutgoingStatusColumn.Header = _localization["Archive.Column.Status"];
            OutgoingCustomerColumn.Header = _localization["Archive.Column.Customer"];
            IncomingSupplierColumn.Header = _localization["IncomingInvoices.Column.Supplier"];
            IncomingNumberColumn.Header = _localization["IncomingInvoices.Column.Number"];
            IncomingGrossColumn.Header = _localization["IncomingInvoices.Column.Gross"];
            if (string.IsNullOrWhiteSpace(SelectedInvoiceNumber))
                ResetSelectedInvoiceDisplay();
        }

        private void MarkPaidButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateSelectedStatus(DokumentStatus.Bezahlt, _localization["Finance.ActionPaid"]);
        }

        private void MarkOpenButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateSelectedStatus(DokumentStatus.Archiviert, _localization["Finance.ActionOpen"]);
        }

        private void UpdateSelectedStatus(DokumentStatus status, string actionText)
        {
            if (OutgoingGrid.SelectedItem is not ArchivEintrag selected)
            {
                MessageBox.Show(_localization["Finance.SelectInvoiceFirst"], _localization["Common.Info"], MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!_archivService.UpdateStatus(selected, status, actionText))
            {
                MessageBox.Show(_localization["Finance.StatusUpdateFailed"], _localization["Common.Error"], MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void OutgoingGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (OutgoingGrid.SelectedItem is not ArchivEintrag selected)
            {
                ResetSelectedInvoiceDisplay();
                return;
            }

            try
            {
                var rechnung = _archivService.LoadRechnung(selected);
                SelectedInvoiceAmount = rechnung?.GesamtBruttoBetrag ?? 0m;
                SelectedInvoiceTaxAmount = Math.Round(SelectedInvoiceAmount * 0.20m, 2);
                SelectedInvoiceTaxVisibility = Visibility.Visible;
                SelectedInvoiceCustomer = string.IsNullOrWhiteSpace(selected.Kunde)
                    ? _localization["Finance.SelectedInvoice.None"]
                    : selected.Kunde;
                SelectedInvoiceNumber = string.IsNullOrWhiteSpace(selected.DokumentNummer)
                    ? "-"
                    : selected.DokumentNummer;
                SelectedInvoiceDate = selected.Datum == default
                    ? "-"
                    : selected.Datum.ToString("dd.MM.yyyy");
            }
            catch
            {
                SelectedInvoiceAmount = 0m;
                SelectedInvoiceTaxAmount = 0m;
                SelectedInvoiceTaxVisibility = Visibility.Collapsed;
                SelectedInvoiceCustomer = _localization["Finance.SelectedInvoice.None"];
                SelectedInvoiceNumber = "-";
                SelectedInvoiceDate = "-";
            }
        }

        private void ResetSelectedInvoiceDisplay()
        {
            SelectedInvoiceAmount = 0m;
            SelectedInvoiceTaxAmount = 0m;
            SelectedInvoiceTaxVisibility = Visibility.Collapsed;
            SelectedInvoiceCustomer = _localization["Finance.SelectedInvoice.None"];
            SelectedInvoiceNumber = "-";
            SelectedInvoiceDate = "-";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
