using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using Örnek.Services;

namespace Örnek
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly AppDataMigrationService _migrationService = new();
        private readonly UpdateService _updateService = new();
        private readonly UpdateSettingsService _updateSettingsService = new();
        private readonly LocalizationService _localization = LocalizationService.Instance;

        protected override void OnStartup(StartupEventArgs e)
        {
            var culture = CultureInfo.GetCultureInfo("de-DE");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

            _migrationService.MigrateLegacyDataIfNeeded();

            base.OnStartup(e);

            Dispatcher.BeginInvoke(async () => await CheckForUpdatesAsync());
        }

        private async Task CheckForUpdatesAsync()
        {
            var manifestUrl = _updateSettingsService.LoadManifestUrl();
            if (string.IsNullOrWhiteSpace(manifestUrl))
                return;

            var result = await _updateService.CheckForUpdateAsync(manifestUrl);
            if (result is null || !result.IsUpdateAvailable)
                return;

            if (MainWindow is null)
                return;

            var prompt = new UpdatePromptWindow(result)
            {
                Owner = MainWindow
            };

            var dialogResult = prompt.ShowDialog();
            if (dialogResult == true && prompt.ShouldStartUpdate && !string.IsNullOrWhiteSpace(result.DownloadUrl))
            {
                try
                {
                    var installerPath = await _updateService.DownloadInstallerAsync(result.DownloadUrl);
                    _updateService.StartInstaller(installerPath);
                    Shutdown();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        string.Format(_localization["UpdatePrompt.DownloadFailed"], ex.Message),
                        _localization["Common.Error"],
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }
    }

}
