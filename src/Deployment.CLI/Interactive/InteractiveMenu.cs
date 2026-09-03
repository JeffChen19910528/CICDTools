using App = Deployment.Domain.Entities.Application;
using Deployment.Application.Interfaces;
using Deployment.Application.Services;
using Deployment.CLI.Localization;
using Deployment.CLI.Settings;
using Deployment.Domain.Entities;
using Spectre.Console;

namespace Deployment.CLI.Interactive;

/// <summary>
/// A menu-driven front end for non-technical users: launched automatically when
/// deployctl is run with no command-line arguments (e.g. double-clicked).
/// The scriptable "deployctl &lt;command&gt; ..." interface is unaffected.
/// </summary>
public partial class InteractiveMenu(
    IApplicationRepository appRepo,
    IReleaseService releaseService,
    IDeploymentService deploymentService,
    IBackupService backupService,
    IRetentionService retentionService,
    IDiffService diffService,
    IAuditService auditService,
    IDeploymentRepository deploymentRepo,
    DeploymentOptions options,
    string dataDir)
{
    public async Task RunAsync()
    {
        EnsureLanguageSelected();

        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine($"[bold cyan]{Markup.Escape(L.T("app.title"))}[/]");
            AnsiConsole.WriteLine();

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title(L.T("menu.prompt"))
                    .PageSize(10)
                    .AddChoices(
                        L.T("menu.apps"), L.T("menu.releases"), L.T("menu.deploy"),
                        L.T("menu.backups"), L.T("menu.history"), L.T("menu.recovery"),
                        L.T("menu.language"), L.T("menu.exit")));

            try
            {
                if (choice == L.T("menu.exit"))
                {
                    AnsiConsole.MarkupLine($"[grey]{L.T("menu.goodbye")}[/]");
                    return;
                }
                else if (choice == L.T("menu.apps")) await AppsMenuAsync();
                else if (choice == L.T("menu.releases")) await ReleasesMenuAsync();
                else if (choice == L.T("menu.deploy")) await DeployMenuAsync();
                else if (choice == L.T("menu.backups")) await BackupsMenuAsync();
                else if (choice == L.T("menu.history")) await HistoryMenuAsync();
                else if (choice == L.T("menu.recovery")) await RecoveryMenuAsync();
                else if (choice == L.T("menu.language")) LanguageMenu();
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]{L.T("common.error", Markup.Escape(ex.Message))}[/]");
                Pause();
            }
        }
    }

    private void EnsureLanguageSelected()
    {
        var settings = CliSettingsStore.Load(dataDir);
        if (settings.Language != null)
        {
            L.Current = LanguageExtensions.FromCode(settings.Language);
            return;
        }

        // First run: no persisted language preference. Ask in both languages
        // since we don't know which one the user reads yet.
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select your language / 請選擇語言")
                .AddChoices(Language.English.DisplayName(), Language.TraditionalChinese.DisplayName()));

        L.Current = choice == Language.TraditionalChinese.DisplayName()
            ? Language.TraditionalChinese
            : Language.English;

        CliSettingsStore.SaveLanguage(dataDir, L.Current);
    }

    private void LanguageMenu()
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine($"[bold]{L.T("lang.menu.title")}[/]\n");
        AnsiConsole.MarkupLine(L.T("lang.menu.current", L.Current.DisplayName()));
        AnsiConsole.WriteLine();

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title(L.T("menu.prompt"))
                .AddChoices(Language.English.DisplayName(), Language.TraditionalChinese.DisplayName(), L.T("common.back")));

        if (choice == Language.English.DisplayName()) L.Current = Language.English;
        else if (choice == Language.TraditionalChinese.DisplayName()) L.Current = Language.TraditionalChinese;
        else return;

        CliSettingsStore.SaveLanguage(dataDir, L.Current);
        AnsiConsole.MarkupLine($"[green]{L.T("lang.menu.changed", L.Current.DisplayName())}[/]");
        Pause();
    }

    private static void Pause()
    {
        AnsiConsole.MarkupLine($"\n[grey]{L.T("common.pressEnter")}[/]");
        AnsiConsole.Console.Input.ReadKey(intercept: true);
    }

    // --- Shared selection helpers -------------------------------------------------

    private async Task<App?> SelectApplicationAsync()
    {
        var apps = await appRepo.ListAsync();
        if (apps.Count == 0)
        {
            AnsiConsole.MarkupLine($"[yellow]{L.T("common.noAppsYet")}[/]");
            Pause();
            return null;
        }

        var back = L.T("common.back");
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>().Title(L.T("select.app")).PageSize(10)
                .AddChoices(apps.Select(a => a.Name).Append(back)));

        return choice == back ? null : apps.First(a => a.Name == choice);
    }

    private AppEnvironment? SelectEnvironment(App app)
    {
        if (app.Environments.Count == 0)
        {
            AnsiConsole.MarkupLine($"[yellow]{L.T("common.noEnvsYet")}[/]");
            Pause();
            return null;
        }

        var back = L.T("common.back");
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>().Title(L.T("select.env")).PageSize(10)
                .AddChoices(app.Environments.Select(e => e.Name).Append(back)));

        return choice == back ? null : app.Environments.First(e => e.Name == choice);
    }

    private DeploymentTarget? SelectTarget(AppEnvironment env)
    {
        if (env.Targets.Count == 0)
        {
            AnsiConsole.MarkupLine($"[yellow]{L.T("common.noTargetsYet")}[/]");
            Pause();
            return null;
        }

        var back = L.T("common.back");
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>().Title(L.T("select.target")).PageSize(10)
                .AddChoices(env.Targets.Select(t => t.Name).Append(back)));

        return choice == back ? null : env.Targets.First(t => t.Name == choice);
    }

    private async Task<(App App, AppEnvironment Env, DeploymentTarget Target)?> SelectTargetChainAsync()
    {
        var app = await SelectApplicationAsync();
        if (app is null) return null;
        var env = SelectEnvironment(app);
        if (env is null) return null;
        var target = SelectTarget(env);
        if (target is null) return null;
        return (app, env, target);
    }

    private async Task<Release?> SelectReleaseAsync(string applicationName)
    {
        var releases = await releaseService.ListReleasesAsync(applicationName);
        if (releases.Count == 0)
        {
            AnsiConsole.MarkupLine($"[yellow]{L.T("common.noReleasesYet")}[/]");
            Pause();
            return null;
        }

        var back = L.T("common.back");
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>().Title(L.T("select.release")).PageSize(10)
                .AddChoices(releases.OrderByDescending(r => r.CreatedAt).Select(r => r.Version).Append(back)));

        return choice == back ? null : releases.First(r => r.Version == choice);
    }
}
