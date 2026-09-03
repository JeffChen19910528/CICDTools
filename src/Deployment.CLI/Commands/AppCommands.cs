using System.CommandLine;
using Deployment.Application.Interfaces;
using Deployment.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace Deployment.CLI.Commands;

public static class AppCommands
{
    public static Command Build(IServiceProvider services)
    {
        var cmd = new Command("app", "Manage applications, environments, and targets");

        cmd.AddCommand(BuildCreate(services));
        cmd.AddCommand(BuildAddEnv(services));
        cmd.AddCommand(BuildAddTarget(services));
        cmd.AddCommand(BuildList(services));

        return cmd;
    }

    private static Command BuildCreate(IServiceProvider services)
    {
        var nameArg = new Argument<string>("name", "Application name");
        var descOpt = new Option<string>("--description", () => "", "Description");
        var cmd = new Command("create", "Create a new application") { nameArg, descOpt };

        cmd.SetHandler(async (name, desc) =>
        {
            var repo = services.GetRequiredService<IApplicationRepository>();
            var existing = await repo.GetByNameAsync(name);
            if (existing != null)
            {
                AnsiConsole.MarkupLine($"[red]Application '{name}' already exists.[/]");
                return;
            }

            await repo.AddAsync(new Domain.Entities.Application { Name = name, Description = desc });
            AnsiConsole.MarkupLine($"[green]Application '[bold]{name}[/]' created.[/]");
        }, nameArg, descOpt);

        return cmd;
    }

    private static Command BuildAddEnv(IServiceProvider services)
    {
        var appArg = new Argument<string>("app", "Application name");
        var envArg = new Argument<string>("environment", "Environment name");
        var requireApprovalOpt = new Option<bool>("--require-approval", "Require approval for deployments");
        var cmd = new Command("add-env", "Add an environment to an application") { appArg, envArg, requireApprovalOpt };

        cmd.SetHandler(async (app, env, requireApproval) =>
        {
            var repo = services.GetRequiredService<IApplicationRepository>();
            var appEntity = await repo.GetByNameAsync(app);
            if (appEntity == null) { AnsiConsole.MarkupLine($"[red]Application '{app}' not found.[/]"); return; }

            var existing = await repo.GetEnvironmentAsync(appEntity.Id, env);
            if (existing != null) { AnsiConsole.MarkupLine($"[yellow]Environment '{env}' already exists.[/]"); return; }

            await repo.AddEnvironmentAsync(new AppEnvironment
            {
                ApplicationId = appEntity.Id,
                Name = env,
                RequireApproval = requireApproval
            });
            AnsiConsole.MarkupLine($"[green]Environment '[bold]{env}[/]' added to '{app}'.[/]");
        }, appArg, envArg, requireApprovalOpt);

        return cmd;
    }

    private static Command BuildAddTarget(IServiceProvider services)
    {
        var appArg = new Argument<string>("app");
        var envArg = new Argument<string>("environment");
        var nameArg = new Argument<string>("target", "Target name");
        var pathArg = new Argument<string>("path", "Deployment path on target");
        var osOpt = new Option<string>("--os", () => "windows", "Target OS (windows|linux)");
        var hostOpt = new Option<string>("--host", () => "localhost", "Host name");

        var cmd = new Command("add-target", "Add a deployment target") { appArg, envArg, nameArg, pathArg, osOpt, hostOpt };

        cmd.SetHandler(async (app, env, name, path, os, host) =>
        {
            var repo = services.GetRequiredService<IApplicationRepository>();
            var appEntity = await repo.GetByNameAsync(app);
            if (appEntity == null) { AnsiConsole.MarkupLine($"[red]Application '{app}' not found.[/]"); return; }

            var envEntity = await repo.GetEnvironmentAsync(appEntity.Id, env);
            if (envEntity == null) { AnsiConsole.MarkupLine($"[red]Environment '{env}' not found.[/]"); return; }

            var targetOs = os.ToLowerInvariant() == "linux" ? TargetOS.Linux : TargetOS.Windows;
            await repo.AddTargetAsync(new DeploymentTarget
            {
                EnvironmentId = envEntity.Id,
                Name = name,
                OS = targetOs,
                Host = host,
                DeploymentPath = path
            });
            AnsiConsole.MarkupLine($"[green]Target '[bold]{name}[/]' ({os}) added to {app}/{env} → {path}[/]");
        }, appArg, envArg, nameArg, pathArg, osOpt, hostOpt);

        return cmd;
    }

    private static Command BuildList(IServiceProvider services)
    {
        var cmd = new Command("list", "List all applications");

        cmd.SetHandler(async () =>
        {
            var repo = services.GetRequiredService<IApplicationRepository>();
            var apps = await repo.ListAsync();

            if (!apps.Any()) { AnsiConsole.MarkupLine("[grey]No applications registered.[/]"); return; }

            var table = new Table().AddColumn("Application").AddColumn("Environments").AddColumn("Targets");
            foreach (var a in apps)
            {
                var envNames = string.Join(", ", a.Environments.Select(e =>
                    $"{e.Name} ({string.Join(", ", e.Targets.Select(t => t.Name))})"));
                table.AddRow(a.Name, string.Join(", ", a.Environments.Select(e => e.Name)), envNames);
            }
            AnsiConsole.Write(table);
        });

        return cmd;
    }
}
