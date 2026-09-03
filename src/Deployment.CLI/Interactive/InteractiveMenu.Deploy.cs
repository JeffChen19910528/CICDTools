using Deployment.CLI.Localization;
using Deployment.Domain.Models;
using Spectre.Console;

namespace Deployment.CLI.Interactive;

public partial class InteractiveMenu
{
    private async Task DeployMenuAsync()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine($"[bold]{L.T("deploy.menu.title")}[/]\n");

            var back = L.T("common.back");
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>().Title(L.T("menu.prompt")).AddChoices(
                    L.T("deploy.menu.diff"), L.T("deploy.menu.dryrun"),
                    L.T("deploy.menu.run"), L.T("deploy.menu.rollback"), back));

            if (choice == back) return;
            if (choice == L.T("deploy.menu.diff")) await ViewDiffAsync();
            else if (choice == L.T("deploy.menu.dryrun")) await RunDeploymentAsync(isDryRun: true);
            else if (choice == L.T("deploy.menu.run")) await RunDeploymentAsync(isDryRun: false);
            else if (choice == L.T("deploy.menu.rollback")) await RollbackAsync();
        }
    }

    private async Task ViewDiffAsync()
    {
        AnsiConsole.Clear();
        var chain = await SelectTargetChainAsync();
        if (chain is null) return;
        var (app, env, target) = chain.Value;

        var release = await SelectReleaseAsync(app.Name);
        if (release is null) return;

        var diff = await diffService.ComputeAsync(
            release.PackagePath, target.DeploymentPath, release.Version, target.CurrentRelease ?? "EMPTY");

        PrintDiff(diff);
        Pause();
    }

    private void PrintDiff(DiffResult diff)
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine(L.T("deploy.diff.summary", diff.SourceVersion, diff.TargetVersion));
        AnsiConsole.MarkupLine($"[green]{L.T("deploy.diff.counts", diff.Added, diff.Modified, diff.Deleted, diff.Unchanged)}[/]");
        AnsiConsole.WriteLine();

        foreach (var f in diff.Files.Where(f => f.ChangeType != FileChangeType.Unchanged))
        {
            var (icon, color) = f.ChangeType switch
            {
                FileChangeType.Added => ("+", "green"),
                FileChangeType.Modified => ("~", "yellow"),
                FileChangeType.Deleted => ("-", "red"),
                _ => ("=", "grey")
            };
            AnsiConsole.MarkupLine($"[{color}]{icon} {Markup.Escape(f.RelativePath)}[/]");
        }
    }

    private async Task RunDeploymentAsync(bool isDryRun)
    {
        AnsiConsole.Clear();
        var chain = await SelectTargetChainAsync();
        if (chain is null) return;
        var (app, env, target) = chain.Value;

        var release = await SelectReleaseAsync(app.Name);
        if (release is null) return;

        if (!isDryRun)
        {
            AnsiConsole.MarkupLine($"[yellow bold]{L.T("deploy.confirm.warning", release.Version, app.Name, env.Name, target.Name)}[/]");
            if (!AnsiConsole.Confirm(L.T("deploy.confirm.backupNote"), false))
            {
                AnsiConsole.MarkupLine($"[grey]{L.T("common.operationCancelled")}[/]");
                Pause();
                return;
            }
        }

        try
        {
            var record = await AnsiConsole.Status().StartAsync(L.T("deploy.menu.run"), async _ =>
                await deploymentService.StartDeploymentAsync(
                    app.Name, env.Name, target.Name, release.Version, Environment.UserName, isDryRun));

            AnsiConsole.MarkupLine(isDryRun
                ? $"[green]{L.T("deploy.result.dryrunDone")}[/]"
                : $"[green bold]{L.T("deploy.result.success", record.DeploymentId)}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red bold]{L.T("deploy.result.failed", Markup.Escape(ex.Message))}[/]");
        }
        Pause();
    }

    private async Task RollbackAsync()
    {
        AnsiConsole.Clear();
        var chain = await SelectTargetChainAsync();
        if (chain is null) return;
        var (app, env, target) = chain.Value;

        var release = await SelectReleaseAsync(app.Name);
        if (release is null) return;

        AnsiConsole.MarkupLine($"[yellow bold]{L.T("rollback.confirm.warning", app.Name, env.Name, target.Name, release.Version)}[/]");
        if (!AnsiConsole.Confirm(L.T("common.confirmContinue"), false))
        {
            AnsiConsole.MarkupLine($"[grey]{L.T("common.operationCancelled")}[/]");
            Pause();
            return;
        }

        try
        {
            var record = await AnsiConsole.Status().StartAsync(L.T("deploy.menu.rollback"), async _ =>
                await deploymentService.RollbackAsync(app.Name, env.Name, target.Name, release.Version, Environment.UserName));

            AnsiConsole.MarkupLine($"[green bold]{L.T("rollback.result.success", record.DeploymentId, release.Version)}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red bold]{L.T("rollback.result.failed", Markup.Escape(ex.Message))}[/]");
        }
        Pause();
    }
}
