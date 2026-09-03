using System.CommandLine;
using Deployment.Application.Interfaces;
using Deployment.Application.Services;
using Deployment.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace Deployment.CLI.Commands;

public static class RecoveryCommands
{
    public static Command Build(IServiceProvider services)
    {
        var cmd = new Command("recovery", "Detect and recover from incomplete deployments");

        cmd.AddCommand(BuildStatus(services));
        cmd.AddCommand(BuildMarkFailed(services));

        return cmd;
    }

    private static Command BuildStatus(IServiceProvider services)
    {
        var cmd = new Command("status", "Show incomplete deployments that may need recovery");

        cmd.SetHandler(async () =>
        {
            var svc = services.GetRequiredService<IDeploymentService>();
            var incomplete = await svc.GetIncompleteDeploymentsAsync();

            if (!incomplete.Any())
            {
                AnsiConsole.MarkupLine("[green]No incomplete deployments found.[/]");
                return;
            }

            AnsiConsole.MarkupLine($"[yellow bold]Found {incomplete.Count} incomplete deployment(s):[/]");

            var table = new Table()
                .AddColumn("Deployment ID")
                .AddColumn("App/Env/Target")
                .AddColumn("Release")
                .AddColumn("Status")
                .AddColumn("Started")
                .AddColumn("Operator");

            foreach (var r in incomplete)
            {
                var target = r.Target;
                table.AddRow(
                    r.DeploymentId,
                    $"{target?.Environment?.Application?.Name ?? "?"}/{target?.Environment?.Name ?? "?"}/{target?.Name ?? "?"}",
                    r.Release?.Version ?? "-",
                    $"[yellow]{r.Status}[/]",
                    r.StartedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? r.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                    r.Operator);
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("Options: [bold]deployctl recovery mark-failed <deployment-id>[/]");
        });

        return cmd;
    }

    private static Command BuildMarkFailed(IServiceProvider services)
    {
        var deploymentIdArg = new Argument<string>("deployment-id");
        var cmd = new Command("mark-failed", "Mark an incomplete deployment as failed") { deploymentIdArg };

        cmd.SetHandler(async (deploymentId) =>
        {
            var repo = services.GetRequiredService<Deployment.Application.Interfaces.IDeploymentRepository>();
            var audit = services.GetRequiredService<Deployment.Application.Interfaces.IAuditService>();

            var record = await repo.GetByDeploymentIdAsync(deploymentId);
            if (record == null) { AnsiConsole.MarkupLine($"[red]Deployment '{deploymentId}' not found.[/]"); return; }

            record.Status = DeploymentStatus.Failed;
            record.FailureReason = "Manually marked as failed during recovery";
            record.CompletedAt = DateTime.UtcNow;
            await repo.UpdateAsync(record);

            await audit.RecordAsync(new Domain.Entities.AuditEvent
            {
                EventType = "DEPLOYMENT_MARKED_FAILED",
                DeploymentId = deploymentId,
                Operator = Environment.UserName,
                Timestamp = DateTime.UtcNow,
                Result = "MANUAL",
                Details = "Operator manually marked as failed during recovery"
            });

            AnsiConsole.MarkupLine($"[yellow]Deployment {deploymentId} marked as FAILED.[/]");
        }, deploymentIdArg);

        return cmd;
    }
}
