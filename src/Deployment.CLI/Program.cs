using System.CommandLine;
using Deployment.CLI.Commands;
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

return await root.InvokeAsync(args);
