using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Deployment.Application.Interfaces;
using Deployment.Application.Services;
using Deployment.CLI;
using Deployment.CLI.Localization;
using Deployment.CLI.Settings;
using Deployment.Desktop.Services;
using Deployment.Desktop.Views;
using Deployment.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Deployment.Desktop;

public partial class App : global::Avalonia.Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var (dataDir, backupStore, releasesStore) = DataPaths.Resolve();

            var provider = new ServiceCollection()
                .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
                .AddDeploymentInfrastructure(dataDir)
                .AddDeploymentApplicationServices(backupStore, releasesStore, dataDir)
                .BuildServiceProvider();

            provider.InitializeDatabaseAsync().GetAwaiter().GetResult();
            var scope = provider.CreateScope();

            var services = new AppServices(
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

            var settings = CliSettingsStore.Load(dataDir);
            L.Current = settings.Language != null ? LanguageExtensions.FromCode(settings.Language) : Language.English;

            desktop.MainWindow = new MainWindow(services);
            desktop.Exit += (_, _) => scope.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
