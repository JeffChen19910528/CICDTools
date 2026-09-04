using AppEntity = Deployment.Domain.Entities.Application;
using Avalonia.Controls;
using Deployment.CLI.Localization;

namespace Deployment.Desktop.Views;

public partial class AddEnvironmentDialog : Window
{
    public AppEntity? SelectedApp { get; private set; }

    public AddEnvironmentDialog() : this([]) { }

    public AddEnvironmentDialog(IReadOnlyList<AppEntity> apps)
    {
        InitializeComponent();
        Header.Text = L.T("app.menu.addEnv");
        Title = L.T("app.menu.addEnv");
        AppLabel.Text = L.T("select.app");
        NameLabel.Text = L.T("app.env.namePrompt");
        RequireApprovalCheck.Content = L.T("app.env.requireApproval");
        OkButton.Content = L.T("common.ok");
        CancelButton.Content = L.T("common.cancel");

        AppCombo.ItemsSource = apps.Select(a => new ComboItem<AppEntity>(a.Name, a)).ToList();
        if (apps.Count > 0) AppCombo.SelectedIndex = 0;
    }

    private void OnAppChanged(object? sender, SelectionChangedEventArgs e) =>
        SelectedApp = (AppCombo.SelectedItem as ComboItem<AppEntity>)?.Value;

    private void OnOkClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (SelectedApp is null || string.IsNullOrWhiteSpace(NameBox.Text))
        {
            ErrorText.Text = L.T("app.env.namePrompt");
            ErrorText.IsVisible = true;
            return;
        }

        Close((SelectedApp, NameBox.Text.Trim(), RequireApprovalCheck.IsChecked ?? false));
    }

    private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close(null);
}
