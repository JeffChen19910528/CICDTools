using Avalonia.Controls;
using Deployment.CLI.Localization;

namespace Deployment.Desktop.Views;

public partial class CreateAppDialog : Window
{
    public CreateAppDialog()
    {
        InitializeComponent();
        Header.Text = L.T("app.menu.create");
        NameLabel.Text = L.T("app.create.namePrompt");
        DescLabel.Text = L.T("app.create.descPrompt");
        OkButton.Content = L.T("common.ok");
        CancelButton.Content = L.T("common.cancel");
        Title = L.T("app.menu.create");
        Opened += (_, _) => NameBox.Focus();
    }

    private void OnOkClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            ErrorText.Text = L.T("app.create.namePrompt");
            ErrorText.IsVisible = true;
            return;
        }

        Close((NameBox.Text.Trim(), DescBox.Text?.Trim() ?? string.Empty));
    }

    private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close(null);
}
