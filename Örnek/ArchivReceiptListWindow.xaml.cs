using System.Collections.Generic;
using System.Windows;
using Örnek.Models;
using Örnek.Services;

namespace Örnek;

public partial class ArchivReceiptListWindow : Window
{
    private readonly LocalizationService _localization = LocalizationService.Instance;
    private readonly EingangsrechnungService _eingangsrechnungService = new(new ArchivSettingsService());

    public ArchivReceiptListWindow(string invoiceNumber, IReadOnlyCollection<EingangsrechnungEintrag> receipts)
    {
        InitializeComponent();
        ApplyLocalization(invoiceNumber, receipts.Count);
        ReceiptsGrid.ItemsSource = receipts;
        EmptyStateText.Visibility = receipts.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ReceiptsGrid.Visibility = receipts.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ApplyLocalization(string invoiceNumber, int receiptCount)
    {
        Title = _localization["Archive.Receipts.Title"];
        HeaderText.Text = _localization["Archive.Receipts.Header"];
        SubtitleText.Text = string.Format(_localization["Archive.Receipts.Subtitle"], invoiceNumber, receiptCount);
        EmptyStateText.Text = _localization["Archive.Receipts.Empty"];
        CloseButton.Content = _localization["Common.Close"];
        SupplierColumn.Header = _localization["IncomingInvoices.Column.Supplier"];
        NumberColumn.Header = _localization["IncomingInvoices.Column.Number"];
        DateColumn.Header = _localization["IncomingInvoices.Column.Date"];
        GrossColumn.Header = _localization["IncomingInvoices.Column.Gross"];
        ProjectColumn.Header = _localization["IncomingInvoices.Column.Project"];
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ReceiptsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (ReceiptsGrid.SelectedItem is not EingangsrechnungEintrag receipt)
            return;

        try
        {
            _eingangsrechnungService.OpenDocument(receipt);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                string.Format(_localization["IncomingInvoices.OpenFailed"], ex.Message),
                _localization["Common.Error"],
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}