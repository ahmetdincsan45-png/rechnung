using System.Windows;
using Örnek.Models;
using Örnek.Services;

namespace Örnek;

public partial class AngebotMetinleriWindow : Window
{
    private readonly FirmaProfilService _service;
    private readonly LocalizationService _localization = LocalizationService.Instance;

    public FirmaProfil Profil { get; }

    public AngebotMetinleriWindow(FirmaProfil profil, FirmaProfilService service)
    {
        InitializeComponent();
        _service = service;
        Profil = profil;
        DataContext = Profil;
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        Title = _localization["OfferTexts.Title"];
        HeaderText.Text = _localization["OfferTexts.Header"];
        IntroLabel.Text = _localization["OfferTexts.Intro"];
        LiabilityLabel.Text = _localization["OfferTexts.Liability"];
        OrderConfirmationLabel.Text = _localization["OfferTexts.OrderConfirmation"];
        CancellationLabel.Text = _localization["OfferTexts.Cancellation"];
        InfoText.Text = _localization["OfferTexts.Note"];
        CancelButton.Content = _localization["Common.Cancel"];
        SaveButton.Content = _localization["Common.Save"];
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
