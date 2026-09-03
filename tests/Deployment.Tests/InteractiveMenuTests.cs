using Deployment.Application.Services;
using Deployment.CLI.Interactive;
using Deployment.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Spectre.Console;
using Spectre.Console.Testing;

namespace Deployment.Tests;

/// <summary>
/// Drives the interactive menu the same way a real terminal would (arrow-key
/// navigation + typed text) using Spectre.Console's TestConsole, since the menu
/// reads from the global AnsiConsole and can't be exercised through piped stdin.
/// </summary>
public class InteractiveMenuTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly IAnsiConsole _originalConsole = AnsiConsole.Console;

    public InteractiveMenuTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        AnsiConsole.Console = _originalConsole;
        Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task FirstRun_SelectLanguage_CreateApplication_ThenExit()
    {
        var db = TestHelper.CreateDb();
        var svcs = TestHelper.CreateServices(db, _tempDir);
        var opts = new DeploymentOptions
        {
            BackupStorePath = Path.Combine(_tempDir, "backups"),
            ReleasesStorePath = Path.Combine(_tempDir, "releases"),
            DataPath = _tempDir
        };

        var backupSvc = new BackupService(svcs.Resolver, svcs.BackupRepo, svcs.Fs, svcs.Checksum, svcs.Audit, NullLogger<BackupService>.Instance);
        var diffSvc = new DiffService(svcs.Fs, svcs.Checksum, NullLogger<DiffService>.Instance);
        var releaseSvc = new ReleaseService(svcs.Resolver, svcs.ReleaseRepo, svcs.Fs, svcs.Checksum, svcs.Audit, NullLogger<ReleaseService>.Instance);
        var deploySvc = new DeploymentService(svcs.Resolver, svcs.AppRepo, svcs.ReleaseRepo, svcs.DeploymentRepo,
            svcs.Fs, svcs.Checksum, svcs.Lock, backupSvc, diffSvc, svcs.Audit, NullLogger<DeploymentService>.Instance, opts);
        var retentionSvc = new RetentionService(svcs.Resolver, svcs.AppRepo, svcs.BackupRepo, svcs.Fs, svcs.Audit, NullLogger<RetentionService>.Instance);

        var menu = new InteractiveMenu(
            svcs.AppRepo, releaseSvc, deploySvc, backupSvc, retentionSvc, diffSvc,
            svcs.Audit, svcs.DeploymentRepo, opts, _tempDir);

        var console = new TestConsole().Interactive();
        AnsiConsole.Console = console;

        // 1. First-run language prompt: default highlight is "English" -> just Enter.
        console.Input.PushKey(ConsoleKey.Enter);

        // 2. Main menu: "Manage Applications" is the first choice -> Enter.
        console.Input.PushKey(ConsoleKey.Enter);

        // 3. Apps submenu: "Create Application" is the first choice -> Enter.
        console.Input.PushKey(ConsoleKey.Enter);

        // 4. Name prompt, then description prompt (left blank).
        console.Input.PushTextWithEnter("SmokeTestApp");
        console.Input.PushTextWithEnter("");
        console.Input.PushKey(ConsoleKey.Enter); // pause after success message

        // 5. Back in the apps submenu, move down to "Back" (5th item) -> Enter.
        for (var i = 0; i < 4; i++) console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushKey(ConsoleKey.Enter);

        // 6. Back at the main menu, move down to "Exit" (8th item) -> Enter.
        for (var i = 0; i < 7; i++) console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushKey(ConsoleKey.Enter);

        await menu.RunAsync();

        var created = await svcs.AppRepo.GetByNameAsync("SmokeTestApp");
        Assert.NotNull(created);

        var settingsPath = Path.Combine(_tempDir, "cli-settings.json");
        Assert.True(File.Exists(settingsPath), "Language preference should be persisted after first run.");
        Assert.Contains("\"en\"", await File.ReadAllTextAsync(settingsPath));
    }

    [Fact]
    public async Task LanguageSwitch_PersistsToSettingsFile()
    {
        var db = TestHelper.CreateDb();
        var svcs = TestHelper.CreateServices(db, _tempDir);
        var opts = new DeploymentOptions
        {
            BackupStorePath = Path.Combine(_tempDir, "backups"),
            ReleasesStorePath = Path.Combine(_tempDir, "releases"),
            DataPath = _tempDir
        };

        var backupSvc = new BackupService(svcs.Resolver, svcs.BackupRepo, svcs.Fs, svcs.Checksum, svcs.Audit, NullLogger<BackupService>.Instance);
        var diffSvc = new DiffService(svcs.Fs, svcs.Checksum, NullLogger<DiffService>.Instance);
        var releaseSvc = new ReleaseService(svcs.Resolver, svcs.ReleaseRepo, svcs.Fs, svcs.Checksum, svcs.Audit, NullLogger<ReleaseService>.Instance);
        var deploySvc = new DeploymentService(svcs.Resolver, svcs.AppRepo, svcs.ReleaseRepo, svcs.DeploymentRepo,
            svcs.Fs, svcs.Checksum, svcs.Lock, backupSvc, diffSvc, svcs.Audit, NullLogger<DeploymentService>.Instance, opts);
        var retentionSvc = new RetentionService(svcs.Resolver, svcs.AppRepo, svcs.BackupRepo, svcs.Fs, svcs.Audit, NullLogger<RetentionService>.Instance);

        var menu = new InteractiveMenu(
            svcs.AppRepo, releaseSvc, deploySvc, backupSvc, retentionSvc, diffSvc,
            svcs.Audit, svcs.DeploymentRepo, opts, _tempDir);

        var console = new TestConsole().Interactive();
        AnsiConsole.Console = console;

        // First-run language prompt -> pick Traditional Chinese (2nd choice).
        console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushKey(ConsoleKey.Enter);

        // Main menu -> "Language Settings" (7th item, index 6).
        for (var i = 0; i < 6; i++) console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushKey(ConsoleKey.Enter);

        // Language submenu -> switch back to English (1st item).
        console.Input.PushKey(ConsoleKey.Enter);
        console.Input.PushKey(ConsoleKey.Enter); // pause after "language switched" message

        // Main menu -> Exit.
        for (var i = 0; i < 7; i++) console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushKey(ConsoleKey.Enter);

        await menu.RunAsync();

        var settingsPath = Path.Combine(_tempDir, "cli-settings.json");
        var json = await File.ReadAllTextAsync(settingsPath);
        Assert.Contains("\"en\"", json);
    }

    [Fact]
    public async Task FullFlow_CreateAppEnvTarget_Release_DryRunDeploy()
    {
        var db = TestHelper.CreateDb();
        var svcs = TestHelper.CreateServices(db, _tempDir);
        var opts = new DeploymentOptions
        {
            BackupStorePath = Path.Combine(_tempDir, "backups"),
            ReleasesStorePath = Path.Combine(_tempDir, "releases"),
            DataPath = _tempDir
        };

        var backupSvc = new BackupService(svcs.Resolver, svcs.BackupRepo, svcs.Fs, svcs.Checksum, svcs.Audit, NullLogger<BackupService>.Instance);
        var diffSvc = new DiffService(svcs.Fs, svcs.Checksum, NullLogger<DiffService>.Instance);
        var releaseSvc = new ReleaseService(svcs.Resolver, svcs.ReleaseRepo, svcs.Fs, svcs.Checksum, svcs.Audit, NullLogger<ReleaseService>.Instance);
        var deploySvc = new DeploymentService(svcs.Resolver, svcs.AppRepo, svcs.ReleaseRepo, svcs.DeploymentRepo,
            svcs.Fs, svcs.Checksum, svcs.Lock, backupSvc, diffSvc, svcs.Audit, NullLogger<DeploymentService>.Instance, opts);
        var retentionSvc = new RetentionService(svcs.Resolver, svcs.AppRepo, svcs.BackupRepo, svcs.Fs, svcs.Audit, NullLogger<RetentionService>.Instance);

        var menu = new InteractiveMenu(
            svcs.AppRepo, releaseSvc, deploySvc, backupSvc, retentionSvc, diffSvc,
            svcs.Audit, svcs.DeploymentRepo, opts, _tempDir);

        var sourceDir = Path.Combine(_tempDir, "src");
        TestHelper.CreateFiles(sourceDir, new Dictionary<string, string> { ["index.html"] = "hello" });

        var console = new TestConsole().Interactive();
        AnsiConsole.Console = console;

        // Language: Enter (English, default).
        console.Input.PushKey(ConsoleKey.Enter);

        // Main menu -> Manage Applications (index 0).
        console.Input.PushKey(ConsoleKey.Enter);

        // Apps submenu -> Create Application (index 0).
        console.Input.PushKey(ConsoleKey.Enter);
        console.Input.PushTextWithEnter("IntegApp");
        console.Input.PushTextWithEnter("");
        console.Input.PushKey(ConsoleKey.Enter); // pause after success message

        // Apps submenu -> Add Environment (index 1).
        console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushKey(ConsoleKey.Enter);
        console.Input.PushKey(ConsoleKey.Enter); // select the only application
        console.Input.PushTextWithEnter("prod");
        console.Input.PushKey(ConsoleKey.Enter); // require-approval confirm: accept default (No)
        console.Input.PushKey(ConsoleKey.Enter); // pause after success message

        // Apps submenu -> Add Deployment Target (index 2).
        console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushKey(ConsoleKey.Enter);
        console.Input.PushKey(ConsoleKey.Enter); // select the only application
        console.Input.PushKey(ConsoleKey.Enter); // select the only environment
        console.Input.PushTextWithEnter("web1");
        console.Input.PushKey(ConsoleKey.Enter); // OS: Windows (default first choice)
        console.Input.PushKey(ConsoleKey.Enter); // host: accept default "localhost"
        console.Input.PushTextWithEnter(Path.Combine(_tempDir, "deployed"));
        console.Input.PushKey(ConsoleKey.Enter); // pause after success message

        // Apps submenu -> Back (index 4).
        for (var i = 0; i < 4; i++) console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushKey(ConsoleKey.Enter);

        // Main menu -> Release Management (index 1).
        console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushKey(ConsoleKey.Enter);

        // Release submenu -> Create Release (index 0).
        console.Input.PushKey(ConsoleKey.Enter);
        console.Input.PushKey(ConsoleKey.Enter); // select the only application
        console.Input.PushTextWithEnter("1.0.0");
        console.Input.PushTextWithEnter(sourceDir);
        console.Input.PushTextWithEnter("");
        console.Input.PushKey(ConsoleKey.Enter); // pause after success message

        // Release submenu -> Back (index 3).
        for (var i = 0; i < 3; i++) console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushKey(ConsoleKey.Enter);

        // Main menu -> Deploy & Rollback (index 2).
        for (var i = 0; i < 2; i++) console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushKey(ConsoleKey.Enter);

        // Deploy submenu -> Deploy (Dry Run) (index 1).
        console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushKey(ConsoleKey.Enter);
        console.Input.PushKey(ConsoleKey.Enter); // select the only application
        console.Input.PushKey(ConsoleKey.Enter); // select the only environment
        console.Input.PushKey(ConsoleKey.Enter); // select the only target
        console.Input.PushKey(ConsoleKey.Enter); // select the only release
        console.Input.PushKey(ConsoleKey.Enter); // pause after result message

        // Deploy submenu -> Back (index 4).
        for (var i = 0; i < 4; i++) console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushKey(ConsoleKey.Enter);

        // Main menu -> Exit (index 7).
        for (var i = 0; i < 7; i++) console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushKey(ConsoleKey.Enter);

        await menu.RunAsync();

        var app = await svcs.AppRepo.GetByNameAsync("IntegApp");
        Assert.NotNull(app);
        Assert.Single(app!.Environments);
        Assert.Single(app.Environments.First().Targets);

        var releases = await releaseSvc.ListReleasesAsync("IntegApp");
        Assert.Single(releases);

        var history = await deploySvc.GetHistoryAsync("IntegApp", "prod", "web1");
        Assert.Single(history);
        Assert.True(history[0].IsDryRun);
        Assert.False(Directory.Exists(Path.Combine(_tempDir, "deployed")) &&
                     File.Exists(Path.Combine(_tempDir, "deployed", "index.html")),
            "Dry run must not write files to the deployment target.");
    }
}
