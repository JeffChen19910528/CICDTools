using Deployment.Application.Interfaces;
using Deployment.Application.Services;
using Deployment.Domain.Entities;
using Deployment.Infrastructure.Data;
using Deployment.Infrastructure.Repositories;
using Deployment.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Deployment.Tests.Helpers;

public static class TestHelper
{
    public static DeploymentDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<DeploymentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DeploymentDbContext(opts);
    }

    public static (
        IApplicationRepository AppRepo,
        IReleaseRepository ReleaseRepo,
        IDeploymentRepository DeploymentRepo,
        IBackupRepository BackupRepo,
        IAuditService Audit,
        IFileSystem Fs,
        IChecksumService Checksum,
        ILockService Lock,
        ITargetResolver Resolver) CreateServices(DeploymentDbContext db, string tempDir)
    {
        var appRepo = new ApplicationRepository(db);
        var releaseRepo = new ReleaseRepository(db);
        var deploymentRepo = new DeploymentRepository(db);
        var backupRepo = new BackupRepository(db);
        var audit = new AuditRepository(db);
        var fs = new LocalFileSystem();
        var checksum = new Sha256ChecksumService();
        var lockSvc = new FileLockService(Path.Combine(tempDir, "locks"));
        var resolver = new TargetResolver(appRepo);
        return (appRepo, releaseRepo, deploymentRepo, backupRepo, audit, fs, checksum, lockSvc, resolver);
    }

    public static async Task<Domain.Entities.Application> CreateAppAsync(
        IApplicationRepository repo,
        string name = "TestApp",
        string envName = "Test",
        string targetName = "server01",
        string deployPath = "")
    {
        var app = new Domain.Entities.Application { Name = name, Description = "Test" };
        await repo.AddAsync(app);

        var env = new AppEnvironment { ApplicationId = app.Id, Name = envName };
        await repo.AddEnvironmentAsync(env);

        var target = new DeploymentTarget
        {
            EnvironmentId = env.Id,
            Name = targetName,
            OS = TargetOS.Windows,
            Host = "localhost",
            DeploymentPath = deployPath
        };
        await repo.AddTargetAsync(target);

        return app;
    }

    public static void CreateFiles(string directory, Dictionary<string, string> files)
    {
        foreach (var (path, content) in files)
        {
            var fullPath = Path.Combine(directory, path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, content);
        }
    }
}
