using System.CommandLine;
using Deployment.Application.Interfaces;
using Deployment.Application.Services;
using Deployment.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace Deployment.CLI.Commands;

public static class BackupCommands
{
    public static Command Build(IServiceProvider services)
    {
        var cmd = new Command("backup", "Manage backups");

        cmd.AddCommand(BuildCreate(services));
        cmd.AddCommand(BuildList(services));
        cmd.AddCommand(BuildCleanup(services));
        cmd.AddCommand(BuildProtect(services));

        return cmd;
    }

    private static Command BuildCreate(IServiceProvider services)
    {
        var appArg = new Argument<string>("app");
        var envArg = new Argument<string>("environment");
        var targetArg = new Argument<string>("target");
        var operatorOpt = new Option<string>("--operator", () => Environment.UserName);
        var cmd = new Command("create", "Create a backup of the current deployment state")
            { appArg, envArg, targetArg, operatorOpt };

        cmd.SetHandler(async (app, env, target, op) =>
        {
            var svc = services.GetRequiredService<IBackupService>();
            var opts = services.GetRequiredService<DeploymentOptions>();

            await AnsiConsole.Status().StartAsync("Creating backup...", async ctx =>
            {
                try
                {
                    var backup = await svc.CreateBackupAsync(app, env, target, opts.BackupStorePath, op);
                    AnsiConsole.MarkupLine($"[green]Backup [bold]{backup.BackupId}[/] created and verified ({backup.FileCount} files).[/]");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
                }
            });
        }, appArg, envArg, targetArg, operatorOpt);

        return cmd;
    }

    private static Command BuildList(IServiceProvider services)
    {
        var appArg = new Argument<string>("app");
        var envArg = new Argument<string>("environment");
        var targetArg = new Argument<string>("target");
        var cmd = new Command("list", "List backups") { appArg, envArg, targetArg };

        cmd.SetHandler(async (app, env, target) =>
        {
            var svc = services.GetRequiredService<IBackupService>();
            var backups = await svc.ListBackupsAsync(app, env, target);

            if (!backups.Any()) { AnsiConsole.MarkupLine("[grey]No backups found.[/]"); return; }

            var table = new Table()
                .AddColumn("Backup ID")
                .AddColumn("Release")
                .AddColumn("Created")
                .AddColumn("Files")
                .AddColumn("Size")
                .AddColumn("Status")
                .AddColumn("Protected");

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
                    b.IsProtected ? "[yellow]PROTECTED[/]" : "");
            }

            AnsiConsole.Write(table);
        }, appArg, envArg, targetArg);

        return cmd;
    }

    private static Command BuildCleanup(IServiceProvider services)
    {
        var appArg = new Argument<string>("app");
        var envArg = new Argument<string>("environment");
        var targetArg = new Argument<string>("target");
        var operatorOpt = new Option<string>("--operator", () => Environment.UserName);
        var cmd = new Command("cleanup", "Apply retention policy and delete old backups")
            { appArg, envArg, targetArg, operatorOpt };

        cmd.SetHandler(async (app, env, target, op) =>
        {
            var svc = services.GetRequiredService<IRetentionService>();
            await AnsiConsole.Status().StartAsync("Applying retention policy...", async ctx =>
            {
                try
                {
                    await svc.ApplyRetentionAsync(app, env, target, op);
                    AnsiConsole.MarkupLine("[green]Retention cleanup complete.[/]");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
                }
            });
        }, appArg, envArg, targetArg, operatorOpt);

        return cmd;
    }

    private static Command BuildProtect(IServiceProvider services)
    {
        var backupIdArg = new Argument<string>("backup-id");
        var protectOpt = new Option<bool>("--protect", () => true, "Set protected status");
        var cmd = new Command("protect", "Mark or unmark a backup as protected") { backupIdArg, protectOpt };

        cmd.SetHandler(async (backupId, protect) =>
        {
            var svc = services.GetRequiredService<IBackupService>();
            await svc.ProtectBackupAsync(backupId, protect);
            AnsiConsole.MarkupLine(protect
                ? $"[green]Backup {backupId} is now PROTECTED.[/]"
                : $"[yellow]Backup {backupId} protection removed.[/]");
        }, backupIdArg, protectOpt);

        return cmd;
    }
}
