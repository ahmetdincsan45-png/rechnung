using System.Text;
using System.Windows;
using Örnek.Models;
using Örnek.Services;
using System.IO;
using System.Windows.Interop;
using System.Globalization;
using System.Windows.Controls;
using System.Linq;
using System.Collections.ObjectModel;

namespace Örnek
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Rechnung _rechnung;
        private RechnungService _service;
        private FirmaProfilService _firmaProfilService;
        private FirmaProfil _firmaProfil;
        private KundenService _kundenService;
        private ArtikelService _artikelService;
        private ArchivSettingsService _archivSettingsService;
        private ArchivService _archivService;
        private EingangsrechnungService _eingangsrechnungService;
        private EmailSettingsService _emailSettingsService;
        private EmailService _emailService;
        private NummerService _nummerService;
        private List<Kunde> _kunden = new();
        private string _lastPlaintext = string.Empty;
        private const string ProfessionellerRechnungstext = "Sehr geehrte Damen und Herren,\n\nfür die erbrachten Leistungen erlauben wir uns, Ihnen wie folgt in Rechnung zu stellen.";
        private bool _isSidebarExpanded;
        private Rechnungsposition? _selectedPosition;
        private readonly ObservableCollection<EingangsrechnungEintrag> _linkedReceipts = new();
        private readonly LocalizationService _localization = LocalizationService.Instance;
        private bool _isApplyingLanguage;

        public MainWindow()
        {
            InitializeComponent();
            _service = new RechnungService();
            _firmaProfilService = new FirmaProfilService();
            _firmaProfil = _firmaProfilService.LoadOrDefault();

            _kundenService = new KundenService();
            _artikelService = new ArtikelService();
            _archivSettingsService = new ArchivSettingsService();
            _archivService = new ArchivService(_archivSettingsService);
            _eingangsrechnungService = new EingangsrechnungService(_archivSettingsService);
            _emailSettingsService = new EmailSettingsService();
            _emailService = new EmailService();
            _nummerService = new NummerService();

            ForceNumberBaselineOnce();

            // Ensure numbering starts from 1002 on first use (or after older installs).
            _nummerService.EnsureAtLeast(DokumentTyp.Angebot, 1001);
            _nummerService.EnsureAtLeast(DokumentTyp.Rechnung, 1001);

            _localization.LanguageChanged += Localization_LanguageChanged;

            LoadKunden();
            StartEmptyDocument(DokumentTyp.Rechnung);
            ApplyLanguageToSidebarSelector();
        }

        protected override void OnClosed(EventArgs e)
        {
            _localization.LanguageChanged -= Localization_LanguageChanged;
            base.OnClosed(e);
        }

        private void Localization_LanguageChanged(object? sender, EventArgs e)
        {
            ApplyLanguageToSidebarSelector();
            ApplyMainWindowLocalization();
        }

        private void ApplyLanguageToSidebarSelector()
        {
            if (LanguageComboBox == null || LanguageSectionText == null || LanguageLabelText == null)
                return;

            _isApplyingLanguage = true;

            LanguageSectionText.Text = _localization["Language.Section"];
            LanguageLabelText.Text = _localization["Language.Label"];

            if (LanguageComboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(i => Equals(i.Tag, "de")) is { } de)
                de.Content = _localization["Language.German"];

            if (LanguageComboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(i => Equals(i.Tag, "tr")) is { } tr)
                tr.Content = _localization["Language.Turkish"];

            LanguageComboBox.SelectedIndex = _localization.CurrentLanguage == UiLanguage.Deutsch ? 0 : 1;
            _isApplyingLanguage = false;
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isApplyingLanguage || LanguageComboBox?.SelectedItem is not ComboBoxItem item)
                return;

            _localization.SetLanguage(Equals(item.Tag, "tr") ? UiLanguage.Turkce : UiLanguage.Deutsch);
        }

        private void ApplyMainWindowLocalization()
        {
            if (AppSubtitleText == null)
                return;

            AppSubtitleText.Text = _localization["Main.AppSubtitle"];
            RechnungButton.Content = _localization["Main.Action.Invoice"];
            ExportButton.Content = _localization["Main.Action.Save"];
            AngebotButton.Content = _localization["Main.Action.Offer"];

            SidebarWorkspaceText.Text = _localization["Main.Sidebar.Workspace"];
            SidebarWorkspaceSubtitleText.Text = _localization["Main.Sidebar.WorkspaceSubtitle"];
            SidebarActiveDocumentText.Text = _localization["Main.Sidebar.ActiveDocument"];
            SidebarQuickActionsText.Text = _localization["Main.Sidebar.QuickActions"];
            SidebarTextsSectionText.Text = _localization["Main.Sidebar.Texts"];
            SidebarManagementSectionText.Text = _localization["Main.Sidebar.Management"];
            SidebarMoreSectionText.Text = _localization["Main.Sidebar.More"];

            SidebarNewOfferText.Text = _localization["Main.Sidebar.NewOffer"];
            SidebarNewInvoiceText.Text = _localization["Main.Sidebar.NewInvoice"];
            SidebarConvertOfferText.Text = _localization["Main.Sidebar.ConvertOffer"];
            SidebarSendEmailText.Text = _localization["Main.Sidebar.SendEmail"];
            SidebarOpenArchiveText.Text = _localization["Main.Sidebar.OpenArchive"];
            SidebarProfessionellerRechnungstextCheckBox.Content = _localization["Main.Sidebar.ProfessionalInvoiceText"];
            SidebarOfferTextsText.Text = _localization["Main.Sidebar.OfferTexts"];
            SidebarCompanyDataText.Text = _localization["Main.Sidebar.CompanyData"];
            SidebarCatalogText.Text = _localization["Main.Sidebar.Catalog"];
            SidebarCustomersText.Text = _localization["Main.Sidebar.Customers"];
            if (FindName("SidebarIncomingInvoicesText") is TextBlock incomingInvoicesText)
                incomingInvoicesText.Text = _localization["Main.Sidebar.IncomingInvoices"];
            SidebarEmailSettingsText.Text = _localization["Main.Sidebar.EmailSettings"];
            if (FindName("SidebarFinanceDashboardText") is TextBlock financeDashboardText)
                financeDashboardText.Text = _localization["Main.Sidebar.FinanceDashboard"];
            SidebarSaveFolderText.Text = _localization["Main.Sidebar.SaveFolder"];
            SidebarArchiveFolderText.Text = _localization["Main.Sidebar.ArchiveFolder"];
            SidebarExitText.Text = _localization["Main.Sidebar.Exit"];
            if (FindName("RightSidebarActionsTitleText") is TextBlock rightSidebarActionsTitleText)
                rightSidebarActionsTitleText.Text = _localization["Main.RightSidebar.Title"];
            if (FindName("RightSidebarActionsSubtitleText") is TextBlock rightSidebarActionsSubtitleText)
                rightSidebarActionsSubtitleText.Text = _localization["Main.RightSidebar.Subtitle"];

            SmallBusinessCheckBox.Content = _localization["Main.Check.SmallBusiness"];
            ObjektLabel.Text = _localization["Main.Label.Object"];
            LieferzeitLabel.Text = _localization["Main.Label.DeliveryTime"];
            RecipientGroupBox.Header = _localization["Main.Group.Recipient"];
            CustomerLabel.Text = _localization["Main.Label.Customer"];
            NameCompanyLabel.Text = _localization["Main.Label.NameCompany"];
            StreetLabel.Text = _localization["Main.Label.Street"];
            HouseNumberLabel.Text = _localization["Main.Label.HouseNumber"];
            PostalCodeLabel.Text = _localization["Main.Label.PostalCode"];
            CityLabel.Text = _localization["Main.Label.City"];
            RecipientEmailLabel.Text = _localization["Main.Label.Email"];
            LinkedReceiptsGroupBox.Header = _localization["Main.LinkedReceipts.Title"];
            AssignReceiptButton.Content = _localization["Main.LinkedReceipts.AssignButton"];
            if (FindName("OpenLinkedReceiptButton") is Button openLinkedReceiptButton)
                openLinkedReceiptButton.Content = _localization["Main.LinkedReceipts.OpenButton"];
            if (FindName("RemoveLinkedReceiptButton") is Button removeLinkedReceiptButton)
                removeLinkedReceiptButton.Content = _localization["Main.LinkedReceipts.RemoveButton"];
            LinkedReceiptSupplierColumn.Header = _localization["IncomingInvoices.Column.Supplier"];
            LinkedReceiptDateColumn.Header = _localization["IncomingInvoices.Column.Date"];
            LinkedReceiptGrossColumn.Header = _localization["IncomingInvoices.Column.Gross"];
            LinkedReceiptNumberColumn.Header = _localization["IncomingInvoices.Column.Number"];
            LinkedReceiptsInfoText.Text = _localization["Main.LinkedReceipts.Info"];

            PositionsGroupBox.Header = _localization["Main.Group.Positions"];
            AddFromCatalogButton.Content = _localization["Main.Button.AddFromCatalog"];
            AddPositionButton.Content = _localization["Main.Button.AddPosition"];
            RemovePositionButton.Content = _localization["Main.Button.RemoveSelection"];
            AciklamaEkleButton.Content = _localization["Main.Button.AddNote"];
            AciklamaSilButton.Content = _localization["Main.Button.RemoveNote"];
            PositionsTitleText.Text = _localization["Main.Positions.Title"];
            PositionsInfoText.Text = _localization["Main.Positions.Info"];
            TotalsTitleText.Text = _localization["Main.Totals.Title"];

            SetDokumentTyp(_rechnung?.DokumentTyp ?? DokumentTyp.Rechnung);
        }

        private void ForceNumberBaselineOnce()
        {
            try
            {
                var appFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Rechnung");
                var marker = Path.Combine(appFolder, "nummer-baseline-1002.applied");
                if (File.Exists(marker))
                    return;

                Directory.CreateDirectory(appFolder);

                // Force counters so the next number becomes 1002.
                _nummerService.ForceBaseline(DokumentTyp.Angebot, 1001);
                _nummerService.ForceBaseline(DokumentTyp.Rechnung, 1001);

                File.WriteAllText(marker, DateTime.Now.ToString("O"));
            }
            catch
            {
                // ignore
            }
        }

        private void StartEmptyDocument(DokumentTyp typ)
        {
            var now = DateTime.Now;
            _rechnung = new Rechnung
            {
                DokumentTyp = typ,
                Status = DokumentStatus.Entwurf,
                LetzteAktionAm = now,
                LetzteAktionText = _localization.IsGerman ? "Neues Dokument erstellt" : "Yeni belge oluşturuldu",
                Rechnungsnummer = string.Empty,
                Empfänger = new Adresse(),
                Rechnungsdatum = now,
                Leistungsdatum = typ == DokumentTyp.Rechnung ? now : null,
                Fälligkeitsdatum = null,
                GueltigBis = typ == DokumentTyp.Angebot ? now.Date.AddDays(14) : null,
                Lieferzeit = typ == DokumentTyp.Angebot ? "" : null
            };

            InitializeCompanyDefaults();
            _rechnung.Positionen.Clear();
            DataContext = _rechnung;
            LinkedReceiptsGrid.ItemsSource = _linkedReceipts;
            _lastPlaintext = string.Empty;
            _selectedPosition = null;

            SetDokumentTyp(typ);
            LoadKunden();
            DisplayRechnung();
        }


        private void Artikelstamm_Click(object sender, RoutedEventArgs e)
        {
            var w = new ArtikelWindow(_artikelService) { Owner = this };
            w.ShowDialog();
        }

        private void SetDokumentTyp(DokumentTyp typ)
        {
            _rechnung.DokumentTyp = typ;

            var isAngebot = typ == DokumentTyp.Angebot;

            if (SidebarProfessionellerRechnungstextCheckBox != null)
                SidebarProfessionellerRechnungstextCheckBox.IsChecked = !isAngebot && _rechnung.ProfessionellerRechnungstextAktiv;

            // Important: don't consume a new sequence number just by switching UI mode.
            // Numbers should advance when creating a new document or when saving/archiving.
            DokumentGroupBox.Header = isAngebot ? _localization["Main.Doc.Offer"] : _localization["Main.Doc.Invoice"];
            GenerierenButton.Content = isAngebot ? _localization["Main.Action.GenerateOffer"] : _localization["Main.Action.GenerateInvoice"];
            AppTitleText.Text = isAngebot
                ? _localization["Main.Title.OfferApp"]
                : _localization["Main.Title.InvoiceApp"];

            Title = isAngebot
                ? _localization["Main.Title.OfferApp"]
                : _localization["Main.Title.InvoiceApp"];

            NummerLabel.Text = isAngebot ? _localization["Main.Label.OfferNumber"] : _localization["Main.Label.InvoiceNumber"];
            DatumLabel.Text = isAngebot ? _localization["Main.Label.OfferDate"] : _localization["Main.Label.InvoiceDate"];
            LeistungsdatumLabel.Text = _localization["Main.Label.ServiceDate"];
            LeistungsdatumLabel.Visibility = isAngebot ? Visibility.Collapsed : Visibility.Visible;
            LeistungsdatumDatePicker.Visibility = isAngebot ? Visibility.Collapsed : Visibility.Visible;

            if (isAngebot)
            {
                LieferzeitPanel.Visibility = Visibility.Visible;

                _rechnung.Lieferzeit ??= "";
            }
            else
            {
                LieferzeitPanel.Visibility = Visibility.Collapsed;
            }

            // Normalize number prefix to match the selected document type
            // (e.g. if the user manually changed it or it came from older data).
            var desiredPrefix = isAngebot ? "AN-" : "RE-";
            if (!string.IsNullOrWhiteSpace(_rechnung.Rechnungsnummer) &&
                !_rechnung.Rechnungsnummer.StartsWith(desiredPrefix, StringComparison.OrdinalIgnoreCase))
            {
                if (NummerService.TryParseSeq(DokumentTyp.Angebot, _rechnung.Rechnungsnummer, out var seq) ||
                    NummerService.TryParseSeq(DokumentTyp.Rechnung, _rechnung.Rechnungsnummer, out seq))
                {
                    _rechnung.Rechnungsnummer = NummerService.Build(isAngebot ? DokumentTyp.Angebot : DokumentTyp.Rechnung, _rechnung.Rechnungsdatum, seq);
                }
                else if (_rechnung.Rechnungsnummer.StartsWith("AN-", StringComparison.OrdinalIgnoreCase) ||
                         _rechnung.Rechnungsnummer.StartsWith("RE-", StringComparison.OrdinalIgnoreCase))
                {
                    _rechnung.Rechnungsnummer = desiredPrefix + _rechnung.Rechnungsnummer[3..];
                }
            }

            // refresh bindings because Rechnung does not implement INotifyPropertyChanged
            DataContext = null;
            DataContext = _rechnung;

            LoadKunden();
            RefreshKundenSelection();

            EnsurePreviewNumber();
        }

        private void EnsurePreviewNumber()
        {
            if (_rechnung is null)
                return;

            if (_rechnung.DokumentTyp == DokumentTyp.Angebot &&
                NummerService.TryParseSeq(DokumentTyp.Rechnung, _rechnung.Rechnungsnummer, out var legacySeq))
            {
                _rechnung.Rechnungsnummer = NummerService.Build(DokumentTyp.Angebot, _rechnung.Rechnungsdatum, legacySeq);
            }

            // Show a number in the UI without consuming it. The real counter advances only on save.
            if (string.IsNullOrWhiteSpace(_rechnung.Rechnungsnummer))
            {
                _rechnung.Rechnungsnummer = _nummerService.PreviewNext(_rechnung.DokumentTyp, _rechnung.Rechnungsdatum);
                DataContext = null;
                DataContext = _rechnung;
            }
        }

        private void StartNewAngebot()
        {
            var now = DateTime.Now;
            _rechnung = new Rechnung
            {
                DokumentTyp = DokumentTyp.Angebot,
                Status = DokumentStatus.Entwurf,
                LetzteAktionAm = now,
                LetzteAktionText = _localization.IsGerman ? "Neues Angebot erstellt" : "Yeni teklif oluşturuldu",
                Rechnungsnummer = string.Empty,
                Empfänger = new Adresse(),
                Rechnungsdatum = now,
                GueltigBis = DateTime.Now.Date.AddDays(14)
            };

            InitializeCompanyDefaults();
            _rechnung.Positionen.Clear();
            DataContext = _rechnung;
            _lastPlaintext = string.Empty;
            _selectedPosition = null;

            SetDokumentTyp(DokumentTyp.Angebot);
            LoadKunden();
            DisplayRechnung();
        }

        private void AciklamaEkleButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPosition is Rechnungsposition pos)
            {
                if (string.IsNullOrWhiteSpace(pos.Notiz))
                    pos.Notiz = string.Empty;
                return;
            }

            var w = new NotizWindow(_rechnung.Notizen) { Owner = this };
            if (w.ShowDialog() == true)
            {
                _rechnung.Notizen = string.IsNullOrWhiteSpace(w.NotizText) ? null : w.NotizText;
                DisplayRechnung();
            }
        }

        private void AciklamaSilButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPosition is Rechnungsposition pos)
            {
                pos.Notiz = null;
                DisplayRechnung();
                return;
            }

            _rechnung.Notizen = null;
            DisplayRechnung();
        }

        private void InitializeCompanyDefaults()
        {
            _rechnung.Absender = new Adresse
            {
                Firmenname = _firmaProfil.Adresse.Firmenname,
                Strasse = _firmaProfil.Adresse.Strasse,
                Hausnummer = _firmaProfil.Adresse.Hausnummer,
                Postleitzahl = _firmaProfil.Adresse.Postleitzahl,
                Stadt = _firmaProfil.Adresse.Stadt,
                Land = _firmaProfil.Adresse.Land,
                Telefon = _firmaProfil.Adresse.Telefon,
                Email = _firmaProfil.Adresse.Email,
                Webseite = _firmaProfil.Adresse.Webseite
            };

            _rechnung.Steuernummer = _firmaProfil.Steuernummer;
            _rechnung.UstIdNr = _firmaProfil.UstIdNr;
            _rechnung.Zahlungsbedingungen = _firmaProfil.Zahlungsbedingungen;
            _rechnung.Angebotsbedingungen = _firmaProfil.Angebotsbedingungen;
            _rechnung.AngebotEinleitungText = _firmaProfil.AngebotEinleitungText;
            _rechnung.AngebotHaftungText = _firmaProfil.AngebotHaftungText;
            _rechnung.AngebotAuftragText = _firmaProfil.AngebotAuftragText;
            _rechnung.AngebotWiderrufText = _firmaProfil.AngebotWiderrufText;
            _rechnung.Kontoinhaber = _firmaProfil.Kontoinhaber;
            _rechnung.IBAN = _firmaProfil.IBAN;
            _rechnung.BIC = _firmaProfil.BIC;
            _rechnung.AbsenderLogoPath = _firmaProfil.LogoPath;

            // keep legacy field in sync (some templates might still use it)
            if (string.IsNullOrWhiteSpace(_rechnung.Bankverbindung) &&
                (!string.IsNullOrWhiteSpace(_rechnung.Kontoinhaber) || !string.IsNullOrWhiteSpace(_rechnung.IBAN) || !string.IsNullOrWhiteSpace(_rechnung.BIC)))
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(_rechnung.Kontoinhaber))
                    parts.Add($"Kontoinhaber: {_rechnung.Kontoinhaber}");
                if (!string.IsNullOrWhiteSpace(_rechnung.IBAN))
                    parts.Add($"IBAN: {_rechnung.IBAN}");
                if (!string.IsNullOrWhiteSpace(_rechnung.BIC))
                    parts.Add($"BIC: {_rechnung.BIC}");
                _rechnung.Bankverbindung = string.Join("\n", parts);
            }

            _rechnung.Rechnungsdatum = DateTime.Now;
            _rechnung.Leistungsdatum = DateTime.Now.AddDays(-7);
            _rechnung.Fälligkeitsdatum = null;
        }

        private void LoadKunden()
        {
            _kunden = _kundenService.Load()
                .OrderBy(k => k.Adresse.Firmenname)
                .ToList();

            if (KundeComboBox != null)
            {
                KundeComboBox.ItemsSource = _kunden;
                KundeComboBox.DisplayMemberPath = "Name";
                KundeComboBox.SelectedIndex = -1;
            }
        }

        public void LoadFromArchive(Rechnung rechnung)
        {
            _rechnung = rechnung;
            _selectedPosition = null;

            if (string.IsNullOrWhiteSpace(_rechnung.SavePdfPath) && !string.IsNullOrWhiteSpace(_rechnung.Rechnungsnummer))
            {
                var settings = _archivSettingsService.LoadOrDefault();
                if (!string.IsNullOrWhiteSpace(settings.DefaultSaveOrdner))
                {
                    _rechnung.SavePdfPath = Path.Combine(settings.DefaultSaveOrdner, $"{_rechnung.Rechnungsnummer}.pdf");
                    _rechnung.SaveJsonPath = Path.ChangeExtension(_rechnung.SavePdfPath, ".json");
                }
            }

            DataContext = null;
            DataContext = _rechnung;
            RefreshLinkedReceipts();

            SetDokumentTyp(_rechnung.DokumentTyp);
            InitializeCompanyDefaults();
            LoadKunden();
            DisplayRechnung();
        }

        private void StartNewInvoice()
        {
            var now = DateTime.Now;
            _rechnung = new Rechnung
            {
                Status = DokumentStatus.Entwurf,
                LetzteAktionAm = now,
                LetzteAktionText = _localization.IsGerman ? "Neue Rechnung erstellt" : "Yeni fatura oluşturuldu",
                Rechnungsnummer = string.Empty,
                Empfänger = new Adresse(),
                Rechnungsdatum = now,
                Leistungsdatum = now,
                Fälligkeitsdatum = null
            };

            InitializeCompanyDefaults();
            _rechnung.Positionen.Clear();
            DataContext = _rechnung;
            LinkedReceiptsGrid.ItemsSource = _linkedReceipts;
            _lastPlaintext = string.Empty;
            _selectedPosition = null;

            SetDokumentTyp(DokumentTyp.Rechnung);
            LoadKunden();
            DisplayRechnung();
        }

        private void RefreshKundenSelection()
        {
            if (KundeComboBox == null)
                return;

            // Rebind to ensure SelectionChanged fires and UI doesn't get stuck with a stale selection
            // when switching between Angebot/Rechnung.
            var selected = KundeComboBox.SelectedItem;
            KundeComboBox.SelectedItem = null;
            KundeComboBox.SelectedItem = selected;
        }

        private void FirmaBilgileri_Click(object sender, RoutedEventArgs e)
        {
            var editable = new FirmaProfil
            {
                Adresse = new Adresse
                {
                    Firmenname = _firmaProfil.Adresse.Firmenname,
                    Strasse = _firmaProfil.Adresse.Strasse,
                    Hausnummer = _firmaProfil.Adresse.Hausnummer,
                    Postleitzahl = _firmaProfil.Adresse.Postleitzahl,
                    Stadt = _firmaProfil.Adresse.Stadt,
                    Land = _firmaProfil.Adresse.Land,
                    Telefon = _firmaProfil.Adresse.Telefon,
                    Email = _firmaProfil.Adresse.Email,
                    Webseite = _firmaProfil.Adresse.Webseite
                },
                Steuernummer = _firmaProfil.Steuernummer,
                UstIdNr = _firmaProfil.UstIdNr,
                Kontoinhaber = _firmaProfil.Kontoinhaber,
                IBAN = _firmaProfil.IBAN,
                BIC = _firmaProfil.BIC,
                Zahlungsbedingungen = _firmaProfil.Zahlungsbedingungen,
                Angebotsbedingungen = _firmaProfil.Angebotsbedingungen,
                AngebotEinleitungText = _firmaProfil.AngebotEinleitungText,
                AngebotHaftungText = _firmaProfil.AngebotHaftungText,
                AngebotAuftragText = _firmaProfil.AngebotAuftragText,
                AngebotWiderrufText = _firmaProfil.AngebotWiderrufText,
                LogoPath = _firmaProfil.LogoPath
            };

            var w = new FirmaBilgileriWindow(editable, _firmaProfilService)
            {
                Owner = this
            };

            if (w.ShowDialog() == true)
            {
                _firmaProfil = editable;
                InitializeCompanyDefaults();
                DisplayRechnung();
            }
        }

        private void AngebotMetinleri_Click(object sender, RoutedEventArgs e)
        {
            var editable = new FirmaProfil
            {
                Adresse = _firmaProfil.Adresse,
                Steuernummer = _firmaProfil.Steuernummer,
                UstIdNr = _firmaProfil.UstIdNr,
                Kontoinhaber = _firmaProfil.Kontoinhaber,
                IBAN = _firmaProfil.IBAN,
                BIC = _firmaProfil.BIC,
                Zahlungsbedingungen = _firmaProfil.Zahlungsbedingungen,
                Angebotsbedingungen = _firmaProfil.Angebotsbedingungen,
                AngebotEinleitungText = _firmaProfil.AngebotEinleitungText,
                AngebotHaftungText = _firmaProfil.AngebotHaftungText,
                AngebotAuftragText = _firmaProfil.AngebotAuftragText,
                AngebotWiderrufText = _firmaProfil.AngebotWiderrufText,
                LogoPath = _firmaProfil.LogoPath
            };

            var w = new AngebotMetinleriWindow(editable, _firmaProfilService)
            {
                Owner = this
            };

            if (w.ShowDialog() == true)
            {
                _firmaProfil = editable;
                InitializeCompanyDefaults();
                DisplayRechnung();
            }
        }

        private void Cikis_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void NormalizePositionNumbers()
        {
            var nr = 1;
            foreach (var pos in _rechnung.Positionen)
            {
                if (pos.Nummer <= 0)
                    pos.Nummer = nr;
                nr = Math.Max(nr, pos.Nummer + 1);
            }
        }

        private void DisplayRechnung()
        {
            if (_selectedPosition is not null && !_rechnung.Positionen.Contains(_selectedPosition))
                _selectedPosition = _rechnung.Positionen.LastOrDefault();

            _lastPlaintext = _service.GenerierePlaintext(_rechnung);
        }

        private void SetDocumentStatus(DokumentStatus status, string actionText)
        {
            if (_rechnung == null)
                return;

            _rechnung.Status = status;
            _rechnung.LetzteAktionAm = DateTime.Now;
            _rechnung.LetzteAktionText = actionText;
        }

        private void RemoveLegacyProfessionellerRechnungstextFromNotizen()
        {
            if (string.IsNullOrWhiteSpace(_rechnung.Notizen))
                return;

            _rechnung.Notizen = _rechnung.Notizen.Replace(ProfessionellerRechnungstext + "\n\n", string.Empty, StringComparison.Ordinal)
                .Replace(ProfessionellerRechnungstext, string.Empty, StringComparison.Ordinal)
                .Trim();

            if (string.IsNullOrWhiteSpace(_rechnung.Notizen))
                _rechnung.Notizen = null;
        }

        private void ProfessionellerRechnungstextMenu_Click(object sender, RoutedEventArgs e)
        {
            if (_rechnung is null)
                return;

            if (_rechnung.DokumentTyp != DokumentTyp.Rechnung)
            {
                if (SidebarProfessionellerRechnungstextCheckBox != null)
                    SidebarProfessionellerRechnungstextCheckBox.IsChecked = false;
                MessageBox.Show(
                    _localization.IsGerman
                        ? "Der professionelle Rechnungstext ist nur für Rechnungen verfügbar."
                        : "Profesyonel fatura metni yalnızca faturalar için kullanılabilir.",
                    _localization.IsGerman ? "Hinweis" : "Bilgi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            _rechnung.ProfessionellerRechnungstextAktiv = SidebarProfessionellerRechnungstextCheckBox?.IsChecked == true;

            RemoveLegacyProfessionellerRechnungstextFromNotizen();

            DataContext = null;
            DataContext = _rechnung;
            DisplayRechnung();
        }

        private void PositionEditor_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: Rechnungsposition pos })
                _selectedPosition = pos;
        }

        private void RechnungButton_Click(object sender, RoutedEventArgs e)
        {
            SetDokumentTyp(DokumentTyp.Rechnung);
            SetDocumentStatus(DokumentStatus.Entwurf, _localization.IsGerman ? "Als Rechnung geöffnet" : "Fatura olarak açıldı");
            DisplayRechnung();
        }

        private void NeueAngebotMenu_Click(object sender, RoutedEventArgs e)
        {
            if (!ConfirmDiscardCurrent())
                return;

            StartNewAngebot();
        }

        private void NeueRechnungMenu_Click(object sender, RoutedEventArgs e)
        {
            if (!ConfirmDiscardCurrent())
                return;

            StartNewInvoice();
        }

        private void AngebotToRechnungMenu_Click(object sender, RoutedEventArgs e)
        {
            if (_rechnung is null)
                return;

            if (_rechnung.DokumentTyp != DokumentTyp.Angebot)
            {
                var (title, message) = GetConvertOnlyFromAngebotText();
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var (confirmTitle, confirmMessage) = GetConvertConfirmText();
            var result = MessageBox.Show(confirmMessage, confirmTitle, MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;

            ConvertCurrentAngebotToRechnung();
        }

        private void ConvertCurrentAngebotToRechnung()
        {
            var now = DateTime.Now;

            _rechnung.DokumentTyp = DokumentTyp.Rechnung;
            _rechnung.Rechnungsnummer = _nummerService.Next(DokumentTyp.Rechnung, now);
            _rechnung.Rechnungsdatum = DateTime.Now; // Ensure the invoice issue date is set to the current date
            _rechnung.Leistungsdatum = now;
            _rechnung.Fälligkeitsdatum = null;

            // Angebot-only fields are not relevant anymore
            _rechnung.GueltigBis = null;
            _rechnung.Lieferzeit = null;
            SetDocumentStatus(DokumentStatus.Entwurf, _localization.IsGerman ? "Aus Angebot in Rechnung umgewandelt" : "Tekliften faturaya dönüştürüldü");

            SetDokumentTyp(DokumentTyp.Rechnung);
            DisplayRechnung();
        }

        private (string Title, string Message) GetConvertOnlyFromAngebotText()
        {
            return _localization.IsGerman
                ? ("Hinweis", "Diese Funktion ist nur verfügbar, wenn ein Angebot geöffnet ist.")
                : ("Bilgi", "Bu işlem yalnızca açık bir teklif varken kullanılabilir.");
        }

        private (string Title, string Message) GetConvertConfirmText()
        {
            return _localization.IsGerman
                ? ("Bestätigung", "Dieses Angebot wird in eine Rechnung umgewandelt. Fortfahren?")
                : ("Onay", "Bu teklif bir faturaya dönüştürülecek. Devam edilsin mi?");
        }

        private bool ConfirmDiscardCurrent()
        {
            if (_rechnung is null)
                return true;

            var hasContent =
                _rechnung.Positionen.Count > 0 ||
                !string.IsNullOrWhiteSpace(_rechnung.Empfänger?.Firmenname) ||
                !string.IsNullOrWhiteSpace(_rechnung.Empfänger?.Strasse) ||
                !string.IsNullOrWhiteSpace(_rechnung.Empfänger?.Stadt) ||
                !string.IsNullOrWhiteSpace(_rechnung.Notizen);

            if (!hasContent)
                return true;

            var result = MessageBox.Show(
                _localization.IsGerman
                    ? "Die aktuelle Arbeit geht verloren und ein neues Dokument wird geöffnet. Möchten Sie fortfahren?"
                    : "Mevcut çalışma kaybolacak ve yeni bir belge açılacak. Devam edilsin mi?",
                _localization.IsGerman ? "Bestätigung" : "Onay",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            return result == MessageBoxResult.Yes;
        }

        private void Kleinunternehmer_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (_rechnung.IstKleinunternehmer)
            {
                foreach (var p in _rechnung.Positionen)
                    p.Steuersatz = 0;
            }

            DisplayRechnung();
        }

        private void GenerierenButton_Click(object sender, RoutedEventArgs e)
        {
            NormalizePositionNumbers();

            var fehler = _service.Validierungsfehler(_rechnung);
            if (fehler.Count > 0)
            {
                MessageBox.Show(string.Join("\n", fehler), _localization.IsGerman ? "Validierungsfehler" : "Doğrulama hatası", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DisplayRechnung();

            try
            {
                SetDocumentStatus(DokumentStatus.Vorschau, _localization.IsGerman ? "PDF-Vorschau erstellt" : "PDF önizlemesi oluşturuldu");
                var pdfBytes = _service.GenerierePdfBytes(_rechnung, GetLinkedReceiptPdfPaths());
                var suggestedName = _rechnung.DokumentTyp == DokumentTyp.Angebot
                    ? $"{_rechnung.Rechnungsnummer}.pdf"
                    : $"{_rechnung.Rechnungsnummer}.pdf";
                var w = new PdfPreviewWindow(pdfBytes, suggestedName)
                {
                    Owner = this
                };
                w.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    _localization.IsGerman ? $"PDF konnte nicht erstellt werden: {ex.Message}" : $"PDF oluşturulamadı: {ex.Message}",
                    _localization.IsGerman ? "Fehler" : "Hata",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void NeueRechnungButton_Click(object sender, RoutedEventArgs e)
        {
            RechnungButton_Click(sender, e);
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            NormalizePositionNumbers();

            var fehler = _service.Validierungsfehler(_rechnung);
            if (fehler.Count > 0)
            {
                MessageBox.Show(string.Join("\n", fehler), _localization.IsGerman ? "Validierungsfehler" : "Doğrulama hatası", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var settings = _archivSettingsService.LoadOrDefault();
            if (string.IsNullOrWhiteSpace(settings.DefaultSaveOrdner) || !Directory.Exists(settings.DefaultSaveOrdner))
            {
                MessageBox.Show(
                    _localization.IsGerman ? "Bitte wählen Sie zuerst einen Speicherordner aus." : "Lütfen önce bir kayıt klasörü seçin.",
                    _localization.IsGerman ? "Hinweis" : "Bilgi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            try
            {
                var pdfPath = SaveAndArchiveCurrentDocument(settings, _localization.IsGerman ? "Gespeichert und archiviert" : "Kaydedildi ve arşivlendi");

                MessageBox.Show(_rechnung.DokumentTyp == DokumentTyp.Angebot
                    ? (_localization.IsGerman ? $"Angebot erfolgreich gespeichert!\n{pdfPath}" : $"Teklif başarıyla kaydedildi!\n{pdfPath}")
                    : (_localization.IsGerman ? $"Rechnung erfolgreich gespeichert!\n{pdfPath}" : $"Fatura başarıyla kaydedildi!\n{pdfPath}"),
                    _localization.IsGerman ? "Erfolg" : "Başarılı",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    _localization.IsGerman ? $"Fehler beim Speichern: {ex.Message}" : $"Kaydetme hatası: {ex.Message}",
                    _localization.IsGerman ? "Fehler" : "Hata",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void KaydetmeKlasoruSec_Click(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            var folder = FolderPicker.PickFolder(hwnd, _localization.IsGerman ? "Speicherordner auswählen" : "Kayıt klasörünü seç");
            if (string.IsNullOrWhiteSpace(folder))
                return;

            var settings = _archivSettingsService.LoadOrDefault();
            settings.DefaultSaveOrdner = folder;
            _archivSettingsService.Save(settings);

            MessageBox.Show(
                _localization.IsGerman ? $"Speicherordner wurde festgelegt:\n{folder}" : $"Kayıt klasörü ayarlandı:\n{folder}",
                _localization.IsGerman ? "Hinweis" : "Bilgi",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void AddPositionButton_Click(object sender, RoutedEventArgs e)
        {
            var next = _rechnung.Positionen.Count == 0 ? 1 : _rechnung.Positionen.Max(p => p.Nummer) + 1;
            var pos = new Rechnungsposition { Nummer = next, Menge = 1, EinzelPreis = 0, Steuersatz = _rechnung.IstKleinunternehmer ? 0 : 19 };
            _rechnung.Positionen.Add(pos);
            _selectedPosition = pos;

            DisplayRechnung();
        }

        private void AddFromCatalogButton_Click(object sender, RoutedEventArgs e)
        {
            var artikel = _artikelService.Load();
            var w = new ArtikelAuswahlWindow(artikel) { Owner = this };
            if (w.ShowDialog() != true || w.SelectedArtikel is null)
                return;

            var a = w.SelectedArtikel;
            var next = _rechnung.Positionen.Count == 0 ? 1 : _rechnung.Positionen.Max(p => p.Nummer) + 1;

            _rechnung.Positionen.Add(new Rechnungsposition
            {
                Nummer = next,
                Beschreibung = string.IsNullOrWhiteSpace(a.Beschreibung) ? a.Bezeichnung : a.Beschreibung,
                Menge = 1,
                Einheit = string.IsNullOrWhiteSpace(a.Einheit) ? "Stück" : a.Einheit,
                EinzelPreis = a.StandardPreis,
                Steuersatz = _rechnung.IstKleinunternehmer ? 0 : a.StandardMwSt
            });

            _selectedPosition = _rechnung.Positionen.LastOrDefault();

            DisplayRechnung();
        }

        private void AngebotButton_Click(object sender, RoutedEventArgs e)
        {
            SetDokumentTyp(DokumentTyp.Angebot);
            DisplayRechnung();
        }

        private void KundeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (KundeComboBox.SelectedItem is not Kunde k)
                return;

            // Replace the whole Empfänger object so WPF bindings refresh reliably
            // even though Rechnung itself doesn't implement INotifyPropertyChanged.
            _rechnung.Empfänger = new Adresse
            {
                Firmenname = k.Adresse.Firmenname,
                Strasse = k.Adresse.Strasse,
                Hausnummer = k.Adresse.Hausnummer,
                Postleitzahl = k.Adresse.Postleitzahl,
                Stadt = k.Adresse.Stadt,
                Land = k.Adresse.Land,
                Telefon = k.Adresse.Telefon,
                Email = k.Adresse.Email,
                Webseite = k.Adresse.Webseite
            };

            // refresh bindings because Rechnung does not implement INotifyPropertyChanged
            DataContext = null;
            DataContext = _rechnung;
            DisplayRechnung();
        }

        private void ArsivKlasoruSec_Click(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            var folder = FolderPicker.PickFolder(hwnd, _localization.IsGerman ? "Archivordner auswählen" : "Arşiv klasörünü seç");
            if (string.IsNullOrWhiteSpace(folder))
                return;

            var settings = _archivSettingsService.LoadOrDefault();
            settings.ArchivOrdner = folder;
            _archivSettingsService.Save(settings);
            MessageBox.Show(
                _localization.IsGerman ? $"Archivordner wurde festgelegt:\n{folder}" : $"Arşiv klasörü ayarlandı:\n{folder}",
                _localization.IsGerman ? "Hinweis" : "Bilgi",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void Musteriler_Click(object sender, RoutedEventArgs e)
        {
            var w = new KundenWindow(_kundenService) { Owner = this };
            w.ShowDialog();
            LoadKunden();
        }

        private void Arsiv_Click(object sender, RoutedEventArgs e)
        {
            var w = new ArchivWindow(_archivService) { Owner = this };
            w.ShowDialog();
            RefreshLinkedReceipts();
        }

        private void Eingangsrechnungen_Click(object sender, RoutedEventArgs e)
        {
            var w = new EingangsrechnungenWindow(_eingangsrechnungService) { Owner = this };
            w.ShowDialog();
            RefreshLinkedReceipts();
        }

        private void AssignReceiptButton_Click(object sender, RoutedEventArgs e)
        {
            EnsurePreviewNumber();

            var invoiceNumber = _rechnung?.Rechnungsnummer;
            if (string.IsNullOrWhiteSpace(invoiceNumber))
            {
                MessageBox.Show(
                    _localization["Main.LinkedReceipts.MissingInvoiceNumber"],
                    _localization["Common.Info"],
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var w = new EingangsrechnungenWindow(_eingangsrechnungService, invoiceNumber) { Owner = this };
            w.ShowDialog();
            RefreshLinkedReceipts();
        }

        private void OpenLinkedReceiptButton_Click(object sender, RoutedEventArgs e)
        {
            if (LinkedReceiptsGrid.SelectedItem is not EingangsrechnungEintrag selectedReceipt)
            {
                MessageBox.Show(
                    _localization["Main.LinkedReceipts.SelectEntryFirst"],
                    _localization["Common.Info"],
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            try
            {
                _eingangsrechnungService.OpenDocument(selectedReceipt);
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

        private void RemoveLinkedReceiptButton_Click(object sender, RoutedEventArgs e)
        {
            if (LinkedReceiptsGrid.SelectedItem is not EingangsrechnungEintrag selectedReceipt)
            {
                MessageBox.Show(
                    _localization["Main.LinkedReceipts.SelectEntryFirst"],
                    _localization["Common.Info"],
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                _localization["Main.LinkedReceipts.RemoveConfirm"],
                _localization["Archive.DeleteConfirmTitle"],
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                _eingangsrechnungService.AssignToInvoice(selectedReceipt, null);
                RefreshLinkedReceipts();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(_localization["Main.LinkedReceipts.RemoveFailed"], ex.Message),
                    _localization["Common.Error"],
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void RefreshLinkedReceipts()
        {
            if (LinkedReceiptsGrid == null)
                return;

            _linkedReceipts.Clear();

            var invoiceNumber = _rechnung?.Rechnungsnummer;
            if (string.IsNullOrWhiteSpace(invoiceNumber))
                return;

            foreach (var item in _eingangsrechnungService.LoadByAssignedInvoice(invoiceNumber))
                _linkedReceipts.Add(item);

            if (_linkedReceipts.Count > 0)
                LinkedReceiptsGrid.SelectedIndex = 0;
        }

        private void InvoiceNumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            RefreshLinkedReceipts();
        }

        private List<string> GetLinkedReceiptPdfPaths()
        {
            return _linkedReceipts
                .Where(x => !string.IsNullOrWhiteSpace(x.DokumentPfad)
                    && File.Exists(x.DokumentPfad)
                    && IsSupportedReceiptAttachment(x.DokumentPfad))
                .Select(x => x.DokumentPfad!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
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

        private void EmailAyarlar_Click(object sender, RoutedEventArgs e)
        {
            var settings = _emailSettingsService.LoadOrDefault();
            var w = new EmailSettingsWindow(settings, _emailSettingsService) { Owner = this };
            w.ShowDialog();
        }

        private void FinanzDashboard_Click(object sender, RoutedEventArgs e)
        {
            var reportingService = new FinancialReportingService(_archivService, _eingangsrechnungService);
            while (true)
            {
                var summary = reportingService.BuildSummary();
                var w = new FinanzDashboardWindow(summary, _archivService) { Owner = this };
                var result = w.ShowDialog();
                if (result != true)
                    break;
            }
        }

        private void EmailGonder_Click(object sender, RoutedEventArgs e)
        {
            NormalizePositionNumbers();
            var fehler = _service.Validierungsfehler(_rechnung);
            if (fehler.Count > 0)
            {
                MessageBox.Show(string.Join("\n", fehler), _localization.IsGerman ? "Validierungsfehler" : "Doğrulama hatası", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var emailSettings = _emailSettingsService.LoadOrDefault();
            var pdfBytes = _service.GenerierePdfBytes(_rechnung);
            var attachmentName = $"{(_rechnung.DokumentTyp == DokumentTyp.Angebot ? (_localization.IsGerman ? "Angebot" : "Teklif") : (_localization.IsGerman ? "Rechnung" : "Fatura"))}_{_rechnung.Rechnungsnummer}.pdf";

            var w = new EmailSendWindow(_emailService, emailSettings, pdfBytes, attachmentName)
            {
                Owner = this
            };
            w.To = _rechnung.Empfänger?.Email ?? string.Empty;
            w.Subject = $"{(_rechnung.DokumentTyp == DokumentTyp.Angebot ? (_localization.IsGerman ? "Angebot" : "Teklif") : (_localization.IsGerman ? "Rechnung" : "Fatura"))} {_rechnung.Rechnungsnummer}";
            var absenderName = !string.IsNullOrWhiteSpace(emailSettings.FromName)
                ? emailSettings.FromName
                : (_firmaProfil?.Adresse?.Firmenname ?? string.Empty);

            var docText = _rechnung.DokumentTyp == DokumentTyp.Angebot
                ? (_localization.IsGerman ? $"unser Angebot mit der Nummer {_rechnung.Rechnungsnummer}" : $"{_rechnung.Rechnungsnummer} numaralı teklifimiz")
                : (_localization.IsGerman ? $"unsere Rechnung mit der Nummer {_rechnung.Rechnungsnummer}" : $"{_rechnung.Rechnungsnummer} numaralı faturamız");

            w.Body = (_localization.IsGerman ? "Guten Tag,\n\n" : "Merhaba,\n\n") +
                     (_localization.IsGerman ? $"anbei übersenden wir Ihnen {docText} im PDF-Format.\n\n" : $"Ek'te {docText} PDF formatında tarafınıza sunulmuştur.\n\n") +
                     (_localization.IsGerman ? "Mit freundlichen Grüßen" : "Saygılarımızla") +
                     (!string.IsNullOrWhiteSpace(absenderName) ? $"\n{absenderName}" : string.Empty);
            w.DataContext = null;
            w.DataContext = w;
            var result = w.ShowDialog();
            if (result == true)
            {
                try
                {
                    var settings = _archivSettingsService.LoadOrDefault();
                    if (string.IsNullOrWhiteSpace(settings.DefaultSaveOrdner) || !Directory.Exists(settings.DefaultSaveOrdner))
                    {
                        MessageBox.Show(
                            _localization.IsGerman ? "Die E-Mail wurde gesendet, aber es ist kein gültiger Speicherordner festgelegt." : "E-posta gönderildi ancak geçerli bir kayıt klasörü ayarlı değil.",
                            _localization["Common.Info"],
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        return;
                    }

                    var pdfPath = SaveAndArchiveCurrentDocument(settings, _localization.IsGerman ? "Per E-Mail gesendet und archiviert" : "E-posta ile gönderildi ve arşivlendi");
                    MessageBox.Show(
                        _localization.IsGerman ? $"Die E-Mail wurde gesendet und das Dokument wurde automatisch gespeichert.\n{pdfPath}" : $"E-posta gönderildi ve belge otomatik kaydedildi.\n{pdfPath}",
                        _localization["Common.Info"],
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        _localization.IsGerman ? $"Die E-Mail wurde gesendet, aber das automatische Speichern ist fehlgeschlagen: {ex.Message}" : $"E-posta gönderildi ancak otomatik kaydetme başarısız oldu: {ex.Message}",
                        _localization["Common.Error"],
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private string SaveAndArchiveCurrentDocument(ArchivSettings settings, string archiveActionText)
        {
            var keepExistingNumber = !string.IsNullOrWhiteSpace(_rechnung.ArchivJsonPath) || !string.IsNullOrWhiteSpace(_rechnung.SavePdfPath);
            var preview = _nummerService.PreviewNext(_rechnung.DokumentTyp, _rechnung.Rechnungsdatum);
            if (!keepExistingNumber && (string.IsNullOrWhiteSpace(_rechnung.Rechnungsnummer) || string.Equals(_rechnung.Rechnungsnummer, preview, StringComparison.OrdinalIgnoreCase)))
                _rechnung.Rechnungsnummer = _nummerService.Next(_rechnung.DokumentTyp, _rechnung.Rechnungsdatum);

            if (NummerService.TryParseSeq(_rechnung.DokumentTyp, _rechnung.Rechnungsnummer, out var seq))
                _nummerService.EnsureAtLeast(_rechnung.DokumentTyp, seq);

            var pdfBytes = _service.GenerierePdfBytes(_rechnung, GetLinkedReceiptPdfPaths());
            Directory.CreateDirectory(settings.DefaultSaveOrdner!);
            var pdfPath = !string.IsNullOrWhiteSpace(_rechnung.SavePdfPath)
                ? _rechnung.SavePdfPath
                : Path.Combine(settings.DefaultSaveOrdner!, $"{_rechnung.Rechnungsnummer}.pdf");
            File.WriteAllBytes(pdfPath, pdfBytes);
            _rechnung.SavePdfPath = pdfPath;
            _rechnung.SaveJsonPath = Path.ChangeExtension(pdfPath, ".json");

            SetDocumentStatus(DokumentStatus.Archiviert, archiveActionText);
            var archivedEntry = _archivService.ArchivEintragAktualisieren(_rechnung, pdfBytes);
            if (archivedEntry != null)
                _archivService.UpdateStatus(archivedEntry, _rechnung.Status, _rechnung.LetzteAktionText);

            var sidecarJsonPath = _rechnung.SaveJsonPath!;
            if (archivedEntry != null && !string.IsNullOrWhiteSpace(archivedEntry.JsonPath) && File.Exists(archivedEntry.JsonPath))
                File.Copy(archivedEntry.JsonPath, sidecarJsonPath, true);

            return pdfPath;
        }

        private void SidebarHoverZone_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            SetSidebarExpanded(true);
        }

        private void SidebarDrawer_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            SetSidebarExpanded(true);
        }

        private void SidebarDrawer_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (IsMouseOverElement(SidebarDrawer) || IsMouseOverElement(SidebarHoverZone))
                return;

            SetSidebarExpanded(false);
        }

        private void RightSidebarHoverZone_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            SetRightSidebarExpanded(true);
        }

        private void RightSidebarDrawer_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            SetRightSidebarExpanded(true);
        }

        private void RightSidebarDrawer_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var rightSidebarDrawer = FindName("RightSidebarDrawer") as FrameworkElement;
            var rightSidebarHoverZone = FindName("RightSidebarHoverZone") as FrameworkElement;

            if (IsMouseOverElement(rightSidebarDrawer) || IsMouseOverElement(rightSidebarHoverZone))
                return;

            SetRightSidebarExpanded(false);
        }

        private void SetSidebarExpanded(bool expanded)
        {
            if (SidebarDrawer == null || SidebarContent == null || _isSidebarExpanded == expanded)
                return;

            _isSidebarExpanded = expanded;
            SidebarDrawer.Width = expanded ? 300 : 18;
            SidebarDrawer.Padding = expanded ? new Thickness(0) : new Thickness(0);
            SidebarContent.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;
            SidebarContent.Opacity = expanded ? 1 : 0;
        }

        private void SetRightSidebarExpanded(bool expanded)
        {
            if (FindName("RightSidebarDrawer") is not Border rightSidebarDrawer ||
                FindName("RightSidebarContent") is not ScrollViewer rightSidebarContent)
                return;

            rightSidebarDrawer.Width = expanded ? 278 : 18;
            rightSidebarDrawer.Padding = expanded ? new Thickness(0) : new Thickness(0);
            rightSidebarContent.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;
            rightSidebarContent.Opacity = expanded ? 1 : 0;
        }

        private static bool IsMouseOverElement(FrameworkElement? control)
        {
            return control is not null && control.IsMouseOver;
        }

        private void RemovePositionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPosition is Rechnungsposition pos)
            {
                _rechnung.Positionen.Remove(pos);
                _selectedPosition = _rechnung.Positionen.LastOrDefault();
                DisplayRechnung();
            }
        }

        private void PositionCard_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: Rechnungsposition pos })
                _selectedPosition = pos;
        }
    }
}
