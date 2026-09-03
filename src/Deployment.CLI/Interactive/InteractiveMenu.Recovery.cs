using Deployment.CLI.Localization;
using Deployment.Domain.Entities;
using Spectre.Console;

namespace Deployment.CLI.Interactive;

public partial class InteractiveMenu
{
    private async Task RecoveryMenuAsync()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine($"[bold]{L.T("recovery.menu.title")}[/]\n");

            var back = L.T("common.back");
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>().Title(L.T("menu.prompt")).AddChoices(
                    L.T("recovery.menu.status"), L.T("recovery.menu.markFailed"), back));

            if (choice == back) return;
            if (choice == L.T("recovery.menu.status")) await ViewIncompleteDeploymentsAsync();
            else if (choice == L.T("recovery.menu.markFailed")) await MarkDeploymentFailedAsync();
        }
    }

    private async Task ViewIncompleteDeploymentsAsync()
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine($"[bold]{L.T("recovery.menu.status")}[/]\n");

        var incomplete = await deploymentService.GetIncompleteDeploymentsAsync();
        if (incomplete.Count == 0)
        {
            AnsiConsole.MarkupLine($"[green]{L.T("recovery.status.none")}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[yellow bold]{L.T("recovery.status.found", incomplete.Count)}[/]");

            var table = new Table()
                .AddColumn(L.T("history.deploy.colId"))
                .AddColumn(L.T("history.audit.colTarget"))
                .AddColumn(L.T("history.deploy.colRelease"))
                .AddColumn(L.T("history.deploy.colStatus"))
                .AddColumn(L.T("history.deploy.colStarted"))
                .AddColumn(L.T("history.deploy.colOperator"));

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
        }
        Pause();
    }

    private async Task MarkDeploymentFailedAsync()
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine($"[bold]{L.T("recovery.menu.markFailed")}[/]\n");

        var incomplete = await deploymentService.GetIncompleteDeploymentsAsync();
        if (incomplete.Count == 0)
        {
            AnsiConsole.MarkupLine($"[green]{L.T("recovery.status.none")}[/]");
            Pause();
            return;
        }

        var back = L.T("common.back");
        var deploymentId = AnsiConsole.Prompt(
            new SelectionPrompt<string>().Title(L.T("recovery.markFailed.idPrompt")).PageSize(10)
                .AddChoices(incomplete.Select(r => r.DeploymentId).Append(back)));
        if (deploymentId == back) return;

        var record = await deploymentRepo.GetByDeploymentIdAsync(deploymentId);
        if (record is null)
        {
            AnsiConsole.MarkupLine($"[red]{L.T("recovery.markFailed.notFound", deploymentId)}[/]");
            Pause();
            return;
        }

        record.Status = DeploymentStatus.Failed;
        record.FailureReason = "Manually marked as failed during recovery";
        record.CompletedAt = DateTime.UtcNow;
        await deploymentRepo.UpdateAsync(record);

        await auditService.RecordAsync(new AuditEvent
        {
            EventType = "DEPLOYMENT_MARKED_FAILED",
            DeploymentId = deploymentId,
            Operator = Environment.UserName,
            Timestamp = DateTime.UtcNow,
            Result = "MANUAL",
            Details = "Operator manually marked as failed during recovery"
        });

        AnsiConsole.MarkupLine($"[yellow]{L.T("recovery.markFailed.success", deploymentId)}[/]");
        Pause();
    }
}
