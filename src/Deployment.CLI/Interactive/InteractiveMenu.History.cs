using Deployment.CLI.Localization;
using Deployment.Domain.Entities;
using Spectre.Console;

namespace Deployment.CLI.Interactive;

public partial class InteractiveMenu
{
    private async Task HistoryMenuAsync()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine($"[bold]{L.T("history.menu.title")}[/]\n");

            var back = L.T("common.back");
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>().Title(L.T("menu.prompt")).AddChoices(
                    L.T("history.menu.deployments"), L.T("history.menu.audit"), back));

            if (choice == back) return;
            if (choice == L.T("history.menu.deployments")) await ViewDeploymentHistoryAsync();
            else if (choice == L.T("history.menu.audit")) await ViewAuditLogAsync();
        }
    }

    private async Task ViewDeploymentHistoryAsync()
    {
        AnsiConsole.Clear();
        var chain = await SelectTargetChainAsync();
        if (chain is null) return;
        var (app, env, target) = chain.Value;

        AnsiConsole.Clear();
        AnsiConsole.MarkupLine($"[bold]{L.T("history.deploy.title", app.Name, env.Name, target.Name)}[/]\n");

        var records = await deploymentService.GetHistoryAsync(app.Name, env.Name, target.Name);
        if (records.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]-[/]");
        }
        else
        {
            var table = new Table()
                .AddColumn(L.T("history.deploy.colId"))
                .AddColumn(L.T("history.deploy.colRelease"))
                .AddColumn(L.T("history.deploy.colStatus"))
                .AddColumn(L.T("history.deploy.colOperator"))
                .AddColumn(L.T("history.deploy.colStarted"))
                .AddColumn(L.T("history.deploy.colCompleted"));

            foreach (var r in records)
            {
                var statusColor = r.Status switch
                {
                    DeploymentStatus.Completed or DeploymentStatus.RolledBack => "green",
                    DeploymentStatus.Failed => "red",
                    DeploymentStatus.Cancelled => "grey",
                    _ => "yellow"
                };
                table.AddRow(
                    r.DeploymentId,
                    r.Release?.Version ?? "-",
                    $"[{statusColor}]{r.Status}[/]",
                    r.Operator,
                    r.StartedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "-",
                    r.CompletedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "-");
            }
            AnsiConsole.Write(table);
        }
        Pause();
    }

    private async Task ViewAuditLogAsync()
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine($"[bold]{L.T("history.audit.title")}[/]\n");

        var appFilter = AnsiConsole.Prompt(new TextPrompt<string>(L.T("history.audit.filterPrompt")).AllowEmpty());
        var events = await auditService.GetRecentAsync(string.IsNullOrWhiteSpace(appFilter) ? null : appFilter, null, 50);

        AnsiConsole.Clear();
        AnsiConsole.MarkupLine($"[bold]{L.T("history.audit.title")}[/]\n");

        if (events.Count == 0)
        {
            AnsiConsole.MarkupLine($"[grey]{L.T("history.audit.none")}[/]");
        }
        else
        {
            var table = new Table()
                .AddColumn(L.T("history.audit.colTime"))
                .AddColumn(L.T("history.audit.colEvent"))
                .AddColumn(L.T("history.audit.colTarget"))
                .AddColumn(L.T("history.audit.colOperator"))
                .AddColumn(L.T("history.audit.colResult"))
                .AddColumn(L.T("history.audit.colDetails"));

            foreach (var e in events)
            {
                var resultColor = e.Result is "SUCCESS" or "STARTED" ? "green" : e.Result == "FAILED" ? "red" : "yellow";
                table.AddRow(
                    e.Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                    e.EventType,
                    $"{e.Application ?? "-"}/{e.Environment ?? "-"}/{e.Target ?? "-"}",
                    e.Operator ?? "-",
                    $"[{resultColor}]{e.Result}[/]",
                    Markup.Escape(e.Details ?? ""));
            }
            AnsiConsole.Write(table);
        }
        Pause();
    }
}
