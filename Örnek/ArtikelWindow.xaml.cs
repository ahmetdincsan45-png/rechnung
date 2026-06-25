using System.Collections.ObjectModel;
using System.Windows;
using Örnek.Models;
using Örnek.Services;

namespace Örnek;

public partial class ArtikelWindow : Window
{
    private readonly ArtikelService _service;
    private readonly LocalizationService _localization = LocalizationService.Instance;

    public ObservableCollection<Artikel> Artikel { get; }

    public Artikel? Selected { get; set; }

    public ArtikelWindow(ArtikelService service)
    {
        InitializeComponent();
        _service = service;
        Artikel = new ObservableCollection<Artikel>(_service.Load());
        ArtikelGrid.ItemsSource = Artikel;

        if (Artikel.Count > 0)
        {
            ArtikelGrid.SelectedIndex = 0;
            Selected = Artikel[0];
        }

        DataContext = this;
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        Title = _localization["Catalog.Title"];
        NewButton.Content = _localization["Common.New"];
        DeleteButton.Content = _localization["Common.Delete"];
        SaveButton.Content = _localization["Common.Save"];
        CloseButton.Content = _localization["Common.Close"];
        DetailGroupBox.Header = _localization["Catalog.Detail"];
        ArtikelNrColumn.Header = _localization["Catalog.Column.Number"];
        ArtikelNameColumn.Header = _localization["Catalog.Column.Name"];
        ArtikelUnitColumn.Header = _localization["Catalog.Column.Unit"];
        ArtikelPriceColumn.Header = _localization["Catalog.Column.Price"];
        ArtikelTaxColumn.Header = _localization["Catalog.Column.Tax"];
        ArtikelActiveColumn.Header = _localization["Catalog.Column.Active"];
        ArtikelNrLabel.Text = _localization["Catalog.Label.Number"];
        ArtikelNameLabel.Text = _localization["Catalog.Label.Name"];
        ArtikelUnitLabel.Text = _localization["Catalog.Label.Unit"];
        ArtikelPriceLabel.Text = _localization["Catalog.Label.Price"];
        ArtikelTaxLabel.Text = _localization["Catalog.Label.Tax"];
        ArtikelDescriptionLabel.Text = _localization["Catalog.Label.Description"];
    }

    private void ArtikelGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        Selected = ArtikelGrid.SelectedItem as Artikel;
        DataContext = null;
        DataContext = this;
    }

    private void Yeni_Click(object sender, RoutedEventArgs e)
    {
        var a = new Artikel
        {
            ArtikelNr = _service.SuggestNextArtikelNr(Artikel)
        };
        Artikel.Add(a);
        ArtikelGrid.SelectedItem = a;
    }

    private void Sil_Click(object sender, RoutedEventArgs e)
    {
        if (ArtikelGrid.SelectedItem is Artikel a)
            Artikel.Remove(a);
    }

    private void Kaydet_Click(object sender, RoutedEventArgs e)
    {
        _service.Save(Artikel.ToList());
        MessageBox.Show(_localization["Common.Saved"], _localization["Common.Info"], MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Kapat_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
