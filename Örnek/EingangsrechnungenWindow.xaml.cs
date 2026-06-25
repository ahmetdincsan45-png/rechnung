using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.Win32;
using Örnek.Models;
using Örnek.Services;

namespace Örnek;

public partial class EingangsrechnungenWindow : Window, INotifyPropertyChanged
{
    private sealed class ArchivedInvoiceOption
    {
        public string InvoiceNumber { get; init; } = string.Empty;
        public string DisplayText { get; init; } = string.Empty;
    }

    private readonly EingangsrechnungService _service;
    private readonly EingangsrechnungenScanService _scanService = new();
    private readonly LocalizationService _localization = LocalizationService.Instance;
    private readonly ArchivService _archivService = new(new ArchivSettingsService());
    private readonly ReceiptAnalysisService _analysisService = new();
    private readonly InvoiceMatchService _invoiceMatchService;
    private readonly string? _preselectedInvoiceNumber;
    private EingangsrechnungEintrag? _selected;
    private bool _isApplyingArchivedInvoiceSelection;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<EingangsrechnungEintrag> Entries { get; } = new();
    private ObservableCollection<ArchivedInvoiceOption> ArchivedInvoices { get; } = new();
    private ObservableCollection<InvoiceMatchSuggestion> SuggestedInvoices { get; } = new();

    public EingangsrechnungEintrag? Selected
    {
        get => _selected;
        set
        {
            if (ReferenceEquals(_selected, value))
                return;

            _selected = value;
            OnPropertyChanged();
        }
    }

    public EingangsrechnungenWindow(EingangsrechnungService service, string? preselectedInvoiceNumber = null)
    {
        InitializeComponent();
        _service = service;
        _invoiceMatchService = new InvoiceMatchService(_archivService);
        _preselectedInvoiceNumber = preselectedInvoiceNumber;
        DataContext = this;
        InvoicesGrid.ItemsSource = Entries;
        if (FindName("ArchivedInvoiceComboBox") is System.Windows.Controls.ComboBox archivedInvoiceComboBox)
            archivedInvoiceComboBox.ItemsSource = ArchivedInvoices;
        if (FindName("SuggestedInvoiceComboBox") is System.Windows.Controls.ComboBox suggestedInvoiceComboBox)
            suggestedInvoiceComboBox.ItemsSource = SuggestedInvoices;
        ApplyLocalization();
        LoadArchivedInvoices();
        LoadData();
    }

    private void ApplyLocalization()
    {
        _analysisService.EnsureOcrDataFolder();

        Title = _localization["IncomingInvoices.Title"];
        ImportButton.Content = _localization["IncomingInvoices.Import"];
        ScanButton.Content = _localization["IncomingInvoices.Scan"];
        AnalyzeButton.Content = _localization["IncomingInvoices.Analyze"];
        if (FindName("OpenOcrFolderButton") is System.Windows.Controls.Button openOcrFolderButton)
            openOcrFolderButton.Content = _localization["IncomingInvoices.OpenOcrFolder"];
        SaveButton.Content = _localization["Common.Save"];
        DeleteButton.Content = _localization["Common.Delete"];
        OpenButton.Content = _localization["IncomingInvoices.OpenDocument"];
        OpenFolderButton.Content = _localization["Archive.OpenFolder"];
        RefreshButton.Content = _localization["Archive.Refresh"];

        ListGroupBox.Header = _localization["IncomingInvoices.ListTitle"];
        DetailsGroupBox.Header = _localization["IncomingInvoices.DetailsTitle"];
        SupplierColumn.Header = _localization["IncomingInvoices.Column.Supplier"];
        InvoiceNumberColumn.Header = _localization["IncomingInvoices.Column.Number"];
        InvoiceDateColumn.Header = _localization["IncomingInvoices.Column.Date"];
        GrossColumn.Header = _localization["IncomingInvoices.Column.Gross"];
        ProjectColumn.Header = _localization["IncomingInvoices.Column.Project"];

        SupplierLabel.Text = _localization["IncomingInvoices.Label.Supplier"];
        InvoiceNumberLabel.Text = _localization["IncomingInvoices.Label.Number"];
        InvoiceDateLabel.Text = _localization["IncomingInvoices.Label.Date"];
        NetLabel.Text = _localization["IncomingInvoices.Label.Net"];
        TaxLabel.Text = _localization["IncomingInvoices.Label.Tax"];
        GrossLabel.Text = _localization["IncomingInvoices.Label.Gross"];
        ProjectLabel.Text = _localization["IncomingInvoices.Label.Project"];
        CategoryLabel.Text = _localization["IncomingInvoices.Label.Category"];
        if (FindName("ArchivedInvoiceSelectLabel") is System.Windows.Controls.TextBlock archivedInvoiceSelectLabel)
            archivedInvoiceSelectLabel.Text = _localization["IncomingInvoices.Label.ArchivedInvoiceSelect"];
        AssignedInvoiceLabel.Text = _localization["IncomingInvoices.Label.AssignedInvoice"];
        if (FindName("SuggestedInvoiceLabel") is System.Windows.Controls.TextBlock suggestedInvoiceLabel)
            suggestedInvoiceLabel.Text = _localization["IncomingInvoices.Label.SuggestedInvoice"];
        if (FindName("DetectedSupplierLabel") is System.Windows.Controls.TextBlock detectedSupplierLabel)
            detectedSupplierLabel.Text = _localization["IncomingInvoices.Label.DetectedSupplier"];
        if (FindName("DetectedInvoiceNumberLabel") is System.Windows.Controls.TextBlock detectedInvoiceNumberLabel)
            detectedInvoiceNumberLabel.Text = _localization["IncomingInvoices.Label.DetectedInvoiceNumber"];
        if (FindName("DetectedOcrTextLabel") is System.Windows.Controls.TextBlock detectedOcrTextLabel)
            detectedOcrTextLabel.Text = _localization["IncomingInvoices.Label.OcrText"];
        if (FindName("OcrStatusTextBlock") is System.Windows.Controls.TextBlock ocrStatusTextBlock)
            ocrStatusTextBlock.Text = _analysisService.HasOcrLanguageData()
                ? _localization["IncomingInvoices.OcrReady"]
                : string.Format(_localization["IncomingInvoices.OcrMissingData"], _analysisService.GetOcrDataFolder());
        OriginalFileLabel.Text = _localization["IncomingInvoices.Label.OriginalFile"];
        NoteLabel.Text = _localization["IncomingInvoices.Label.Note"];
    }

    private void LoadArchivedInvoices()
    {
        ArchivedInvoices.Clear();

        foreach (var item in _archivService.ListeLaden()
                     .Where(x => x.DokumentTyp == DokumentTyp.Rechnung && !string.IsNullOrWhiteSpace(x.DokumentNummer))
                     .OrderByDescending(x => x.Datum))
        {
            ArchivedInvoices.Add(new ArchivedInvoiceOption
            {
                InvoiceNumber = item.DokumentNummer,
                DisplayText = $"{item.DokumentNummer} | {item.Kunde} | {item.Datum:dd.MM.yyyy}"
            });
        }
    }

    private void LoadData()
    {
        Entries.Clear();
        foreach (var entry in _service.LoadAll())
            Entries.Add(entry);

        Selected = Entries.FirstOrDefault();
        InvoicesGrid.SelectedItem = Selected;
        RefreshAnalysisUi();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => LoadData();

    private async void Import_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Dokumente (*.pdf;*.jpg;*.jpeg;*.png)|*.pdf;*.jpg;*.jpeg;*.png|Alle Dateien (*.*)|*.*",
            Multiselect = false,
            Title = _localization["IncomingInvoices.ImportDialogTitle"]
        };

        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            var entry = _service.ImportDocument(dialog.FileName);
            await AnalyzeEntryAsync(entry);
            ApplyPreselectedInvoice(entry);
            Entries.Insert(0, entry);
            Selected = entry;
            InvoicesGrid.SelectedItem = entry;
            MessageBox.Show(_localization["IncomingInvoices.ImportSuccess"], _localization["Common.Info"], MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(string.Format(_localization["IncomingInvoices.ImportFailed"], ex.Message), _localization["Common.Error"], MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Scan_Click(object sender, RoutedEventArgs e)
    {
        string? tempFile = null;

        try
        {
            System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            tempFile = _scanService.AcquireScanToTempFile();
            var visibleName = $"scan_{DateTime.Now:yyyyMMdd_HHmmss}{System.IO.Path.GetExtension(tempFile)}";
            var entry = _service.ImportDocument(tempFile, visibleName);
            await AnalyzeEntryAsync(entry);
            ApplyPreselectedInvoice(entry);

            Entries.Insert(0, entry);
            Selected = entry;
            InvoicesGrid.SelectedItem = entry;

            MessageBox.Show(_localization["IncomingInvoices.ScanSuccess"], _localization["Common.Info"], MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (OperationCanceledException)
        {
            MessageBox.Show(_localization["IncomingInvoices.ScanCanceled"], _localization["Common.Info"], MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(string.Format(_localization["IncomingInvoices.ScanFailed"], ex.Message), _localization["Common.Error"], MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            System.Windows.Input.Mouse.OverrideCursor = null;

            if (!string.IsNullOrWhiteSpace(tempFile) && System.IO.File.Exists(tempFile))
            {
                try
                {
                    System.IO.File.Delete(tempFile);
                }
                catch
                {
                    // ignore temp cleanup issues
                }
            }
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (Selected == null)
            return;

        try
        {
            _service.Save(Selected);
            MessageBox.Show(_localization["Common.Saved"], _localization["Common.Info"], MessageBoxButton.OK, MessageBoxImage.Information);
            LoadData();
        }
        catch (Exception ex)
        {
            MessageBox.Show(string.Format(_localization["IncomingInvoices.SaveFailed"], ex.Message), _localization["Common.Error"], MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (Selected == null)
            return;

        var result = MessageBox.Show(
            _localization["IncomingInvoices.DeleteConfirm"],
            _localization["Archive.DeleteConfirmTitle"],
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        if (!_service.Delete(Selected))
        {
            MessageBox.Show(_localization["IncomingInvoices.DeleteFailed"], _localization["Common.Error"], MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        LoadData();
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        if (Selected == null)
            return;

        try
        {
            _service.OpenDocument(Selected);
        }
        catch (Exception ex)
        {
            MessageBox.Show(string.Format(_localization["IncomingInvoices.OpenFailed"], ex.Message), _localization["Common.Error"], MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (Selected == null)
            return;

        try
        {
            _service.OpenFolder(Selected);
        }
        catch (Exception ex)
        {
            MessageBox.Show(string.Format(_localization["IncomingInvoices.OpenFolderFailed"], ex.Message), _localization["Common.Error"], MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void InvoicesGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        Selected = InvoicesGrid.SelectedItem as EingangsrechnungEintrag;

        if (Selected != null && string.IsNullOrWhiteSpace(Selected.ZugeordneteRechnungNummer))
            ApplyPreselectedInvoice(Selected);

        SyncArchivedInvoiceSelection();
        RefreshAnalysisUi();
    }

    private void ArchivedInvoiceComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_isApplyingArchivedInvoiceSelection || Selected == null)
            return;

        if (FindName("ArchivedInvoiceComboBox") is System.Windows.Controls.ComboBox archivedInvoiceComboBox &&
            archivedInvoiceComboBox.SelectedItem is ArchivedInvoiceOption option)
            Selected.ZugeordneteRechnungNummer = option.InvoiceNumber;
    }

    private void ApplyPreselectedInvoice(EingangsrechnungEintrag entry)
    {
        if (string.IsNullOrWhiteSpace(_preselectedInvoiceNumber) || !string.IsNullOrWhiteSpace(entry.ZugeordneteRechnungNummer))
            return;

        entry.ZugeordneteRechnungNummer = _preselectedInvoiceNumber;
        _service.Save(entry);
    }

    private void SyncArchivedInvoiceSelection()
    {
        if (FindName("ArchivedInvoiceComboBox") is not System.Windows.Controls.ComboBox archivedInvoiceComboBox)
            return;

        _isApplyingArchivedInvoiceSelection = true;

        try
        {
            if (Selected == null || string.IsNullOrWhiteSpace(Selected.ZugeordneteRechnungNummer))
            {
                archivedInvoiceComboBox.SelectedItem = null;
                return;
            }

            archivedInvoiceComboBox.SelectedItem = ArchivedInvoices
                .FirstOrDefault(x => string.Equals(x.InvoiceNumber, Selected.ZugeordneteRechnungNummer, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            _isApplyingArchivedInvoiceSelection = false;
        }
    }

    private void SuggestedInvoiceComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (Selected == null)
            return;

        if (FindName("SuggestedInvoiceComboBox") is System.Windows.Controls.ComboBox suggestedInvoiceComboBox &&
            suggestedInvoiceComboBox.SelectedItem is InvoiceMatchSuggestion suggestion)
        {
            Selected.ZugeordneteRechnungNummer = suggestion.InvoiceNumber;
            SyncArchivedInvoiceSelection();
        }
    }

    private async Task AnalyzeEntryAsync(EingangsrechnungEintrag entry)
    {
        var result = await _analysisService.AnalyzeAsync(entry);

        entry.OcrText = result.RawText;
        entry.OcrVerarbeitet = !string.IsNullOrWhiteSpace(result.RawText);

        if (string.IsNullOrWhiteSpace(entry.ErkannterLieferant))
            entry.ErkannterLieferant = result.Supplier;

        if (string.IsNullOrWhiteSpace(entry.ErkannteRechnungsnummer))
            entry.ErkannteRechnungsnummer = result.InvoiceNumber;

        if (string.IsNullOrWhiteSpace(entry.Lieferant) && !string.IsNullOrWhiteSpace(result.Supplier))
            entry.Lieferant = result.Supplier;

        if (string.IsNullOrWhiteSpace(entry.Rechnungsnummer) && !string.IsNullOrWhiteSpace(result.InvoiceNumber))
            entry.Rechnungsnummer = result.InvoiceNumber;

        if (entry.Rechnungsdatum <= DateTime.MinValue || entry.Rechnungsdatum == DateTime.Today)
        {
            var detectedDate = _analysisService.ExtractDate(result.RawText);
            if (detectedDate.HasValue)
                entry.Rechnungsdatum = detectedDate.Value;
        }

        if (entry.Brutto <= 0)
        {
            var gross = _analysisService.ExtractAmount(result.RawText, "brutto", "gesamt", "total");
            if (gross.HasValue)
                entry.Brutto = gross.Value;
        }

        if (entry.Netto <= 0)
        {
            var net = _analysisService.ExtractAmount(result.RawText, "netto", "net");
            if (net.HasValue)
                entry.Netto = net.Value;
        }

        if (entry.MwSt <= 0)
        {
            var tax = _analysisService.ExtractAmount(result.RawText, "mwst", "ust", "kdv", "vat", "steuer");
            if (tax.HasValue)
                entry.MwSt = tax.Value;
        }

        if (string.IsNullOrWhiteSpace(entry.Kategorie))
        {
            var category = _analysisService.ExtractCategory(result.RawText);
            if (!string.IsNullOrWhiteSpace(category))
                entry.Kategorie = category;
        }

        if (string.IsNullOrWhiteSpace(entry.Projekt))
        {
            var project = _analysisService.ExtractProject(result.RawText);
            if (!string.IsNullOrWhiteSpace(project))
                entry.Projekt = project;
        }

        _service.Save(entry);
    }

    private void OpenOcrFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _analysisService.EnsureOcrDataFolder();
            var path = _analysisService.GetOcrDataFolder();
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(string.Format(_localization["IncomingInvoices.OpenOcrFolderFailed"], ex.Message), _localization["Common.Error"], MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Analyze_Click(object sender, RoutedEventArgs e)
    {
        if (Selected == null)
            return;

        try
        {
            System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            Selected.OcrText = string.Empty;
            Selected.ErkannterLieferant = string.Empty;
            Selected.ErkannteRechnungsnummer = string.Empty;
            await AnalyzeEntryAsync(Selected);
            RefreshAnalysisUiWithoutReanalyze();

            MessageBox.Show(_localization["IncomingInvoices.AnalyzeSuccess"], _localization["Common.Info"], MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(string.Format(_localization["IncomingInvoices.AnalyzeFailed"], ex.Message), _localization["Common.Error"], MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            System.Windows.Input.Mouse.OverrideCursor = null;
        }
    }

    private void RefreshAnalysisUi()
    {
        SuggestedInvoices.Clear();

        if (Selected == null)
            return;

        _ = RefreshAnalysisUiAsync();
    }

    private async Task RefreshAnalysisUiAsync()
    {
        if (Selected == null)
            return;

        await AnalyzeEntryAsync(Selected);
        RefreshAnalysisUiWithoutReanalyze();
    }

    private void RefreshAnalysisUiWithoutReanalyze()
    {
        SuggestedInvoices.Clear();

        if (Selected == null)
            return;

        foreach (var suggestion in _invoiceMatchService.Suggest(Selected))
            SuggestedInvoices.Add(suggestion);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
