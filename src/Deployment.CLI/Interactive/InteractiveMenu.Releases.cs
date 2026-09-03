using Deployment.CLI.Localization;
using Deployment.CLI;
using Spectre.Console;

namespace Deployment.CLI.Interactive;

public partial class InteractiveMenu
{
    private async Task ReleasesMenuAsync()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine($"[bold]{L.T("release.menu.title")}[/]\n");

            var back = L.T("common.back");
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>().Title(L.T("menu.prompt")).AddChoices(
                    L.T("release.menu.create"), L.T("release.menu.list"), L.T("release.menu.show"), back));

            if (choice == back) return;
            if (choice == L.T("release.menu.create")) await CreateReleaseAsync();
            else if (choice == L.T("release.menu.list")) await ListReleasesAsync();
            else if (choice == L.T("release.menu.show")) await ShowReleaseAsync();
        }
    }

    private async Task CreateReleaseAsync()
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine($"[bold]{L.T("release.menu.create")}[/]\n");

        var app = await SelectApplicationAsync();
        if (app is null) return;

        var version = AnsiConsole.Ask<string>(L.T("release.create.versionPrompt"));
        var source = AnsiConsole.Ask<string>(L.T("release.create.sourcePrompt"));
        var notes = AnsiConsole.Prompt(new TextPrompt<string>(L.T("release.create.notesPrompt")).AllowEmpty());

        if (!Directory.Exists(source))
        {
            AnsiConsole.MarkupLine($"[red]{L.T("release.create.sourceNotFound", source)}[/]");
            Pause();
            return;
        }

        try
        {
            var release = await AnsiConsole.Status().StartAsync(L.T("release.menu.create"), async _ =>
                await releaseService.CreateReleaseAsync(
                    app.Name, version, source, options.ReleasesStorePath, Environment.UserName, notes: notes));

            AnsiConsole.MarkupLine($"[green]{L.T("release.create.success", release.ReleaseId, release.Files.Count)}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]{L.T("common.error", Markup.Escape(ex.Message))}[/]");
        }
        Pause();
    }

    private async Task ListReleasesAsync()
    {
        AnsiConsole.Clear();
        var app = await SelectApplicationAsync();
        if (app is null) return;

        AnsiConsole.MarkupLine($"[bold]{L.T("release.list.title", app.Name)}[/]\n");
        var releases = await releaseService.ListReleasesAsync(app.Name);

        if (releases.Count == 0)
        {
            AnsiConsole.MarkupLine($"[grey]{L.T("common.noReleasesYet")}[/]");
        }
        else
        {
            var table = new Table()
                .AddColumn(L.T("release.list.colId"))
                .AddColumn(L.T("release.list.colVersion"))
                .AddColumn(L.T("release.list.colCreated"))
                .AddColumn(L.T("release.list.colBy"));

            foreach (var r in releases)
                table.AddRow(r.ReleaseId, r.Version, r.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"), r.CreatedBy);

            AnsiConsole.Write(table);
        }
        Pause();
    }

    private async Task ShowReleaseAsync()
    {
        AnsiConsole.Clear();
        var app = await SelectApplicationAsync();
        if (app is null) return;
        var release = await SelectReleaseAsync(app.Name);
        if (release is null) return;

        AnsiConsole.Clear();
        AnsiConsole.MarkupLine($"[bold]{release.ReleaseId}[/]");
        AnsiConsole.MarkupLine($"{L.T("release.list.colCreated")}: {release.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss} ({release.CreatedBy})");
        if (release.Notes != null) AnsiConsole.MarkupLine($"{Markup.Escape(release.Notes)}");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine($"[bold]{L.T("release.show.files")}[/] ({release.Files.Count})");
        var table = new Table()
            .AddColumn(L.T("release.show.colPath"))
            .AddColumn(L.T("release.show.colSize"))
            .AddColumn(L.T("release.show.colSha"));

        foreach (var f in release.Files.OrderBy(f => f.RelativePath))
            table.AddRow(Markup.Escape(f.RelativePath), ByteFormatter.Format(f.Size), f.Sha256[..16] + "…");

        AnsiConsole.Write(table);
        Pause();
    }
}
