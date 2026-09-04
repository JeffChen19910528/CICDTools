using Avalonia.Controls;
using Deployment.CLI;
using Deployment.CLI.Localization;
using Deployment.Desktop.Services;
using Deployment.Domain.Entities;

namespace Deployment.Desktop.Views;

public partial class BackupsView : UserControl
{
    private readonly AppServices _services;
    private readonly Window _owner;
    private IReadOnlyList<Backup> _backups = [];

    public BackupsView() : this(null!, null!) { }

    public BackupsView(AppServices services, Window owner)
    {
        InitializeComponent();
        _services = services;
        _owner = owner;

        Header.Text = L.T("backup.menu.title");
        CreateBackupButton.Content = L.T("backup.menu.create");
        ProtectButton.Content = L.T("backup.menu.protect");
        CleanupButton.Content = L.T("backup.menu.cleanup");
        RefreshButton.Content = L.T("common.refresh");

        Grid.Columns[0].Header = L.T("backup.list.colId");
        Grid.Columns[1].Header = L.T("backup.list.colRelease");
        Grid.Columns[2].Header = L.T("backup.list.colCreated");
        Grid.Columns[3].Header = L.T("backup.list.colFiles");
        Grid.Columns[4].Header = L.T("backup.list.colSize");
        Grid.Columns[5].Header = L.T("backup.list.colStatus");
        Grid.Columns[6].Header = L.T("backup.list.colProtected");

        Selector.Configure(services, showRelease: false);
        Selector.SelectionChanged += async () => await RefreshAsync();
        Loaded += async (_, _) => await Selector.LoadAsync();
    }

    private async Task RefreshAsync()
    {
        var target = Selector.SelectedTarget;
        var app = Selector.SelectedApp;
        var env = Selector.SelectedEnvironment;
        if (app is null || env is null || target is null)
        {
            Grid.ItemsSource = null;
            _backups = [];
            return;
        }

        _backups = await _services.BackupService.ListBackupsAsync(app.Name, env.Name, target.Name);
        Grid.ItemsSource = _backups.Select(b => new BackupRow(
            b.BackupId,
            b.ReleaseVersion ?? "-",
            b.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
            b.FileCount?.ToString() ?? "-",
            b.TotalBytes.HasValue ? ByteFormatter.Format(b.TotalBytes.Value) : "-",
            b.Status.ToString(),
            b.IsProtected ? L.T("common.yes") : L.T("common.no"))).ToList();
    }

    private async void OnCreateBackupClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var app = Selector.SelectedApp;
        var env = Selector.SelectedEnvironment;
        var target = Selector.SelectedTarget;
        if (app is null || env is null || target is null) return;

        try
        {
            var backup = await _services.BackupService.CreateBackupAsync(
                app.Name, env.Name, target.Name, _services.Options.BackupStorePath, Environment.UserName);
            await RefreshAsync();
            await DialogService.ShowMessageAsync(_owner, L.T("backup.menu.create"),
                L.T("backup.create.success", backup.BackupId, backup.FileCount ?? 0));
        }
        catch (Exception ex)
        {
            await DialogService.ShowMessageAsync(_owner, L.T("backup.menu.create"), L.T("common.error", ex.Message), isError: true);
        }
    }

    private async void OnProtectClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not BackupRow row) return;

        var protect = !row.Protected.Equals(L.T("common.yes"));
        if (!await DialogService.ConfirmAsync(_owner, L.T("backup.menu.protect"), L.T("backup.protect.prompt"), defaultYes: protect))
            return;

        await _services.BackupService.ProtectBackupAsync(row.BackupId, protect);
        await RefreshAsync();
    }

    private async void OnCleanupClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var app = Selector.SelectedApp;
        var env = Selector.SelectedEnvironment;
        var target = Selector.SelectedTarget;
        if (app is null || env is null || target is null) return;

        if (!await DialogService.ConfirmAsync(_owner, L.T("backup.menu.cleanup"),
                L.T("backup.cleanup.confirm", app.Name, env.Name, target.Name), defaultYes: false))
            return;

        try
        {
            await _services.RetentionService.ApplyRetentionAsync(app.Name, env.Name, target.Name, Environment.UserName);
            await RefreshAsync();
            await DialogService.ShowMessageAsync(_owner, L.T("backup.menu.cleanup"), L.T("backup.cleanup.success"));
        }
        catch (Exception ex)
        {
            await DialogService.ShowMessageAsync(_owner, L.T("backup.menu.cleanup"), L.T("common.error", ex.Message), isError: true);
        }
    }

    private async void OnRefreshClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => await RefreshAsync();

    private sealed record BackupRow(string BackupId, string Release, string Created, string Files, string Size, string Status, string Protected);
}
