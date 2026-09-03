using System.CommandLine;
using Deployment.Application.Interfaces;
using Deployment.Application.Services;
using Deployment.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace Deployment.CLI.Commands;

public static class HistoryCommands
{
    public static Command Build(IServiceProvider services)
    {
        var cmd = new Command("history", "View deployment history and audit log");

        cmd.AddCommand(BuildDeployments(services));
        cmd.AddCommand(BuildAudit(services));

        return cmd;
    }

    private static Command BuildDeployments(IServiceProvider services)
    {
        var appArg = new Argument<string>("app");
        var envArg = new Argument<string>("environment");
        var targetArg = new Argument<string>("target");
        var cmd = new Command("deployments", "List recent deployments for a target")
            { appArg, envArg, targetArg };

        cmd.SetHandler(async (app, env, target) =>
        {
            var svc = services.GetRequiredService<DeploymentService>();
            var records = await svc.GetHistoryAsync(app, env, target);

            if (!records.Any()) { AnsiConsole.MarkupLine("[grey]No deployments found.[/]"); return; }

            var table = new Table()
                .AddColumn("Deployment ID")
                .AddColumn("Release")
                .AddColumn("Status")
                .AddColumn("Operator")
                .AddColumn("Started")
                .AddColumn("Completed");

            foreach (var r in records)
            {
                var statusColor = r.Status switch
                {
                    DeploymentStatus.Completed => "green",
                    DeploymentStatus.RolledBack => "green",
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
        }, appArg, envArg, targetArg);

        return cmd;
    }

    private static Command BuildAudit(IServiceProvider services)
    {
        var appArg = new Argument<string?>("app") { Arity = ArgumentArity.ZeroOrOne };
        var envOpt = new Option<string?>("--environment");
        var limitOpt = new Option<int>("--limit", () => 50);
        var cmd = new Command("audit", "View audit events") { appArg, envOpt, limitOpt };

        cmd.SetHandler(async (app, env, limit) =>
        {
            var auditSvc = services.GetRequiredService<IAuditService>();
            var events = await auditSvc.GetRecentAsync(app, env, limit);

            if (!events.Any()) { AnsiConsole.MarkupLine("[grey]No audit events found.[/]"); return; }

            var table = new Table()
                .AddColumn("Timestamp")
                .AddColumn("Event")
                .AddColumn("App/Env/Target")
                .AddColumn("Operator")
                .AddColumn("Result")
                .AddColumn("Details");

            foreach (var e in events)
            {
                var resultColor = e.Result == "SUCCESS" || e.Result == "STARTED" ? "green"
                    : e.Result == "FAILED" ? "red" : "yellow";

                table.AddRow(
                    e.Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                    e.EventType,
                    $"{e.Application ?? "-"}/{e.Environment ?? "-"}/{e.Target ?? "-"}",
                    e.Operator ?? "-",
                    $"[{resultColor}]{e.Result}[/]",
                    Markup.Escape(e.Details ?? ""));
            }

            AnsiConsole.Write(table);
        }, appArg, envOpt, limitOpt);

        return cmd;
    }
}
