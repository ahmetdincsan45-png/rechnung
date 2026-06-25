using System.Windows;

namespace Örnek;

public partial class NotizWindow : Window
{
    public string? NotizText { get; private set; }
    private readonly Services.LocalizationService _localization = Services.LocalizationService.Instance;

    public NotizWindow(string? current)
    {
        InitializeComponent();
        Title = _localization["NoteWindow.Title"];
        SaveButton.Content = _localization["Common.Save"];
        CancelButton.Content = _localization["Common.Cancel"];
        NotizTextBox.Text = current ?? string.Empty;
        NotizTextBox.Focus();
        NotizTextBox.CaretIndex = NotizTextBox.Text.Length;
    }

    private void Kaydet_Click(object sender, RoutedEventArgs e)
    {
        NotizText = NotizTextBox.Text;
        DialogResult = true;
        Close();
    }

    private void Iptal_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
