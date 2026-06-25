using System.Collections.ObjectModel;
using System.Windows;
using Örnek.Models;
using Örnek.Services;

namespace Örnek;

public partial class ArtikelAuswahlWindow : Window
{
    private readonly List<Artikel> _all;
    private readonly LocalizationService _localization = LocalizationService.Instance;
    public ObservableCollection<Artikel> Filtered { get; }

    public Artikel? SelectedArtikel => ArtikelGrid.SelectedItem as Artikel;

    public ArtikelAuswahlWindow(List<Artikel> artikel)
    {
        InitializeComponent();
        _all = artikel.Where(a => a.Aktiv).ToList();
        Filtered = new ObservableCollection<Artikel>(_all);
        ArtikelGrid.ItemsSource = Filtered;
        ApplyLocalization();

        if (Filtered.Count > 0)
            ArtikelGrid.SelectedIndex = 0;
    }

    private void ApplyLocalization()
    {
        Title = _localization["ArticleSelection.Title"];
        SearchLabel.Text = _localization["ArticleSelection.Search"];
        ArtikelNrColumn.Header = _localization["Catalog.Column.Number"];
        ArtikelNameColumn.Header = _localization["Catalog.Column.Name"];
        ArtikelUnitColumn.Header = _localization["Catalog.Column.Unit"];
        ArtikelPriceColumn.Header = _localization["Catalog.Column.Price"];
        ArtikelTaxColumn.Header = _localization["Catalog.Column.Tax"];
        AddButton.Content = _localization["ArticleSelection.Add"];
        CancelButton.Content = _localization["Common.Cancel"];
    }

    private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        var q = (SearchBox.Text ?? string.Empty).Trim();
        IEnumerable<Artikel> result;

        if (string.IsNullOrWhiteSpace(q))
        {
            result = _all;
        }
        else
        {
            result = _all.Where(a =>
                (a.ArtikelNr?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (a.Bezeichnung?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (a.Beschreibung?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        Filtered.Clear();
        foreach (var a in result)
            Filtered.Add(a);

        if (Filtered.Count > 0)
            ArtikelGrid.SelectedIndex = 0;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedArtikel is null)
            return;

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ArtikelGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        Ok_Click(sender, e);
    }
}
