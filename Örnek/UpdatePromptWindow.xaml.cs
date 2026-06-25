using System.Windows;
using Örnek.Models;
using Örnek.Services;

namespace Örnek;

public partial class UpdatePromptWindow : Window
{
    private readonly LocalizationService _localization = LocalizationService.Instance;

    public UpdatePromptWindow(UpdateCheckResult update)
    {
        InitializeComponent();
        UpdateInfo = update;
        ApplyLocalization();
    }

    public UpdateCheckResult UpdateInfo { get; }

    public bool ShouldStartUpdate { get; private set; }

    private void ApplyLocalization()
    {
        Title = _localization["UpdatePrompt.WindowTitle"];
        TitleTextBlock.Text = _localization["UpdatePrompt.Title"];
        MessageTextBlock.Text = string.Format(
            _localization["UpdatePrompt.Message"],
            UpdateInfo.AvailableVersion?.ToString() ?? "-",
            UpdateInfo.CurrentVersion?.ToString() ?? "-");
        NotesTextBlock.Text = string.IsNullOrWhiteSpace(UpdateInfo.Notes)
            ? _localization["UpdatePrompt.NoNotes"]
            : UpdateInfo.Notes;
        CancelButton.Content = _localization["Common.Cancel"];
        UpdateButton.Content = _localization["UpdatePrompt.UpdateButton"];
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        ShouldStartUpdate = true;
        DialogResult = true;
        Close();
    }
}
