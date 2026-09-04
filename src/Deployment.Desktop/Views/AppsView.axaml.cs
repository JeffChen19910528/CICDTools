using AppEntity = Deployment.Domain.Entities.Application;
using Avalonia.Controls;
using Deployment.CLI.Localization;
using Deployment.Desktop.Services;

namespace Deployment.Desktop.Views;

public partial class AppsView : UserControl
{
    private readonly AppServices _services;
    private readonly Window _owner;

    public AppsView() : this(null!, null!) { }

    public AppsView(AppServices services, Window owner)
    {
        InitializeComponent();
        _services = services;
        _owner = owner;

        Header.Text = L.T("app.menu.title");
        CreateAppButton.Content = L.T("app.menu.create");
        AddEnvButton.Content = L.T("app.menu.addEnv");
        AddTargetButton.Content = L.T("app.menu.addTarget");
        RefreshButton.Content = L.T("common.refresh");

        Grid.Columns[0].Header = L.T("app.list.colApp");
        Grid.Columns[1].Header = L.T("app.list.colEnvs");
        Grid.Columns[2].Header = L.T("app.list.colTargets");

        Loaded += async (_, _) => await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        var apps = await _services.AppRepo.ListAsync();
        Grid.ItemsSource = apps.Select(a => new AppRow(
            a.Name,
            string.Join(", ", a.Environments.Select(e => e.Name)),
            string.Join("; ", a.Environments.Select(e =>
                $"{e.Name} ({string.Join(", ", e.Targets.Select(t => t.Name))})")))).ToList();
    }

    private async void OnCreateAppClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var dialog = new CreateAppDialog();
        var result = await dialog.ShowDialog<(string Name, string Desc)?>(_owner);
        if (result is null) return;

        var existing = await _services.AppRepo.GetByNameAsync(result.Value.Name);
        if (existing != null)
        {
            await DialogService.ShowMessageAsync(_owner, L.T("app.menu.create"), L.T("app.create.exists", result.Value.Name), isError: true);
            return;
        }

        await _services.AppRepo.AddAsync(new AppEntity { Name = result.Value.Name, Description = result.Value.Desc });
        await RefreshAsync();
    }

    private async void OnAddEnvClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var apps = await _services.AppRepo.ListAsync();
        if (apps.Count == 0)
        {
            await DialogService.ShowMessageAsync(_owner, L.T("app.menu.addEnv"), L.T("common.noAppsYet"));
            return;
        }

        var dialog = new AddEnvironmentDialog(apps);
        var result = await dialog.ShowDialog<(AppEntity AppEntity, string Name, bool RequireApproval)?>(_owner);
        if (result is null) return;

        var existing = await _services.AppRepo.GetEnvironmentAsync(result.Value.AppEntity.Id, result.Value.Name);
        if (existing != null)
        {
            await DialogService.ShowMessageAsync(_owner, L.T("app.menu.addEnv"), L.T("app.env.exists", result.Value.Name), isError: true);
            return;
        }

        await _services.AppRepo.AddEnvironmentAsync(new Domain.Entities.AppEnvironment
        {
            ApplicationId = result.Value.AppEntity.Id,
            Name = result.Value.Name,
            RequireApproval = result.Value.RequireApproval
        });
        await RefreshAsync();
    }

    private async void OnAddTargetClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var apps = await _services.AppRepo.ListAsync();
        if (apps.Count == 0 || apps.All(a => a.Environments.Count == 0))
        {
            await DialogService.ShowMessageAsync(_owner, L.T("app.menu.addTarget"), L.T("common.noEnvsYet"));
            return;
        }

        var dialog = new AddTargetDialog(apps);
        var result = await dialog.ShowDialog<(Domain.Entities.AppEnvironment Env, string Name, Domain.Entities.TargetOS Os, string Host, string Path)?>(_owner);
        if (result is null) return;

        await _services.AppRepo.AddTargetAsync(new Domain.Entities.DeploymentTarget
        {
            EnvironmentId = result.Value.Env.Id,
            Name = result.Value.Name,
            OS = result.Value.Os,
            Host = result.Value.Host,
            DeploymentPath = result.Value.Path
        });
        await RefreshAsync();
    }

    private async void OnRefreshClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => await RefreshAsync();

    private sealed record AppRow(string AppName, string EnvNames, string TargetSummary);
}
