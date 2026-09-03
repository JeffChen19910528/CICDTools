using Deployment.Application.Interfaces;
using Deployment.Application.Services;
using Deployment.Infrastructure.Data;
using Deployment.Infrastructure.Repositories;
using Deployment.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Deployment.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDeploymentInfrastructure(
        this IServiceCollection services,
        string dataDirectory)
    {
        Directory.CreateDirectory(dataDirectory);

        var dbPath = Path.Combine(dataDirectory, "deployctl.db");
        services.AddDbContext<DeploymentDbContext>(opt =>
            opt.UseSqlite($"Data Source={dbPath}"));

        services.AddScoped<IApplicationRepository, ApplicationRepository>();
        services.AddScoped<IReleaseRepository, ReleaseRepository>();
        services.AddScoped<IDeploymentRepository, DeploymentRepository>();
        services.AddScoped<IBackupRepository, BackupRepository>();
        services.AddScoped<IAuditService, AuditRepository>();

        services.AddSingleton<IFileSystem, LocalFileSystem>();
        services.AddSingleton<IChecksumService, Sha256ChecksumService>();
        services.AddSingleton<ILockService>(_ => new FileLockService(Path.Combine(dataDirectory, "locks")));

        return services;
    }

    public static IServiceCollection AddDeploymentApplicationServices(
        this IServiceCollection services,
        string backupStorePath,
        string releasesStorePath,
        string dataPath)
    {
        services.AddSingleton(new DeploymentOptions
        {
            BackupStorePath = backupStorePath,
            ReleasesStorePath = releasesStorePath,
            DataPath = dataPath
        });

        services.AddScoped<ReleaseService>();
        services.AddScoped<BackupService>();
        services.AddScoped<DiffService>();
        services.AddScoped<DeploymentService>();
        services.AddScoped<RetentionService>();

        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DeploymentDbContext>();
        await db.Database.MigrateAsync();
    }
}
