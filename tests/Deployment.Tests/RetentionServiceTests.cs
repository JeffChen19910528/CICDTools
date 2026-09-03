using Deployment.Application.Services;
using Deployment.Domain.Entities;
using Deployment.Infrastructure.Repositories;
using Deployment.Infrastructure.Services;
using Deployment.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Deployment.Tests;

public class RetentionServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    [Fact]
    public async Task ApplyRetention_ByCount_DeletesOldBackups()
    {
        var db = TestHelper.CreateDb();
        var svcs = TestHelper.CreateServices(db, _tempDir);
        var svc = BuildService(db, svcs);
        var backupSvc = BuildBackupService(db, svcs);

        var deployPath = Path.Combine(_tempDir, "deploy");
        TestHelper.CreateFiles(deployPath, new Dictionary<string, string> { ["file.txt"] = "content" });
        var app = await TestHelper.CreateAppAsync(svcs.AppRepo, deployPath: deployPath);
        var env = await svcs.AppRepo.GetEnvironmentAsync(app.Id, "Test");

        await svcs.AppRepo.UpsertRetentionPolicyAsync(new RetentionPolicy
        {
            EnvironmentId = env!.Id,
            RetainCount = 2,
            MinimumCount = 1
        });

        var backupStore = Path.Combine(_tempDir, "backups");
        for (int i = 0; i < 5; i++)
            await backupSvc.CreateBackupAsync("TestApp", "Test", "server01", backupStore, "admin");

        await svc.ApplyRetentionAsync("TestApp", "Test", "server01", "admin");

        var remaining = await backupSvc.ListBackupsAsync("TestApp", "Test", "server01");
        var notDeleted = remaining.Where(b => b.Status != BackupStatus.Deleted).ToList();
        Assert.True(notDeleted.Count <= 2, $"Expected ≤2 remaining but got {notDeleted.Count}");
    }

    [Fact]
    public async Task ApplyRetention_ProtectedBackup_IsNeverDeleted()
    {
        var db = TestHelper.CreateDb();
        var svcs = TestHelper.CreateServices(db, _tempDir);
        var svc = BuildService(db, svcs);
        var backupSvc = BuildBackupService(db, svcs);

        var deployPath = Path.Combine(_tempDir, "deploy");
        TestHelper.CreateFiles(deployPath, new Dictionary<string, string> { ["file.txt"] = "content" });
        var app = await TestHelper.CreateAppAsync(svcs.AppRepo, deployPath: deployPath);
        var env = await svcs.AppRepo.GetEnvironmentAsync(app.Id, "Test");

        await svcs.AppRepo.UpsertRetentionPolicyAsync(new RetentionPolicy
        {
            EnvironmentId = env!.Id,
            RetainCount = 1,
            MinimumCount = 1
        });

        var backupStore = Path.Combine(_tempDir, "backups");
        var backup1 = await backupSvc.CreateBackupAsync("TestApp", "Test", "server01", backupStore, "admin");
        await backupSvc.ProtectBackupAsync(backup1.BackupId, protect: true);

        await backupSvc.CreateBackupAsync("TestApp", "Test", "server01", backupStore, "admin");
        await backupSvc.CreateBackupAsync("TestApp", "Test", "server01", backupStore, "admin");

        await svc.ApplyRetentionAsync("TestApp", "Test", "server01", "admin");

        var all = await backupSvc.ListBackupsAsync("TestApp", "Test", "server01");
        var protectedBackup = all.First(b => b.BackupId == backup1.BackupId);
        Assert.NotEqual(BackupStatus.Deleted, protectedBackup.Status);
    }

    private RetentionService BuildService(
        Deployment.Infrastructure.Data.DeploymentDbContext db,
        (Deployment.Application.Interfaces.IApplicationRepository AppRepo,
         Deployment.Application.Interfaces.IReleaseRepository ReleaseRepo,
         Deployment.Application.Interfaces.IDeploymentRepository DeploymentRepo,
         Deployment.Application.Interfaces.IBackupRepository BackupRepo,
         Deployment.Application.Interfaces.IAuditService Audit,
         Deployment.Application.Interfaces.IFileSystem Fs,
         Deployment.Application.Interfaces.IChecksumService Checksum,
         Deployment.Application.Interfaces.ILockService Lock) svcs)
    {
        return new RetentionService(svcs.AppRepo, svcs.BackupRepo, svcs.Fs, svcs.Audit,
            NullLogger<RetentionService>.Instance);
    }

    private BackupService BuildBackupService(
        Deployment.Infrastructure.Data.DeploymentDbContext db,
        (Deployment.Application.Interfaces.IApplicationRepository AppRepo,
         Deployment.Application.Interfaces.IReleaseRepository ReleaseRepo,
         Deployment.Application.Interfaces.IDeploymentRepository DeploymentRepo,
         Deployment.Application.Interfaces.IBackupRepository BackupRepo,
         Deployment.Application.Interfaces.IAuditService Audit,
         Deployment.Application.Interfaces.IFileSystem Fs,
         Deployment.Application.Interfaces.IChecksumService Checksum,
         Deployment.Application.Interfaces.ILockService Lock) svcs)
    {
        return new BackupService(svcs.AppRepo, svcs.BackupRepo, svcs.Fs, svcs.Checksum,
            svcs.Audit, NullLogger<BackupService>.Instance);
    }
}
