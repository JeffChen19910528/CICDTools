using Avalonia.Controls;
using Avalonia.Input;
using Deployment.CLI.Localization;

namespace Deployment.Desktop.Views;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog() => InitializeComponent();

    public ConfirmDialog(string title, string message, bool defaultYes) : this()
    {
        Title = title;
        MessageText.Text = message;
        NoButton.Content = L.T("common.no");
        YesButton.Content = L.T("common.yes");
        if (defaultYes) YesButton.Focus();
        else NoButton.Focus();

        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) Close(false);
        };
    }

    private void OnYesClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close(true);
    private void OnNoClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close(false);
}
