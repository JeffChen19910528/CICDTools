using AppEntity = Deployment.Domain.Entities.Application;
using Avalonia.Controls;
using Deployment.CLI.Localization;
using Deployment.Desktop.Services;
using Deployment.Domain.Entities;

namespace Deployment.Desktop.Views;

/// <summary>
/// Cascading AppEntity -> Environment -> Target (-> Release) picker, replacing InteractiveMenu's
/// SelectApplicationAsync/SelectEnvironment/SelectTarget/SelectReleaseAsync chain with combo boxes.
/// </summary>
public partial class TargetSelector : UserControl
{
    private AppServices? _services;
    private bool _showRelease;

    public event Action? SelectionChanged;

    public AppEntity? SelectedApp { get; private set; }
    public AppEnvironment? SelectedEnvironment { get; private set; }
    public DeploymentTarget? SelectedTarget { get; private set; }
    public Release? SelectedRelease { get; private set; }

    public TargetSelector() => InitializeComponent();

    public void Configure(AppServices services, bool showRelease)
    {
        _services = services;
        _showRelease = showRelease;
        ReleasePanel.IsVisible = showRelease;
        ApplyLabels();
    }

    private void ApplyLabels()
    {
        AppLabel.Text = L.T("select.app");
        EnvLabel.Text = L.T("select.env");
        TargetLabel.Text = L.T("select.target");
        ReleaseLabel.Text = L.T("select.release");
    }

    public async Task LoadAsync()
    {
        if (_services is null) return;
        ApplyLabels();

        var apps = await _services.AppRepo.ListAsync();
        AppCombo.ItemsSource = apps.Select(a => new ComboItem<AppEntity>(a.Name, a)).ToList();
        AppCombo.SelectedIndex = apps.Count > 0 ? 0 : -1;
    }

    private void OnAppChanged(object? sender, SelectionChangedEventArgs e)
    {
        SelectedApp = (AppCombo.SelectedItem as ComboItem<AppEntity>)?.Value;

        var envs = SelectedApp?.Environments.ToList() ?? [];
        EnvCombo.ItemsSource = envs.Select(x => new ComboItem<AppEnvironment>(x.Name, x)).ToList();
        EnvCombo.SelectedIndex = envs.Count > 0 ? 0 : -1;

        if (_showRelease) _ = LoadReleasesAsync();
    }

    private void OnEnvChanged(object? sender, SelectionChangedEventArgs e)
    {
        SelectedEnvironment = (EnvCombo.SelectedItem as ComboItem<AppEnvironment>)?.Value;

        var targets = SelectedEnvironment?.Targets.ToList() ?? [];
        TargetCombo.ItemsSource = targets.Select(x => new ComboItem<DeploymentTarget>(x.Name, x)).ToList();
        TargetCombo.SelectedIndex = targets.Count > 0 ? 0 : -1;
    }

    private void OnTargetChanged(object? sender, SelectionChangedEventArgs e)
    {
        SelectedTarget = (TargetCombo.SelectedItem as ComboItem<DeploymentTarget>)?.Value;
        SelectionChanged?.Invoke();
    }

    private async Task LoadReleasesAsync()
    {
        if (_services is null || SelectedApp is null)
        {
            ReleaseCombo.ItemsSource = null;
            return;
        }

        var releases = await _services.ReleaseService.ListReleasesAsync(SelectedApp.Name);
        ReleaseCombo.ItemsSource = releases
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ComboItem<Release>(r.Version, r))
            .ToList();
        ReleaseCombo.SelectedIndex = releases.Count > 0 ? 0 : -1;
    }

    private void OnReleaseChanged(object? sender, SelectionChangedEventArgs e)
    {
        SelectedRelease = (ReleaseCombo.SelectedItem as ComboItem<Release>)?.Value;
        SelectionChanged?.Invoke();
    }
}
