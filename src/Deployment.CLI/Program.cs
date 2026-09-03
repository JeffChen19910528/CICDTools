using System.CommandLine;
using Deployment.Application.Interfaces;
using Deployment.Application.Services;
using Deployment.CLI.Commands;
using Deployment.CLI.Interactive;
using Deployment.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

if (OperatingSystem.IsWindows())
{
    try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch { /* redirected output stream */ }
}

var dataDir = Environment.GetEnvironmentVariable("DEPLOYCTL_DATA")
    ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "deployctl");

var backupStore = Environment.GetEnvironmentVariable("DEPLOYCTL_BACKUPS")
    ?? Path.Combine(dataDir, "backups");

var releasesStore = Environment.GetEnvironmentVariable("DEPLOYCTL_RELEASES")
    ?? Path.Combine(dataDir, "releases");

var services = new ServiceCollection()
    .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
    .AddDeploymentInfrastructure(dataDir)
    .AddDeploymentApplicationServices(backupStore, releasesStore, dataDir)
    .BuildServiceProvider();

await services.InitializeDatabaseAsync();
using var scope = services.CreateScope();

var root = new RootCommand("deployctl — deployment and release management tool");

root.AddCommand(ReleaseCommands.Build(scope.ServiceProvider));
root.AddCommand(DeployCommands.Build(scope.ServiceProvider));
root.AddCommand(BackupCommands.Build(scope.ServiceProvider));
root.AddCommand(HistoryCommands.Build(scope.ServiceProvider));
root.AddCommand(AppCommands.Build(scope.ServiceProvider));
root.AddCommand(RecoveryCommands.Build(scope.ServiceProvider));

// No arguments: launch the menu-driven UI for non-technical users (e.g. double-clicking
// the executable). Falls back to CLI --help when there's no interactive terminal to
// drive a menu with (piped input, CI, etc.) instead of crashing on a prompt exception.
if (args.Length == 0)
{
    if (Console.IsInputRedirected || Console.IsOutputRedirected)
        return await root.InvokeAsync("--help");

    var menu = new InteractiveMenu(
        scope.ServiceProvider.GetRequiredService<IApplicationRepository>(),
        scope.ServiceProvider.GetRequiredService<IReleaseService>(),
        scope.ServiceProvider.GetRequiredService<IDeploymentService>(),
        scope.ServiceProvider.GetRequiredService<IBackupService>(),
        scope.ServiceProvider.GetRequiredService<IRetentionService>(),
        scope.ServiceProvider.GetRequiredService<IDiffService>(),
        scope.ServiceProvider.GetRequiredService<IAuditService>(),
        scope.ServiceProvider.GetRequiredService<IDeploymentRepository>(),
        scope.ServiceProvider.GetRequiredService<DeploymentOptions>(),
        dataDir);

    try
    {
        await menu.RunAsync();
        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Unexpected error: {ex.Message}");
        return 1;
    }
}

return await root.InvokeAsync(args);
