using AppEntity = Deployment.Domain.Entities.Application;
using Avalonia.Controls;
using Deployment.CLI.Localization;
using Deployment.Desktop.Services;
using Deployment.Domain.Entities;

namespace Deployment.Desktop.Views;

public partial class HistoryView : UserControl
{
    private readonly AppServices _services;

    public HistoryView() : this(null!, null!) { }

    public HistoryView(AppServices services, Window owner)
    {
        InitializeComponent();
        _services = services;

        Header.Text = L.T("history.menu.title");
        DeployTab.Header = L.T("history.menu.deployments");
        AuditTab.Header = L.T("history.menu.audit");
        AuditAppLabel.Text = L.T("select.app");
        AuditEnvLabel.Text = L.T("select.env");
        AuditRefreshButton.Content = L.T("common.refresh");

        DeployGrid.Columns[0].Header = L.T("history.deploy.colId");
        DeployGrid.Columns[1].Header = L.T("history.deploy.colRelease");
        DeployGrid.Columns[2].Header = L.T("history.deploy.colStatus");
        DeployGrid.Columns[3].Header = L.T("history.deploy.colOperator");
        DeployGrid.Columns[4].Header = L.T("history.deploy.colStarted");
        DeployGrid.Columns[5].Header = L.T("history.deploy.colCompleted");

        AuditGrid.Columns[0].Header = L.T("history.audit.colTime");
        AuditGrid.Columns[1].Header = L.T("history.audit.colEvent");
        AuditGrid.Columns[2].Header = L.T("history.audit.colTarget");
        AuditGrid.Columns[3].Header = L.T("history.audit.colOperator");
        AuditGrid.Columns[4].Header = L.T("history.audit.colResult");
        AuditGrid.Columns[5].Header = L.T("history.audit.colDetails");

        Selector.Configure(services, showRelease: false);
        Selector.SelectionChanged += async () => await RefreshDeploymentHistoryAsync();

        Loaded += async (_, _) =>
        {
            await Selector.LoadAsync();
            await LoadAuditAppsAsync();
        };
    }

    private async Task RefreshDeploymentHistoryAsync()
    {
        var app = Selector.SelectedApp;
        var env = Selector.SelectedEnvironment;
        var target = Selector.SelectedTarget;
        if (app is null || env is null || target is null)
        {
            DeployGrid.ItemsSource = null;
            return;
        }

        var records = await _services.DeploymentService.GetHistoryAsync(app.Name, env.Name, target.Name);
        DeployGrid.ItemsSource = records.Select(r => new DeployRow(
            r.DeploymentId,
            r.Release?.Version ?? "-",
            r.Status.ToString(),
            r.Operator,
            r.StartedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "-",
            r.CompletedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "-")).ToList();
    }

    private async Task LoadAuditAppsAsync()
    {
        var apps = await _services.AppRepo.ListAsync();
        var appItems = new List<ComboItem<AppEntity?>> { new(L.T("common.all"), null) };
        appItems.AddRange(apps.Select(a => new ComboItem<AppEntity?>(a.Name, a)));
        AuditAppCombo.ItemsSource = appItems;
        AuditAppCombo.SelectedIndex = 0;

        await RefreshAuditAsync();
    }

    private void OnAuditFilterChanged(object? sender, SelectionChangedEventArgs e)
    {
        var app = (AuditAppCombo.SelectedItem as ComboItem<AppEntity?>)?.Value;
        if (sender == AuditAppCombo)
        {
            var envItems = new List<ComboItem<AppEnvironment?>> { new(L.T("common.all"), null) };
            envItems.AddRange(app?.Environments.Select(x => new ComboItem<AppEnvironment?>(x.Name, x)) ?? []);
            AuditEnvCombo.ItemsSource = envItems;
            AuditEnvCombo.SelectedIndex = 0;
        }

        _ = RefreshAuditAsync();
    }

    private async void OnAuditRefreshClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => await RefreshAuditAsync();

    private async Task RefreshAuditAsync()
    {
        var app = (AuditAppCombo.SelectedItem as ComboItem<AppEntity?>)?.Value;
        var env = (AuditEnvCombo.SelectedItem as ComboItem<AppEnvironment?>)?.Value;

        var events = await _services.AuditService.GetRecentAsync(app?.Name, env?.Name, 50);
        AuditGrid.ItemsSource = events.Select(e => new AuditRow(
            e.Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
            e.EventType,
            $"{e.Application ?? "-"}/{e.Environment ?? "-"}/{e.Target ?? "-"}",
            e.Operator ?? "-",
            e.Result,
            e.Details ?? "")).ToList();
    }

    private sealed record DeployRow(string DeploymentId, string Release, string Status, string Operator, string Started, string Completed);
    private sealed record AuditRow(string Timestamp, string EventType, string Target, string Operator, string Result, string Details);
}
