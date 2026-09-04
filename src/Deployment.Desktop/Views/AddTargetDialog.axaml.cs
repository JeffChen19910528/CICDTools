using AppEntity = Deployment.Domain.Entities.Application;
using Avalonia.Controls;
using Deployment.CLI.Localization;
using Deployment.Desktop.Services;
using Deployment.Domain.Entities;

namespace Deployment.Desktop.Views;

public partial class AddTargetDialog : Window
{
    public AppEntity? SelectedApp { get; private set; }
    public AppEnvironment? SelectedEnvironment { get; private set; }

    public AddTargetDialog() : this([]) { }

    public AddTargetDialog(IReadOnlyList<AppEntity> apps)
    {
        InitializeComponent();
        Header.Text = L.T("app.menu.addTarget");
        Title = L.T("app.menu.addTarget");
        AppLabel.Text = L.T("select.app");
        EnvLabel.Text = L.T("select.env");
        NameLabel.Text = L.T("app.target.namePrompt");
        OsLabel.Text = L.T("app.target.osPrompt");
        HostLabel.Text = L.T("app.target.hostPrompt");
        PathLabel.Text = L.T("app.target.pathPrompt");
        BrowseButton.Content = L.T("common.browse");
        OkButton.Content = L.T("common.ok");
        CancelButton.Content = L.T("common.cancel");

        OsCombo.ItemsSource = new[] { TargetOS.Windows, TargetOS.Linux };
        OsCombo.SelectedIndex = OperatingSystem.IsWindows() ? 0 : 1;

        AppCombo.ItemsSource = apps.Select(a => new ComboItem<AppEntity>(a.Name, a)).ToList();
        if (apps.Count > 0) AppCombo.SelectedIndex = 0;
    }

    private void OnAppChanged(object? sender, SelectionChangedEventArgs e)
    {
        SelectedApp = (AppCombo.SelectedItem as ComboItem<AppEntity>)?.Value;
        var envs = SelectedApp?.Environments.ToList() ?? [];
        EnvCombo.ItemsSource = envs.Select(x => new ComboItem<AppEnvironment>(x.Name, x)).ToList();
        EnvCombo.SelectedIndex = envs.Count > 0 ? 0 : -1;
        SelectedEnvironment = envs.Count > 0 ? envs[0] : null;
    }

    private async void OnBrowseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var path = await DialogService.PickFolderAsync(this, L.T("app.target.pathPrompt"), PathBox.Text);
        if (path != null) PathBox.Text = path;
    }

    private void OnOkClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        SelectedEnvironment = (EnvCombo.SelectedItem as ComboItem<AppEnvironment>)?.Value;

        if (SelectedEnvironment is null || string.IsNullOrWhiteSpace(NameBox.Text) || string.IsNullOrWhiteSpace(PathBox.Text))
        {
            ErrorText.Text = L.T("app.target.namePrompt");
            ErrorText.IsVisible = true;
            return;
        }

        var os = (TargetOS)OsCombo.SelectedItem!;
        var host = string.IsNullOrWhiteSpace(HostBox.Text) ? "localhost" : HostBox.Text.Trim();

        Close((SelectedEnvironment, NameBox.Text.Trim(), os, host, PathBox.Text.Trim()));
    }

    private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close(null);
}
