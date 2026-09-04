using Avalonia.Controls;
using Deployment.CLI.Localization;
using Deployment.Desktop.Services;
using Deployment.Domain.Entities;

namespace Deployment.Desktop.Views;

public partial class RecoveryView : UserControl
{
    private readonly AppServices _services;
    private readonly Window _owner;
    private IReadOnlyList<DeploymentRecord> _incomplete = [];

    public RecoveryView() : this(null!, null!) { }

    public RecoveryView(AppServices services, Window owner)
    {
        InitializeComponent();
        _services = services;
        _owner = owner;

        Header.Text = L.T("recovery.menu.title");
        MarkFailedButton.Content = L.T("recovery.menu.markFailed");
        RefreshButton.Content = L.T("common.refresh");

        Grid.Columns[0].Header = L.T("history.deploy.colId");
        Grid.Columns[1].Header = L.T("history.audit.colTarget");
        Grid.Columns[2].Header = L.T("history.deploy.colRelease");
        Grid.Columns[3].Header = L.T("history.deploy.colStatus");
        Grid.Columns[4].Header = L.T("history.deploy.colStarted");
        Grid.Columns[5].Header = L.T("history.deploy.colOperator");

        Loaded += async (_, _) => await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        _incomplete = await _services.DeploymentService.GetIncompleteDeploymentsAsync();
        EmptyText.Text = _incomplete.Count == 0 ? L.T("recovery.status.none") : L.T("recovery.status.found", _incomplete.Count);

        Grid.ItemsSource = _incomplete.Select(r => new RecoveryRow(
            r.DeploymentId,
            $"{r.Target?.Environment?.Application?.Name ?? "?"}/{r.Target?.Environment?.Name ?? "?"}/{r.Target?.Name ?? "?"}",
            r.Release?.Version ?? "-",
            r.Status.ToString(),
            r.StartedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? r.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
            r.Operator)).ToList();
    }

    private async void OnMarkFailedClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not RecoveryRow row) return;

        if (!await DialogService.ConfirmAsync(_owner, L.T("recovery.menu.markFailed"), L.T("common.confirmContinue"), defaultYes: false))
            return;

        var record = await _services.DeploymentRepo.GetByDeploymentIdAsync(row.DeploymentId);
        if (record is null)
        {
            await DialogService.ShowMessageAsync(_owner, L.T("recovery.menu.markFailed"), L.T("recovery.markFailed.notFound", row.DeploymentId), isError: true);
            return;
        }

        record.Status = DeploymentStatus.Failed;
        record.FailureReason = "Manually marked as failed during recovery";
        record.CompletedAt = DateTime.UtcNow;
        await _services.DeploymentRepo.UpdateAsync(record);

        await _services.AuditService.RecordAsync(new AuditEvent
        {
            EventType = "DEPLOYMENT_MARKED_FAILED",
            DeploymentId = row.DeploymentId,
            Operator = Environment.UserName,
            Timestamp = DateTime.UtcNow,
            Result = "MANUAL",
            Details = "Operator manually marked as failed during recovery"
        });

        await RefreshAsync();
        await DialogService.ShowMessageAsync(_owner, L.T("recovery.menu.markFailed"), L.T("recovery.markFailed.success", row.DeploymentId));
    }

    private async void OnRefreshClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => await RefreshAsync();

    private sealed record RecoveryRow(string DeploymentId, string Target, string Release, string Status, string Started, string Operator);
}
