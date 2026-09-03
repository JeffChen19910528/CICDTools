using System.CommandLine;
using Deployment.Application.Interfaces;
using Deployment.Application.Services;
using Deployment.Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace Deployment.CLI.Commands;

public static class DeployCommands
{
    public static Command Build(IServiceProvider services)
    {
        var cmd = new Command("deploy", "Deploy or rollback a release");

        cmd.AddCommand(BuildDeploy(services));
        cmd.AddCommand(BuildRollback(services));
        cmd.AddCommand(BuildDiff(services));

        return cmd;
    }

    private static Command BuildDeploy(IServiceProvider services)
    {
        var appArg = new Argument<string>("app");
        var envArg = new Argument<string>("environment");
        var targetArg = new Argument<string>("target");
        var versionArg = new Argument<string>("version", "Release version to deploy");
        var operatorOpt = new Option<string>("--operator", () => Environment.UserName);
        var dryRunOpt = new Option<bool>("--dry-run", "Simulate deployment without modifying files");
        var yesOpt = new Option<bool>(["--yes", "-y"], "Skip confirmation prompt");

        var cmd = new Command("run", "Deploy a release to a target")
            { appArg, envArg, targetArg, versionArg, operatorOpt, dryRunOpt, yesOpt };

        cmd.SetHandler(async (app, env, target, version, op, dryRun, yes) =>
        {
            var svc = services.GetRequiredService<IDeploymentService>();

            if (!dryRun && !yes)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[yellow bold]WARNING:[/]");
                AnsiConsole.MarkupLine($"  You are about to deploy version [bold]{version}[/]");
                AnsiConsole.MarkupLine($"  to [bold]{app} / {env} / {target}[/].");

                if (!AnsiConsole.Confirm("A backup will be created before deployment. Continue?", defaultValue: false))
                {
                    AnsiConsole.MarkupLine("[grey]Deployment cancelled.[/]");
                    return;
                }
            }

            if (dryRun)
                AnsiConsole.MarkupLine("[yellow bold]DRY RUN — no files will be modified.[/]");

            await AnsiConsole.Progress()
                .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new SpinnerColumn())
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask(dryRun ? "Simulating deployment..." : "Deploying...");
                    try
                    {
                        var record = await svc.StartDeploymentAsync(app, env, target, version, op, dryRun);
                        task.Value = 100;

                        AnsiConsole.WriteLine();
                        if (dryRun)
                            AnsiConsole.MarkupLine($"[green]Dry run completed. No changes were applied.[/]");
                        else
                            AnsiConsole.MarkupLine($"[green bold]Deployment {record.DeploymentId} completed successfully![/]");

                        var table = new Table().AddColumn("Step").AddColumn("Status").AddColumn("Duration");
                        foreach (var step in record.Steps.OrderBy(s => s.StepNumber))
                        {
                            var duration = step.CompletedAt.HasValue && step.StartedAt.HasValue
                                ? $"{(step.CompletedAt.Value - step.StartedAt.Value).TotalSeconds:F1}s" : "-";
                            var status = step.Status == Domain.Entities.StepStatus.Success
                                ? "[green]✓[/]" : "[red]✗[/]";
                            table.AddRow(step.Name, status, duration);
                        }
                        AnsiConsole.Write(table);
                    }
                    catch (Exception ex)
                    {
                        task.Value = 100;
                        AnsiConsole.WriteLine();
                        AnsiConsole.MarkupLine($"[red bold]Deployment failed:[/] {Markup.Escape(ex.Message)}");
                    }
                });
        }, appArg, envArg, targetArg, versionArg, operatorOpt, dryRunOpt, yesOpt);

        return cmd;
    }

    private static Command BuildRollback(IServiceProvider services)
    {
        var appArg = new Argument<string>("app");
        var envArg = new Argument<string>("environment");
        var targetArg = new Argument<string>("target");
        var versionArg = new Argument<string>("version", "Version to roll back to");
        var operatorOpt = new Option<string>("--operator", () => Environment.UserName);
        var yesOpt = new Option<bool>(["--yes", "-y"], "Skip confirmation");

        var cmd = new Command("rollback", "Roll back to a previous release version")
            { appArg, envArg, targetArg, versionArg, operatorOpt, yesOpt };

        cmd.SetHandler(async (app, env, target, version, op, yes) =>
        {
            if (!yes)
            {
                AnsiConsole.MarkupLine($"[yellow bold]WARNING:[/] Rolling back [bold]{app}/{env}/{target}[/] to version [bold]{version}[/].");
                AnsiConsole.MarkupLine("A backup of the current state will be created before rollback.");
                if (!AnsiConsole.Confirm("Continue?", defaultValue: false))
                {
                    AnsiConsole.MarkupLine("[grey]Rollback cancelled.[/]");
                    return;
                }
            }

            var svc = services.GetRequiredService<IDeploymentService>();

            await AnsiConsole.Status().StartAsync("Rolling back...", async ctx =>
            {
                try
                {
                    var record = await svc.RollbackAsync(app, env, target, version, op);
                    AnsiConsole.MarkupLine($"[green bold]Rollback {record.DeploymentId} completed. Now at version {version}.[/]");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red bold]Rollback failed:[/] {Markup.Escape(ex.Message)}");
                }
            });
        }, appArg, envArg, targetArg, versionArg, operatorOpt, yesOpt);

        return cmd;
    }

    private static Command BuildDiff(IServiceProvider services)
    {
        var appArg = new Argument<string>("app");
        var envArg = new Argument<string>("environment");
        var targetArg = new Argument<string>("target");
        var versionArg = new Argument<string>("version", "Release version to diff against target");
        var showUnchangedOpt = new Option<bool>("--show-unchanged", "Include unchanged files in output");

        var cmd = new Command("diff", "Compare a release against the deployed target")
            { appArg, envArg, targetArg, versionArg, showUnchangedOpt };

        cmd.SetHandler(async (app, env, target, version, showUnchanged) =>
        {
            var releaseSvc = services.GetRequiredService<IReleaseService>();
            var diffSvc = services.GetRequiredService<IDiffService>();
            var resolver = services.GetRequiredService<ITargetResolver>();

            try
            {
                var (_, _, targetEntity) = await resolver.ResolveTargetAsync(app, env, target);
                var release = await releaseSvc.GetReleaseAsync($"{app}-{version}");

                var diff = await diffSvc.ComputeAsync(
                    release.PackagePath,
                    targetEntity.DeploymentPath,
                    version,
                    targetEntity.CurrentRelease ?? "EMPTY");

                AnsiConsole.MarkupLine($"[bold]Source:[/] {diff.SourceVersion}  [bold]Target:[/] {diff.TargetVersion}");
                AnsiConsole.MarkupLine($"[green]+ Added: {diff.Added}[/]  [yellow]~ Modified: {diff.Modified}[/]  [red]- Deleted: {diff.Deleted}[/]  = Unchanged: {diff.Unchanged}");
                AnsiConsole.WriteLine();

                foreach (var f in diff.Files)
                {
                    if (!showUnchanged && f.ChangeType == Domain.Models.FileChangeType.Unchanged) continue;

                    var (icon, color) = f.ChangeType switch
                    {
                        Domain.Models.FileChangeType.Added => ("[green]+[/]", "green"),
                        Domain.Models.FileChangeType.Modified => ("[yellow]~[/]", "yellow"),
                        Domain.Models.FileChangeType.Deleted => ("[red]-[/]", "red"),
                        _ => ("=", "grey")
                    };

                    AnsiConsole.MarkupLine($"{icon} [{color}]{Markup.Escape(f.RelativePath)}[/]");

                    if (f.ChangeType == Domain.Models.FileChangeType.Modified && f.LineDiffs != null && !f.IsBinary)
                    {
                        foreach (var line in f.LineDiffs.Where(l => l.Type != Domain.Models.LineDiffType.Context))
                        {
                            var prefix = line.Type == Domain.Models.LineDiffType.Added ? "[green]  +[/]" : "[red]  -[/]";
                            AnsiConsole.MarkupLine($"{prefix} {Markup.Escape(line.Content)}");
                        }
                    }
                    else if (f.IsBinary && f.ChangeType == Domain.Models.FileChangeType.Modified)
                    {
                        AnsiConsole.MarkupLine($"  [grey]Binary file differs. Old: {f.TargetSize?.ToString() ?? "?"} B  New: {f.SourceSize?.ToString() ?? "?"} B[/]");
                    }
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
            }
        }, appArg, envArg, targetArg, versionArg, showUnchangedOpt);

        return cmd;
    }
}
