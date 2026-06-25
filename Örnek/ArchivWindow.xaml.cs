using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Örnek.Models;
using Örnek.Services;

namespace Örnek
{
    public partial class ArchivWindow : Window
    {
        private sealed class FilterOption<T>
        {
            public string Label { get; init; } = string.Empty;
            public T? Value { get; init; }
            public override string ToString() => Label;
        }

        private readonly ArchivService _archiv;
        private readonly EingangsrechnungService _eingangsrechnungService = new(new ArchivSettingsService());
        private readonly RechnungService _rechnungService = new();
        private readonly LocalizationService _localization = LocalizationService.Instance;

        public ArchivWindow(ArchivService archiv)
        {
            InitializeComponent();
            _archiv = archiv;
            ApplyLocalization();
            LoadFilters();
            LoadData();
        }

        private void ApplyLocalization()
        {
            Title = _localization["Archive.Title"];
            RefreshButton.Content = _localization["Archive.Refresh"];
            OpenPdfButton.Content = _localization["Archive.OpenPdf"];
            OpenFolderButton.Content = _localization["Archive.OpenFolder"];
            DeleteButton.Content = _localization["Archive.Delete"];
            EditButton.Content = _localization["Archive.Edit"];
            TypeColumn.Header = _localization["Archive.Column.Type"];
            NumberColumn.Header = _localization["Archive.Column.Number"];
            if (FindName("AttachmentCountColumn") is DataGridTextColumn attachmentCountColumn)
                attachmentCountColumn.Header = _localization["Archive.Column.Attachments"];
            StatusColumn.Header = _localization["Archive.Column.Status"];
            DateColumn.Header = _localization["Archive.Column.Date"];
            LastActionColumn.Header = _localization["Archive.Column.LastAction"];
            CustomerColumn.Header = _localization["Archive.Column.Customer"];
            SearchTextBox.ToolTip = _localization["Archive.Search.Placeholder"];
        }

        private void LoadData()
        {
            DokumentTyp? selectedType = TypeFilterComboBox.SelectedIndex > 0 && TypeFilterComboBox.SelectedItem is FilterOption<DokumentTyp> typeOption
                ? (DokumentTyp?)typeOption.Value
                : null;

            DokumentStatus? selectedStatus = StatusFilterComboBox.SelectedIndex > 0 && StatusFilterComboBox.SelectedItem is FilterOption<DokumentStatus> statusOption
                ? (DokumentStatus?)statusOption.Value
                : null;

            var filter = new ArchivFilter
            {
                Suchtext = SearchTextBox.Text,
                DokumentTyp = selectedType,
                Status = selectedStatus,
                VonDatum = FromDatePicker.SelectedDate,
                BisDatum = ToDatePicker.SelectedDate
            };

            ArchivGrid.ItemsSource = filter.HasAnyCriteria() ? _archiv.Suche(filter) : _archiv.ListeLaden();
        }

        private void LoadFilters()
        {
            TypeFilterComboBox.ItemsSource = new object[]
            {
                new FilterOption<DokumentTyp> { Label = _localization["Archive.Filter.AllTypes"] },
                new FilterOption<DokumentTyp> { Label = _localization["Main.Doc.Invoice"], Value = DokumentTyp.Rechnung },
                new FilterOption<DokumentTyp> { Label = _localization["Main.Doc.Offer"], Value = DokumentTyp.Angebot }
            };
            TypeFilterComboBox.SelectedIndex = 0;

            StatusFilterComboBox.ItemsSource = new object[]
            {
                new FilterOption<DokumentStatus> { Label = _localization["Archive.Filter.AllStatuses"] },
                new FilterOption<DokumentStatus> { Label = _localization["Archive.Status.Draft"], Value = DokumentStatus.Entwurf },
                new FilterOption<DokumentStatus> { Label = _localization["Archive.Status.Preview"], Value = DokumentStatus.Vorschau },
                new FilterOption<DokumentStatus> { Label = _localization["Archive.Status.Sent"], Value = DokumentStatus.Gesendet },
                new FilterOption<DokumentStatus> { Label = _localization["Archive.Status.Archived"], Value = DokumentStatus.Archiviert },
                new FilterOption<DokumentStatus> { Label = _localization["Archive.Status.Paid"], Value = DokumentStatus.Bezahlt }
            };
            StatusFilterComboBox.SelectedIndex = 0;
        }

        private void SearchFilter_Changed(object sender, RoutedEventArgs e) => LoadData();

        private void Yenile_Click(object sender, RoutedEventArgs e) => LoadData();

        private void ArchivGrid_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is not DependencyObject source)
                return;

            var cell = FindParent<DataGridCell>(source);
            if (cell == null || cell.Column != AttachmentCountColumn)
                return;

            if (FindParent<DataGridRow>(cell) is not DataGridRow { Item: ArchivEintrag entry })
                return;

            ShowAttachedReceipts(entry);
            e.Handled = true;
        }

        private void PdfAc_Click(object sender, RoutedEventArgs e)
        {
            if (ArchivGrid.SelectedItem is not ArchivEintrag e1)
                return;

            RefreshCombinedInvoicePdf(e1);

            if (File.Exists(e1.PdfPath))
            {
                Process.Start(new ProcessStartInfo(e1.PdfPath) { UseShellExecute = true });
            }
        }

        private void ShowAttachedReceipts(ArchivEintrag entry)
        {
            if (entry.DokumentTyp != DokumentTyp.Rechnung || string.IsNullOrWhiteSpace(entry.DokumentNummer))
                return;

            var receipts = _eingangsrechnungService.LoadByAssignedInvoice(entry.DokumentNummer);
            var window = new ArchivReceiptListWindow(entry.DokumentNummer, receipts)
            {
                Owner = this
            };
            window.ShowDialog();
        }

        private void RefreshCombinedInvoicePdf(ArchivEintrag entry)
        {
            if (entry.DokumentTyp != DokumentTyp.Rechnung)
                return;

            try
            {
                var rechnung = _archiv.LoadRechnung(entry);
                if (rechnung == null || string.IsNullOrWhiteSpace(rechnung.Rechnungsnummer))
                    return;

                var attachmentPaths = _eingangsrechnungService.LoadByAssignedInvoice(rechnung.Rechnungsnummer)
                    .Where(x => !string.IsNullOrWhiteSpace(x.DokumentPfad)
                        && File.Exists(x.DokumentPfad)
                        && IsSupportedReceiptAttachment(x.DokumentPfad))
                    .Select(x => x.DokumentPfad!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (attachmentPaths.Count == 0)
                    return;

                rechnung.ArchivPdfPath = entry.PdfPath;
                rechnung.ArchivJsonPath = entry.JsonPath;

                var pdfBytes = _rechnungService.GenerierePdfBytes(rechnung, attachmentPaths);
                var updatedEntry = _archiv.ArchivEintragAktualisieren(rechnung, pdfBytes);
                if (updatedEntry != null)
                {
                    entry.PdfPath = updatedEntry.PdfPath;
                    entry.JsonPath = updatedEntry.JsonPath;
                }
            }
            catch
            {
                // keep opening the existing archive PDF if refresh fails
            }
        }

        private static bool IsSupportedReceiptAttachment(string path)
        {
            var extension = Path.GetExtension(path);

            return string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".bmp", StringComparison.OrdinalIgnoreCase);
        }

        private static T? FindParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T match)
                    return match;

                child = VisualTreeHelper.GetParent(child);
            }

            return null;
        }

        private void KlasorAc_Click(object sender, RoutedEventArgs e)
        {
            if (ArchivGrid.SelectedItem is not ArchivEintrag e1)
                return;

            var folder = Path.GetDirectoryName(e1.PdfPath);
            if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder))
            {
                Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
            }
        }

        private void Sil_Click(object sender, RoutedEventArgs e)
        {
            if (ArchivGrid.SelectedItem is not ArchivEintrag e1)
                return;

            var result = MessageBox.Show(
                _localization["Archive.DeleteConfirmMessage"],
                _localization["Archive.DeleteConfirmTitle"],
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            if (!_archiv.Loeschen(e1))
            {
                MessageBox.Show(_localization["Archive.DeleteFailed"], _localization["Common.Error"], MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            LoadData();
        }

        private void Duzenle_Click(object sender, RoutedEventArgs e)
        {
            if (ArchivGrid.SelectedItem is not ArchivEintrag e1)
                return;

            if (string.IsNullOrWhiteSpace(e1.JsonPath) || !File.Exists(e1.JsonPath))
            {
                MessageBox.Show(_localization["Archive.JsonMissing"], _localization["Common.Error"], MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var rechnung = _archiv.LoadRechnung(e1) ?? CreateFallbackRechnung(e1);

                if (rechnung == null)
                {
                    MessageBox.Show(_localization["Archive.EntryUnreadable"], _localization["Common.Error"], MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (Owner is not MainWindow mw)
                {
                    MessageBox.Show(_localization["Archive.MainWindowMissing"], _localization["Common.Error"], MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                mw.LoadFromArchive(rechnung);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(_localization["Archive.OpenFailed"], ex.Message), _localization["Common.Error"], MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static Rechnung CreateFallbackRechnung(ArchivEintrag entry)
        {
            return new Rechnung
            {
                DokumentTyp = entry.DokumentTyp,
                Rechnungsnummer = entry.DokumentNummer,
                Status = entry.Status,
                Rechnungsdatum = entry.Datum == default ? DateTime.Now : entry.Datum,
                Leistungsdatum = entry.DokumentTyp == DokumentTyp.Rechnung ? entry.Datum : null,
                LetzteAktionAm = entry.LetzteAktionAm,
                LetzteAktionText = entry.LetzteAktionText,
                Empfänger = new Adresse
                {
                    Firmenname = entry.Kunde
                }
            };
        }
    }
}
