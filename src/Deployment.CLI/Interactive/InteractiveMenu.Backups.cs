using Deployment.CLI.Localization;
using Deployment.Domain.Entities;
using Spectre.Console;

namespace Deployment.CLI.Interactive;

public partial class InteractiveMenu
{
    private async Task BackupsMenuAsync()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine($"[bold]{L.T("backup.menu.title")}[/]\n");

            var back = L.T("common.back");
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>().Title(L.T("menu.prompt")).AddChoices(
                    L.T("backup.menu.create"), L.T("backup.menu.list"),
                    L.T("backup.menu.protect"), L.T("backup.menu.cleanup"), back));

            if (choice == back) return;
            if (choice == L.T("backup.menu.create")) await CreateBackupAsync();
            else if (choice == L.T("backup.menu.list")) await ListBackupsAsync();
            else if (choice == L.T("backup.menu.protect")) await ProtectBackupAsync();
            else if (choice == L.T("backup.menu.cleanup")) await CleanupBackupsAsync();
        }
    }

    private async Task CreateBackupAsync()
    {
        AnsiConsole.Clear();
        var chain = await SelectTargetChainAsync();
        if (chain is null) return;
        var (app, env, target) = chain.Value;

        try
        {
            var backup = await AnsiConsole.Status().StartAsync(L.T("backup.menu.create"), async _ =>
                await backupService.CreateBackupAsync(app.Name, env.Name, target.Name, options.BackupStorePath, Environment.UserName));

            AnsiConsole.MarkupLine($"[green]{L.T("backup.create.success", backup.BackupId, backup.FileCount ?? 0)}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]{L.T("common.error", Markup.Escape(ex.Message))}[/]");
        }
        Pause();
    }

    private async Task ListBackupsAsync()
    {
        AnsiConsole.Clear();
        var chain = await SelectTargetChainAsync();
        if (chain is null) return;
        var (app, env, target) = chain.Value;

        AnsiConsole.Clear();
        AnsiConsole.MarkupLine($"[bold]{L.T("backup.list.title", app.Name, env.Name, target.Name)}[/]\n");

        var backups = await backupService.ListBackupsAsync(app.Name, env.Name, target.Name);
        PrintBackupTable(backups);
        Pause();
    }

    private static void PrintBackupTable(IReadOnlyList<Backup> backups)
    {
        if (backups.Count == 0)
        {
            AnsiConsole.MarkupLine($"[grey]{L.T("common.noBackupsYet")}[/]");
            return;
        }

        var table = new Table()
            .AddColumn(L.T("backup.list.colId"))
            .AddColumn(L.T("backup.list.colRelease"))
            .AddColumn(L.T("backup.list.colCreated"))
            .AddColumn(L.T("backup.list.colFiles"))
            .AddColumn(L.T("backup.list.colSize"))
            .AddColumn(L.T("backup.list.colStatus"))
            .AddColumn(L.T("backup.list.colProtected"));

        foreach (var b in backups)
        {
            var statusColor = b.Status switch
            {
                BackupStatus.Verified => "green",
                BackupStatus.Failed => "red",
                BackupStatus.Deleted => "grey",
                _ => "yellow"
            };
            table.AddRow(
                b.BackupId,
                b.ReleaseVersion ?? "-",
                b.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                b.FileCount?.ToString() ?? "-",
                b.TotalBytes.HasValue ? ByteFormatter.Format(b.TotalBytes.Value) : "-",
                $"[{statusColor}]{b.Status}[/]",
                b.IsProtected ? $"[yellow]{L.T("common.yes")}[/]" : L.T("common.no"));
        }
        AnsiConsole.Write(table);
    }

    private async Task ProtectBackupAsync()
    {
        AnsiConsole.Clear();
        var chain = await SelectTargetChainAsync();
        if (chain is null) return;
        var (app, env, target) = chain.Value;

        var backups = await backupService.ListBackupsAsync(app.Name, env.Name, target.Name);
        if (backups.Count == 0)
        {
            AnsiConsole.MarkupLine($"[grey]{L.T("common.noBackupsYet")}[/]");
            Pause();
            return;
        }

        var back = L.T("common.back");
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>().Title(L.T("select.backup")).PageSize(10)
                .AddChoices(backups.Select(b => b.BackupId).Append(back)));
        if (choice == back) return;

        var protect = AnsiConsole.Confirm(L.T("backup.protect.prompt"), true);
        await backupService.ProtectBackupAsync(choice, protect);

        AnsiConsole.MarkupLine(protect
            ? $"[green]{L.T("backup.protect.protectedMsg", choice)}[/]"
            : $"[yellow]{L.T("backup.protect.unprotectedMsg", choice)}[/]");
        Pause();
    }

    private async Task CleanupBackupsAsync()
    {
        AnsiConsole.Clear();
        var chain = await SelectTargetChainAsync();
        if (chain is null) return;
        var (app, env, target) = chain.Value;

        if (!AnsiConsole.Confirm(L.T("backup.cleanup.confirm", app.Name, env.Name, target.Name), false))
        {
            AnsiConsole.MarkupLine($"[grey]{L.T("common.operationCancelled")}[/]");
            Pause();
            return;
        }

        try
        {
            await AnsiConsole.Status().StartAsync(L.T("backup.menu.cleanup"), async _ =>
                await retentionService.ApplyRetentionAsync(app.Name, env.Name, target.Name, Environment.UserName));

            AnsiConsole.MarkupLine($"[green]{L.T("backup.cleanup.success")}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]{L.T("common.error", Markup.Escape(ex.Message))}[/]");
        }
        Pause();
    }
}
