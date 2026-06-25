using System;
using System.IO;
using System.Windows;

namespace Örnek;

public partial class PdfPreviewWindow : Window
{
    private readonly byte[] _pdfBytes;
    private readonly string _suggestedFileName;
    private string? _tempPath;
    private readonly Services.LocalizationService _localization = Services.LocalizationService.Instance;

    public PdfPreviewWindow(byte[] pdfBytes, string suggestedFileName)
    {
        InitializeComponent();
        _pdfBytes = pdfBytes;
        _suggestedFileName = suggestedFileName;
        Title = _localization["PdfPreview.Title"];
        SaveButton.Content = _localization["Common.Save"];
        CloseButton.Content = _localization["Common.Close"];

        Loaded += PdfPreviewWindow_Loaded;
        Closed += PdfPreviewWindow_Closed;
    }

    private async void PdfPreviewWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await PdfView.EnsureCoreWebView2Async();

            _tempPath = Path.Combine(Path.GetTempPath(), $"pdf_preview_{Guid.NewGuid():N}.pdf");
            File.WriteAllBytes(_tempPath, _pdfBytes);

            PdfView.Source = new Uri(_tempPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                _localization.IsGerman ? $"Die PDF-Vorschau konnte nicht geöffnet werden: {ex.Message}" : $"PDF önizleme açılamadı: {ex.Message}",
                _localization["Common.Error"],
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void PdfPreviewWindow_Closed(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_tempPath))
            return;

        try
        {
            File.Delete(_tempPath);
        }
        catch
        {
            // ignore
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            FileName = _suggestedFileName,
            DefaultExt = ".pdf",
            Filter = "PDF (*.pdf)|*.pdf|Alle Dateien (*.*)|*.*"
        };

        if (dlg.ShowDialog() == true)
        {
            try
            {
                File.WriteAllBytes(dlg.FileName, _pdfBytes);
                MessageBox.Show(_localization.IsGerman ? "PDF wurde gespeichert." : "PDF kaydedildi.", _localization["Common.Info"], MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    _localization.IsGerman ? $"Fehler beim Speichern: {ex.Message}" : $"Kaydetme hatası: {ex.Message}",
                    _localization["Common.Error"],
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
