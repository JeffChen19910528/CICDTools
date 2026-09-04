using Avalonia.Controls;
using Deployment.CLI.Localization;
using Deployment.Desktop.Services;
using Deployment.Domain.Models;

namespace Deployment.Desktop.Views;

public partial class DeployView : UserControl
{
    private readonly AppServices _services;
    private readonly Window _owner;

    public DeployView() : this(null!, null!) { }

    public DeployView(AppServices services, Window owner)
    {
        InitializeComponent();
        _services = services;
        _owner = owner;

        Header.Text = L.T("deploy.menu.title");
        DiffButton.Content = L.T("deploy.menu.diff");
        DryRunButton.Content = L.T("deploy.menu.dryrun");
        DeployButton.Content = L.T("deploy.menu.run");
        RollbackButton.Content = L.T("deploy.menu.rollback");

        Selector.Configure(services, showRelease: true);
        Loaded += async (_, _) => await Selector.LoadAsync();
    }

    private async void OnDiffClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var (app, env, target, release) = Selection();
        if (app is null || env is null || target is null || release is null) return;

        var diff = await _services.DiffService.ComputeAsync(
            release.PackagePath, target.DeploymentPath, release.Version, target.CurrentRelease ?? "EMPTY");

        ResultText.Text = FormatDiff(diff);
    }

    private string FormatDiff(DiffResult diff)
    {
        var lines = new List<string>
        {
            L.T("deploy.diff.summary", diff.SourceVersion, diff.TargetVersion),
            L.T("deploy.diff.counts", diff.Added, diff.Modified, diff.Deleted, diff.Unchanged),
            string.Empty
        };

        foreach (var f in diff.Files.Where(f => f.ChangeType != FileChangeType.Unchanged))
        {
            var icon = f.ChangeType switch
            {
                FileChangeType.Added => "+",
                FileChangeType.Modified => "~",
                FileChangeType.Deleted => "-",
                _ => "="
            };
            lines.Add($"{icon} {f.RelativePath}");
        }

        return string.Join('\n', lines);
    }

    private async void OnDryRunClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => await DeployAsync(isDryRun: true);
    private async void OnDeployClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => await DeployAsync(isDryRun: false);

    private async Task DeployAsync(bool isDryRun)
    {
        var (app, env, target, release) = Selection();
        if (app is null || env is null || target is null || release is null) return;

        if (!isDryRun)
        {
            var warning = L.T("deploy.confirm.warning", release.Version, app.Name, env.Name, target.Name)
                + "\n" + L.T("deploy.confirm.backupNote");
            if (!await DialogService.ConfirmAsync(_owner, L.T("deploy.menu.run"), warning, defaultYes: false))
            {
                ResultText.Text = L.T("common.operationCancelled");
                return;
            }
        }

        try
        {
            var record = await _services.DeploymentService.StartDeploymentAsync(
                app.Name, env.Name, target.Name, release.Version, Environment.UserName, isDryRun);

            ResultText.Text = isDryRun
                ? L.T("deploy.result.dryrunDone")
                : L.T("deploy.result.success", record.DeploymentId);
        }
        catch (Exception ex)
        {
            ResultText.Text = L.T("deploy.result.failed", ex.Message);
        }
    }

    private async void OnRollbackClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var (app, env, target, release) = Selection();
        if (app is null || env is null || target is null || release is null) return;

        var warning = L.T("rollback.confirm.warning", app.Name, env.Name, target.Name, release.Version);
        if (!await DialogService.ConfirmAsync(_owner, L.T("deploy.menu.rollback"), warning, defaultYes: false))
        {
            ResultText.Text = L.T("common.operationCancelled");
            return;
        }

        try
        {
            var record = await _services.DeploymentService.RollbackAsync(
                app.Name, env.Name, target.Name, release.Version, Environment.UserName);

            ResultText.Text = L.T("rollback.result.success", record.DeploymentId, release.Version);
        }
        catch (Exception ex)
        {
            ResultText.Text = L.T("rollback.result.failed", ex.Message);
        }
    }

    private (Domain.Entities.Application? App, Domain.Entities.AppEnvironment? Env, Domain.Entities.DeploymentTarget? Target, Domain.Entities.Release? Release) Selection() =>
        (Selector.SelectedApp, Selector.SelectedEnvironment, Selector.SelectedTarget, Selector.SelectedRelease);
}
