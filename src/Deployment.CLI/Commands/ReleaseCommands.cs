using System.CommandLine;
using Deployment.Application.Interfaces;
using Deployment.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace Deployment.CLI.Commands;

public static class ReleaseCommands
{
    public static Command Build(IServiceProvider services)
    {
        var cmd = new Command("release", "Manage releases");

        cmd.AddCommand(BuildCreate(services));
        cmd.AddCommand(BuildList(services));
        cmd.AddCommand(BuildShow(services));

        return cmd;
    }

    private static Command BuildCreate(IServiceProvider services)
    {
        var appArg = new Argument<string>("app", "Application name");
        var versionArg = new Argument<string>("version", "Release version (e.g. 1.4.2)");
        var sourceArg = new Argument<string>("source", "Source directory path");
        var operatorOpt = new Option<string>("--operator", () => Environment.UserName, "Operator name");
        var commitOpt = new Option<string?>("--commit", "Git commit ID");
        var buildOpt = new Option<string?>("--build", "Build ID");
        var notesOpt = new Option<string?>("--notes", "Release notes");

        var cmd = new Command("create", "Create a new release from a directory")
            { appArg, versionArg, sourceArg, operatorOpt, commitOpt, buildOpt, notesOpt };

        cmd.SetHandler(async (app, version, source, op, commit, build, notes) =>
        {
            var svc = services.GetRequiredService<IReleaseService>();
            var opts = services.GetRequiredService<DeploymentOptions>();

            await AnsiConsole.Status().StartAsync($"Creating release {app}-{version}...", async ctx =>
            {
                try
                {
                    var release = await svc.CreateReleaseAsync(app, version, source, opts.ReleasesStorePath, op, commit, build, notes);
                    AnsiConsole.MarkupLine($"[green]Release [bold]{release.ReleaseId}[/] created with {release.Files.Count} files.[/]");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
                }
            });
        }, appArg, versionArg, sourceArg, operatorOpt, commitOpt, buildOpt, notesOpt);

        return cmd;
    }

    private static Command BuildList(IServiceProvider services)
    {
        var appArg = new Argument<string>("app", "Application name");
        var cmd = new Command("list", "List releases for an application") { appArg };

        cmd.SetHandler(async (app) =>
        {
            var svc = services.GetRequiredService<IReleaseService>();
            var releases = await svc.ListReleasesAsync(app);

            if (!releases.Any()) { AnsiConsole.MarkupLine("[grey]No releases found.[/]"); return; }

            var table = new Table()
                .AddColumn("Release ID")
                .AddColumn("Version")
                .AddColumn("Created")
                .AddColumn("By")
                .AddColumn("Commit")
                .AddColumn("Build");

            foreach (var r in releases)
            {
                table.AddRow(
                    r.ReleaseId,
                    r.Version,
                    r.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                    r.CreatedBy,
                    r.CommitId ?? "-",
                    r.BuildId ?? "-");
            }

            AnsiConsole.Write(table);
        }, appArg);

        return cmd;
    }

    private static Command BuildShow(IServiceProvider services)
    {
        var releaseIdArg = new Argument<string>("release-id", "Release ID (e.g. ERP-1.4.2)");
        var cmd = new Command("show", "Show release details") { releaseIdArg };

        cmd.SetHandler(async (releaseId) =>
        {
            var svc = services.GetRequiredService<IReleaseService>();
            try
            {
                var r = await svc.GetReleaseAsync(releaseId);
                AnsiConsole.MarkupLine($"[bold]Release:[/] {r.ReleaseId}");
                AnsiConsole.MarkupLine($"[bold]Version:[/] {r.Version}");
                AnsiConsole.MarkupLine($"[bold]Created:[/] {r.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss} by {r.CreatedBy}");
                if (r.CommitId != null) AnsiConsole.MarkupLine($"[bold]Commit:[/]  {r.CommitId}");
                if (r.BuildId != null) AnsiConsole.MarkupLine($"[bold]Build:[/]   {r.BuildId}");
                if (r.Notes != null) AnsiConsole.MarkupLine($"[bold]Notes:[/]   {Markup.Escape(r.Notes)}");
                AnsiConsole.MarkupLine($"[bold]Files:[/]   {r.Files.Count}");

                var table = new Table().AddColumn("Path").AddColumn("Size").AddColumn("SHA-256");
                foreach (var f in r.Files.OrderBy(f => f.RelativePath))
                    table.AddRow(Markup.Escape(f.RelativePath), ByteFormatter.Format(f.Size), f.Sha256[..16] + "…");
                AnsiConsole.Write(table);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
            }
        }, releaseIdArg);

        return cmd;
    }
}
