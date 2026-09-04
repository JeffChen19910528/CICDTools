using AppEntity = Deployment.Domain.Entities.Application;
using Avalonia.Controls;
using Deployment.CLI.Localization;
using Deployment.Desktop.Services;
using Deployment.Domain.Entities;

namespace Deployment.Desktop.Views;

public partial class ReleasesView : UserControl
{
    private readonly AppServices _services;
    private readonly Window _owner;
    private IReadOnlyList<Release> _releases = [];

    public ReleasesView() : this(null!, null!) { }

    public ReleasesView(AppServices services, Window owner)
    {
        InitializeComponent();
        _services = services;
        _owner = owner;

        Header.Text = L.T("release.menu.title");
        CreateReleaseButton.Content = L.T("release.menu.create");
        RefreshButton.Content = L.T("common.refresh");
        Grid.Columns[0].Header = L.T("release.list.colId");
        Grid.Columns[1].Header = L.T("release.list.colVersion");
        Grid.Columns[2].Header = L.T("release.list.colCreated");
        Grid.Columns[3].Header = L.T("release.list.colBy");

        Loaded += async (_, _) => await LoadAppsAsync();
    }

    private async Task LoadAppsAsync()
    {
        var apps = await _services.AppRepo.ListAsync();
        AppCombo.ItemsSource = apps.Select(a => new ComboItem<AppEntity>(a.Name, a)).ToList();
        AppCombo.SelectedIndex = apps.Count > 0 ? 0 : -1;
    }

    private async void OnAppChanged(object? sender, SelectionChangedEventArgs e) => await RefreshAsync();
    private async void OnRefreshClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => await RefreshAsync();

    private async Task RefreshAsync()
    {
        var app = (AppCombo.SelectedItem as ComboItem<AppEntity>)?.Value;
        if (app is null)
        {
            Grid.ItemsSource = null;
            _releases = [];
            return;
        }

        _releases = await _services.ReleaseService.ListReleasesAsync(app.Name);
        Grid.ItemsSource = _releases.Select(r => new ReleaseRow(
            r.ReleaseId, r.Version, r.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"), r.CreatedBy)).ToList();
    }

    private async void OnCreateReleaseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var app = (AppCombo.SelectedItem as ComboItem<AppEntity>)?.Value;
        if (app is null)
        {
            await DialogService.ShowMessageAsync(_owner, L.T("release.menu.create"), L.T("common.noAppsYet"));
            return;
        }

        var dialog = new CreateReleaseDialog();
        var result = await dialog.ShowDialog<(string Version, string Source, string Notes)?>(_owner);
        if (result is null) return;

        try
        {
            await _services.ReleaseService.CreateReleaseAsync(
                app.Name, result.Value.Version, result.Value.Source, _services.Options.ReleasesStorePath,
                Environment.UserName, notes: result.Value.Notes);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            await DialogService.ShowMessageAsync(_owner, L.T("release.menu.create"), L.T("common.error", ex.Message), isError: true);
        }
    }

    private async void OnRowDoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (Grid.SelectedItem is not ReleaseRow row) return;
        var release = _releases.FirstOrDefault(r => r.ReleaseId == row.ReleaseId);
        if (release is null) return;

        var full = await _services.ReleaseService.GetReleaseAsync(release.ReleaseId);
        await new ReleaseDetailDialog(full).ShowDialog(_owner);
    }

    private sealed record ReleaseRow(string ReleaseId, string Version, string Created, string CreatedBy);
}
