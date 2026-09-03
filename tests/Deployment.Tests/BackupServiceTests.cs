using Deployment.Application.Services;
using Deployment.Domain.Entities;
using Deployment.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Deployment.Tests;

public class BackupServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    [Fact]
    public async Task CreateBackup_CreatesVerifiedBackup()
    {
        var db = TestHelper.CreateDb();
        var (svc, appRepo) = BuildService(db);

        var deployPath = Path.Combine(_tempDir, "deploy");
        TestHelper.CreateFiles(deployPath, new Dictionary<string, string>
        {
            ["app.exe"] = "binary content",
            ["config.json"] = """{"timeout":30}"""
        });
        await TestHelper.CreateAppAsync(appRepo, deployPath: deployPath);

        var backupStore = Path.Combine(_tempDir, "backups");
        var backup = await svc.CreateBackupAsync("TestApp", "Test", "server01", backupStore, "admin");

        Assert.Equal(BackupStatus.Verified, backup.Status);
        Assert.Equal(2, backup.FileCount);
        Assert.True(Directory.Exists(backup.BackupPath));
    }

    [Fact]
    public async Task CreateBackup_EmptyDirectory_CreatesEmptyBackup()
    {
        var db = TestHelper.CreateDb();
        var (svc, appRepo) = BuildService(db);

        var deployPath = Path.Combine(_tempDir, "deploy");
        Directory.CreateDirectory(deployPath);
        await TestHelper.CreateAppAsync(appRepo, deployPath: deployPath);

        var backupStore = Path.Combine(_tempDir, "backups");
        var backup = await svc.CreateBackupAsync("TestApp", "Test", "server01", backupStore, "admin");

        Assert.Equal(BackupStatus.Verified, backup.Status);
        Assert.Equal(0, backup.FileCount);
    }

    [Fact]
    public async Task ListBackups_ReturnsBackupsForTarget()
    {
        var db = TestHelper.CreateDb();
        var (svc, appRepo) = BuildService(db);

        var deployPath = Path.Combine(_tempDir, "deploy");
        TestHelper.CreateFiles(deployPath, new Dictionary<string, string> { ["file.txt"] = "content" });
        await TestHelper.CreateAppAsync(appRepo, deployPath: deployPath);

        var backupStore = Path.Combine(_tempDir, "backups");
        await svc.CreateBackupAsync("TestApp", "Test", "server01", backupStore, "admin");
        await svc.CreateBackupAsync("TestApp", "Test", "server01", backupStore, "admin");

        var backups = await svc.ListBackupsAsync("TestApp", "Test", "server01");
        Assert.Equal(2, backups.Count);
    }

    [Fact]
    public async Task ProtectBackup_SetsProtectedFlag()
    {
        var db = TestHelper.CreateDb();
        var (svc, appRepo) = BuildService(db);

        var deployPath = Path.Combine(_tempDir, "deploy");
        TestHelper.CreateFiles(deployPath, new Dictionary<string, string> { ["file.txt"] = "content" });
        await TestHelper.CreateAppAsync(appRepo, deployPath: deployPath);

        var backupStore = Path.Combine(_tempDir, "backups");
        var backup = await svc.CreateBackupAsync("TestApp", "Test", "server01", backupStore, "admin");

        await svc.ProtectBackupAsync(backup.BackupId, protect: true);

        var backups = await svc.ListBackupsAsync("TestApp", "Test", "server01");
        Assert.True(backups[0].IsProtected);
    }

    private (BackupService Service, Deployment.Application.Interfaces.IApplicationRepository AppRepo)
        BuildService(Deployment.Infrastructure.Data.DeploymentDbContext db)
    {
        var svcs = TestHelper.CreateServices(db, _tempDir);
        var service = new BackupService(
            svcs.AppRepo, svcs.BackupRepo, svcs.Fs, svcs.Checksum,
            svcs.Audit, NullLogger<BackupService>.Instance);
        return (service, svcs.AppRepo);
    }
}
