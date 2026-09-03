using Deployment.Application.Interfaces;
using Deployment.Domain.Entities;
using Deployment.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Deployment.Application.Services;

public class DeploymentOptions
{
    public string BackupStorePath { get; set; } = string.Empty;
    public string ReleasesStorePath { get; set; } = string.Empty;
    public string DataPath { get; set; } = string.Empty;
}

public class DeploymentService(
    IApplicationRepository appRepo,
    IReleaseRepository releaseRepo,
    IDeploymentRepository deploymentRepo,
    IFileSystem fs,
    IChecksumService checksum,
    ILockService lockService,
    BackupService backupService,
    DiffService diffService,
    IAuditService audit,
    ILogger<DeploymentService> logger,
    DeploymentOptions options)
{
    public async Task<DeploymentRecord> StartDeploymentAsync(
        string applicationName,
        string environmentName,
        string targetName,
        string releaseVersion,
        string @operator,
        bool isDryRun = false,
        CancellationToken ct = default)
    {
        var (app, env, target) = await ResolveAsync(applicationName, environmentName, targetName, ct);
        var release = await releaseRepo.GetByVersionAsync(app.Id, releaseVersion, ct)
            ?? throw new InvalidOperationException($"Release '{releaseVersion}' not found.");

        if (target.CurrentRelease == releaseVersion)
        {
            logger.LogInformation("Release {Version} already deployed to {Target}. Skipping.", releaseVersion, targetName);
            throw new InvalidOperationException($"Release '{releaseVersion}' is already deployed to '{targetName}'. Already deployed.");
        }

        var lockInfo = new DeploymentLock(applicationName, environmentName, targetName,
            $"DEP-{DateTime.UtcNow:yyyyMMdd-HHmmss}", @operator, DateTime.UtcNow);

        if (!await lockService.TryAcquireAsync(lockInfo, ct))
        {
            var existing = await lockService.GetCurrentLockAsync(applicationName, environmentName, targetName, ct);
            throw new InvalidOperationException(
                $"Deployment locked by {existing?.Operator} (DeploymentId: {existing?.DeploymentId}, since {existing?.AcquiredAt:yyyy-MM-dd HH:mm:ss}).");
        }

        var record = new DeploymentRecord
        {
            DeploymentId = lockInfo.DeploymentId,
            TargetId = target.Id,
            ReleaseId = release.Id,
            Status = DeploymentStatus.Created,
            Operator = @operator,
            CreatedAt = DateTime.UtcNow,
            IsDryRun = isDryRun
        };

        await deploymentRepo.AddAsync(record, ct);

        await audit.RecordAsync(new AuditEvent
        {
            EventType = "DEPLOYMENT_STARTED",
            DeploymentId = record.DeploymentId,
            ReleaseId = release.ReleaseId,
            Application = applicationName,
            Environment = environmentName,
            Target = targetName,
            Operator = @operator,
            Timestamp = DateTime.UtcNow,
            Result = isDryRun ? "DRY_RUN" : "STARTED"
        }, ct);

        try
        {
            await ExecuteDeploymentAsync(record, app, env, target, release, isDryRun, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Deployment {Id} failed", record.DeploymentId);
            await SetStatusAsync(record, DeploymentStatus.Failed, ex.Message, ct);
            await audit.RecordAsync(new AuditEvent
            {
                EventType = "DEPLOYMENT_FAILED",
                DeploymentId = record.DeploymentId,
                Application = applicationName,
                Environment = environmentName,
                Target = targetName,
                Operator = @operator,
                Timestamp = DateTime.UtcNow,
                Result = "FAILED",
                Details = ex.Message
            }, ct);
            throw;
        }
        finally
        {
            await lockService.ReleaseAsync(applicationName, environmentName, targetName, ct);
        }

        return record;
    }

    private async Task ExecuteDeploymentAsync(
        DeploymentRecord record,
        Domain.Entities.Application app,
        AppEnvironment env,
        DeploymentTarget target,
        Release release,
        bool isDryRun,
        CancellationToken ct)
    {
        // Step 1: Validate
        await RunStepAsync(record, 1, "Validation", async () =>
        {
            await SetStatusAsync(record, DeploymentStatus.Validating, null, ct);
            if (!fs.DirectoryExists(release.PackagePath))
                throw new InvalidOperationException($"Release package not found at: {release.PackagePath}");
        }, ct);

        // Step 2: Diff
        DiffResult? diff = null;
        await RunStepAsync(record, 2, "Diff", async () =>
        {
            await SetStatusAsync(record, DeploymentStatus.DiffReady, null, ct);
            diff = await diffService.ComputeAsync(
                release.PackagePath,
                target.DeploymentPath,
                release.Version,
                target.CurrentRelease ?? "EMPTY",
                ct);
        }, ct);

        if (isDryRun)
        {
            record.Status = DeploymentStatus.Completed;
            await deploymentRepo.UpdateAsync(record, ct);
            return;
        }

        // Step 3: Backup
        Backup? backup = null;
        await RunStepAsync(record, 3, "Backup", async () =>
        {
            await SetStatusAsync(record, DeploymentStatus.BackingUp, null, ct);
            backup = await backupService.CreateBackupAsync(
                app.Name, env.Name, target.Name,
                options.BackupStorePath, record.Operator, ct);
            await SetStatusAsync(record, DeploymentStatus.BackupVerified, null, ct);
        }, ct);

        if (backup == null || backup.Status != BackupStatus.Verified)
            throw new InvalidOperationException("Backup was not verified. Deployment aborted.");

        // Step 4: Deploy files
        await RunStepAsync(record, 4, "Deploy", async () =>
        {
            await SetStatusAsync(record, DeploymentStatus.Deploying, null, ct);

            if (!fs.DirectoryExists(target.DeploymentPath))
                fs.CreateDirectory(target.DeploymentPath);

            foreach (var releaseFile in release.Files)
            {
                ct.ThrowIfCancellationRequested();
                var srcFile = fs.CombinePath(release.PackagePath, releaseFile.RelativePath);
                var destFile = fs.CombinePath(target.DeploymentPath, releaseFile.RelativePath);
                var destDir = Path.GetDirectoryName(destFile)!;
                if (!fs.DirectoryExists(destDir))
                    fs.CreateDirectory(destDir);
                fs.CopyFile(srcFile, destFile, overwrite: true);
            }
        }, ct);

        // Step 5: Verify checksums
        await RunStepAsync(record, 5, "Checksum Verification", async () =>
        {
            await SetStatusAsync(record, DeploymentStatus.Verifying, null, ct);
            foreach (var releaseFile in release.Files)
            {
                ct.ThrowIfCancellationRequested();
                var destFile = fs.CombinePath(target.DeploymentPath, releaseFile.RelativePath);
                var ok = await checksum.VerifyAsync(destFile, releaseFile.Sha256, ct);
                if (!ok)
                    throw new InvalidOperationException($"Checksum mismatch for '{releaseFile.RelativePath}'. Deployment failed.");
            }
        }, ct);

        // Step 6: Update state
        await RunStepAsync(record, 6, "Record", async () =>
        {
            target.PreviousRelease = target.CurrentRelease;
            target.CurrentRelease = release.Version;
            target.LastDeploymentAt = DateTime.UtcNow;
            await appRepo.UpdateTargetAsync(target, ct);

            record.Status = DeploymentStatus.Completed;
            record.CompletedAt = DateTime.UtcNow;
            await deploymentRepo.UpdateAsync(record, ct);
        }, ct);

        await audit.RecordAsync(new AuditEvent
        {
            EventType = "DEPLOYMENT_COMPLETED",
            DeploymentId = record.DeploymentId,
            ReleaseId = release.ReleaseId,
            Application = app.Name,
            Environment = env.Name,
            Target = target.Name,
            Operator = record.Operator,
            Timestamp = DateTime.UtcNow,
            Result = "SUCCESS"
        }, ct);

        logger.LogInformation("Deployment {Id} completed successfully: {App}/{Env}/{Target} → {Version}",
            record.DeploymentId, app.Name, env.Name, target.Name, release.Version);
    }

    public async Task<DeploymentRecord> RollbackAsync(
        string applicationName,
        string environmentName,
        string targetName,
        string targetVersion,
        string @operator,
        CancellationToken ct = default)
    {
        var (app, env, target) = await ResolveAsync(applicationName, environmentName, targetName, ct);
        var release = await releaseRepo.GetByVersionAsync(app.Id, targetVersion, ct)
            ?? throw new InvalidOperationException($"Release '{targetVersion}' not found.");

        var lockInfo = new DeploymentLock(applicationName, environmentName, targetName,
            $"RBK-{DateTime.UtcNow:yyyyMMdd-HHmmss}", @operator, DateTime.UtcNow);

        if (!await lockService.TryAcquireAsync(lockInfo, ct))
            throw new InvalidOperationException("Target is locked by another operation.");

        var record = new DeploymentRecord
        {
            DeploymentId = lockInfo.DeploymentId,
            TargetId = target.Id,
            ReleaseId = release.Id,
            Status = DeploymentStatus.Created,
            Operator = @operator,
            CreatedAt = DateTime.UtcNow
        };
        await deploymentRepo.AddAsync(record, ct);

        try
        {
            await SetStatusAsync(record, DeploymentStatus.BackingUp, null, ct);
            var preRollbackBackup = await backupService.CreateBackupAsync(
                applicationName, environmentName, targetName,
                options.BackupStorePath, @operator, ct);

            if (preRollbackBackup.Status != BackupStatus.Verified)
                throw new InvalidOperationException("Pre-rollback backup failed. Rollback aborted.");

            await SetStatusAsync(record, DeploymentStatus.RollingBack, null, ct);

            if (!fs.DirectoryExists(target.DeploymentPath))
                fs.CreateDirectory(target.DeploymentPath);

            foreach (var releaseFile in release.Files)
            {
                ct.ThrowIfCancellationRequested();
                var srcFile = fs.CombinePath(release.PackagePath, releaseFile.RelativePath);
                var destFile = fs.CombinePath(target.DeploymentPath, releaseFile.RelativePath);
                var destDir = Path.GetDirectoryName(destFile)!;
                if (!fs.DirectoryExists(destDir))
                    fs.CreateDirectory(destDir);
                fs.CopyFile(srcFile, destFile, overwrite: true);
            }

            await SetStatusAsync(record, DeploymentStatus.Verifying, null, ct);
            foreach (var releaseFile in release.Files)
            {
                var destFile = fs.CombinePath(target.DeploymentPath, releaseFile.RelativePath);
                var ok = await checksum.VerifyAsync(destFile, releaseFile.Sha256, ct);
                if (!ok)
                    throw new InvalidOperationException($"Rollback checksum mismatch: {releaseFile.RelativePath}");
            }

            target.PreviousRelease = target.CurrentRelease;
            target.CurrentRelease = release.Version;
            target.LastDeploymentAt = DateTime.UtcNow;
            await appRepo.UpdateTargetAsync(target, ct);

            record.Status = DeploymentStatus.RolledBack;
            record.CompletedAt = DateTime.UtcNow;
            await deploymentRepo.UpdateAsync(record, ct);

            await audit.RecordAsync(new AuditEvent
            {
                EventType = "ROLLBACK_COMPLETED",
                DeploymentId = record.DeploymentId,
                ReleaseId = release.ReleaseId,
                Application = applicationName,
                Environment = environmentName,
                Target = targetName,
                Operator = @operator,
                Timestamp = DateTime.UtcNow,
                Result = "SUCCESS"
            }, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Rollback {Id} failed", record.DeploymentId);
            await SetStatusAsync(record, DeploymentStatus.Failed, ex.Message, ct);
            await audit.RecordAsync(new AuditEvent
            {
                EventType = "ROLLBACK_FAILED",
                DeploymentId = record.DeploymentId,
                Application = applicationName,
                Environment = environmentName,
                Target = targetName,
                Operator = @operator,
                Timestamp = DateTime.UtcNow,
                Result = "FAILED",
                Details = ex.Message
            }, ct);
            throw;
        }
        finally
        {
            await lockService.ReleaseAsync(applicationName, environmentName, targetName, ct);
        }

        return record;
    }

    public async Task<IReadOnlyList<DeploymentRecord>> GetHistoryAsync(
        string applicationName, string environmentName, string targetName, CancellationToken ct = default)
    {
        var (_, _, target) = await ResolveAsync(applicationName, environmentName, targetName, ct);
        return await deploymentRepo.ListByTargetAsync(target.Id, 50, ct);
    }

    public async Task<IReadOnlyList<DeploymentRecord>> GetIncompleteDeploymentsAsync(CancellationToken ct = default)
        => await deploymentRepo.GetIncompleteAsync(ct);

    private async Task RunStepAsync(DeploymentRecord record, int number, string name, Func<Task> action, CancellationToken ct)
    {
        var step = new DeploymentStep
        {
            DeploymentId = record.Id,
            StepNumber = number,
            Name = name,
            Status = StepStatus.Running,
            StartedAt = DateTime.UtcNow
        };
        await deploymentRepo.AddStepAsync(step, ct);

        try
        {
            await action();
            step.Status = StepStatus.Success;
            step.CompletedAt = DateTime.UtcNow;
            await deploymentRepo.UpdateStepAsync(step, ct);
        }
        catch
        {
            step.Status = StepStatus.Failed;
            step.CompletedAt = DateTime.UtcNow;
            await deploymentRepo.UpdateStepAsync(step, ct);
            throw;
        }
    }

    private async Task SetStatusAsync(DeploymentRecord record, DeploymentStatus status, string? failureReason, CancellationToken ct)
    {
        record.Status = status;
        if (failureReason != null) record.FailureReason = failureReason;
        await deploymentRepo.UpdateAsync(record, ct);
    }

    private async Task<(Domain.Entities.Application, AppEnvironment, DeploymentTarget)> ResolveAsync(
        string app, string env, string target, CancellationToken ct)
    {
        var appEntity = await appRepo.GetByNameAsync(app, ct)
            ?? throw new InvalidOperationException($"Application '{app}' not found.");
        var envEntity = await appRepo.GetEnvironmentAsync(appEntity.Id, env, ct)
            ?? throw new InvalidOperationException($"Environment '{env}' not found.");
        var targetEntity = await appRepo.GetTargetAsync(envEntity.Id, target, ct)
            ?? throw new InvalidOperationException($"Target '{target}' not found.");
        return (appEntity, envEntity, targetEntity);
    }
}
