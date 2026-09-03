using App = Deployment.Domain.Entities.Application;
using Deployment.CLI.Localization;
using Deployment.Domain.Entities;
using Spectre.Console;

namespace Deployment.CLI.Interactive;

public partial class InteractiveMenu
{
    private async Task AppsMenuAsync()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine($"[bold]{L.T("app.menu.title")}[/]\n");

            var back = L.T("common.back");
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>().Title(L.T("menu.prompt")).AddChoices(
                    L.T("app.menu.create"), L.T("app.menu.addEnv"),
                    L.T("app.menu.addTarget"), L.T("app.menu.list"), back));

            if (choice == back) return;
            if (choice == L.T("app.menu.create")) await CreateApplicationAsync();
            else if (choice == L.T("app.menu.addEnv")) await AddEnvironmentAsync();
            else if (choice == L.T("app.menu.addTarget")) await AddTargetAsync();
            else if (choice == L.T("app.menu.list")) await ListApplicationsAsync();
        }
    }

    private async Task CreateApplicationAsync()
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine($"[bold]{L.T("app.menu.create")}[/]\n");

        var name = AnsiConsole.Ask<string>(L.T("app.create.namePrompt"));
        var desc = AnsiConsole.Prompt(new TextPrompt<string>(L.T("app.create.descPrompt")).AllowEmpty());

        var existing = await appRepo.GetByNameAsync(name);
        if (existing != null)
        {
            AnsiConsole.MarkupLine($"[red]{L.T("app.create.exists", name)}[/]");
        }
        else
        {
            await appRepo.AddAsync(new App { Name = name, Description = desc });
            AnsiConsole.MarkupLine($"[green]{L.T("app.create.success", name)}[/]");
        }
        Pause();
    }

    private async Task AddEnvironmentAsync()
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine($"[bold]{L.T("app.menu.addEnv")}[/]\n");

        var app = await SelectApplicationAsync();
        if (app is null) return;

        var name = AnsiConsole.Ask<string>(L.T("app.env.namePrompt"));
        var requireApproval = AnsiConsole.Confirm(L.T("app.env.requireApproval"), false);

        var existing = await appRepo.GetEnvironmentAsync(app.Id, name);
        if (existing != null)
        {
            AnsiConsole.MarkupLine($"[yellow]{L.T("app.env.exists", name)}[/]");
        }
        else
        {
            await appRepo.AddEnvironmentAsync(new AppEnvironment
            {
                ApplicationId = app.Id,
                Name = name,
                RequireApproval = requireApproval
            });
            AnsiConsole.MarkupLine($"[green]{L.T("app.env.success", name, app.Name)}[/]");
        }
        Pause();
    }

    private async Task AddTargetAsync()
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine($"[bold]{L.T("app.menu.addTarget")}[/]\n");

        var app = await SelectApplicationAsync();
        if (app is null) return;
        var env = SelectEnvironment(app);
        if (env is null) return;

        var name = AnsiConsole.Ask<string>(L.T("app.target.namePrompt"));
        var os = AnsiConsole.Prompt(
            new SelectionPrompt<TargetOS>().Title(L.T("app.target.osPrompt")).AddChoices(TargetOS.Windows, TargetOS.Linux));
        var host = AnsiConsole.Prompt(new TextPrompt<string>(L.T("app.target.hostPrompt")).DefaultValue("localhost"));
        var path = AnsiConsole.Ask<string>(L.T("app.target.pathPrompt"));

        await appRepo.AddTargetAsync(new DeploymentTarget
        {
            EnvironmentId = env.Id,
            Name = name,
            OS = os,
            Host = host,
            DeploymentPath = path
        });
        AnsiConsole.MarkupLine($"[green]{L.T("app.target.success", name)}[/]");
        Pause();
    }

    private async Task ListApplicationsAsync()
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine($"[bold]{L.T("app.list.title")}[/]\n");

        var apps = await appRepo.ListAsync();
        if (apps.Count == 0)
        {
            AnsiConsole.MarkupLine($"[grey]{L.T("common.noAppsYet")}[/]");
        }
        else
        {
            var table = new Table()
                .AddColumn(L.T("app.list.colApp"))
                .AddColumn(L.T("app.list.colEnvs"))
                .AddColumn(L.T("app.list.colTargets"));

            foreach (var a in apps)
            {
                var envNames = string.Join(", ", a.Environments.Select(e =>
                    $"{e.Name} ({string.Join(", ", e.Targets.Select(t => t.Name))})"));
                table.AddRow(a.Name, string.Join(", ", a.Environments.Select(e => e.Name)), envNames);
            }
            AnsiConsole.Write(table);
        }
        Pause();
    }
}
