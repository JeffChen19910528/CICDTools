using Deployment.Application.Interfaces;
using Deployment.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Deployment.Application.Services;

public class BackupService(
    ITargetResolver resolver,
    IBackupRepository backupRepo,
    IFileSystem fs,
    IChecksumService checksum,
    IAuditService audit,
    ILogger<BackupService> logger) : IBackupService
{
    public async Task<Backup> CreateBackupAsync(
        string applicationName,
        string environmentName,
        string targetName,
        string backupStorePath,
        string createdBy,
        CancellationToken ct = default)
    {
        var (_, _, target) = await resolver.ResolveTargetAsync(applicationName, environmentName, targetName, ct);

        if (!fs.DirectoryExists(target.DeploymentPath))
        {
            logger.LogWarning("Target deployment path does not exist; creating empty backup marker: {Path}", target.DeploymentPath);
        }

        var timestamp = DateTime.UtcNow;
        var backupId = $"BKP-{timestamp:yyyyMMdd-HHmmss}";
        var version = target.CurrentRelease ?? "UNKNOWN";
        var backupPath = fs.CombinePath(backupStorePath, applicationName, environmentName, targetName,
            $"{timestamp:yyyyMMdd-HHmmss}-{version}");

        fs.CreateDirectory(backupPath);

        logger.LogInformation("Creating backup {BackupId} from {Path}", backupId, target.DeploymentPath);

        var backup = new Backup
        {
            BackupId = backupId,
            TargetId = target.Id,
            ReleaseVersion = target.CurrentRelease,
            BackupPath = backupPath,
            Status = BackupStatus.Creating,
            CreatedAt = timestamp,
            CreatedBy = createdBy
        };
        await backupRepo.AddAsync(backup, ct);

        var files = new List<BackupFile>();
        long totalBytes = 0;

        if (fs.DirectoryExists(target.DeploymentPath))
        {
            foreach (var srcFile in fs.EnumerateFiles(target.DeploymentPath))
            {
                ct.ThrowIfCancellationRequested();
                var relativePath = fs.GetRelativePath(target.DeploymentPath, srcFile);
                var destFile = fs.CombinePath(backupPath, relativePath);

                var destDir = Path.GetDirectoryName(destFile)!;
                if (!fs.DirectoryExists(destDir))
                    fs.CreateDirectory(destDir);

                fs.CopyFile(srcFile, destFile, overwrite: true);

                var sha256 = await checksum.ComputeSha256Async(destFile, ct);
                var size = fs.GetFileSize(destFile);
                totalBytes += size;
                files.Add(new BackupFile { BackupId = backup.Id, RelativePath = relativePath, Size = size, Sha256 = sha256 });
            }
        }

        await backupRepo.AddFilesAsync(files, ct);
        backup.FileCount = files.Count;
        backup.TotalBytes = totalBytes;

        var verified = await VerifyBackupAsync(backup, target.DeploymentPath, ct);
        backup.Status = verified ? BackupStatus.Verified : BackupStatus.Failed;
        await backupRepo.UpdateAsync(backup, ct);

        if (!verified)
        {
            await audit.RecordAsync(new AuditEvent
            {
                EventType = "BACKUP_FAILED",
                BackupId = backupId,
                Application = applicationName,
                Environment = environmentName,
                Target = targetName,
                Operator = createdBy,
                Timestamp = DateTime.UtcNow,
                Result = "FAILED"
            }, ct);
            throw new InvalidOperationException($"Backup verification failed for {backupId}. Deployment must not proceed.");
        }

        await audit.RecordAsync(new AuditEvent
        {
            EventType = "BACKUP_CREATED",
            BackupId = backupId,
            Application = applicationName,
            Environment = environmentName,
            Target = targetName,
            Operator = createdBy,
            Timestamp = DateTime.UtcNow,
            Result = "SUCCESS",
            Details = $"Files={files.Count}, Bytes={totalBytes}"
        }, ct);

        logger.LogInformation("Backup {BackupId} verified successfully ({Count} files)", backupId, files.Count);
        return backup;
    }

    public async Task<Backup> RestoreBackupAsync(
        Backup backup,
        string targetPath,
        CancellationToken ct = default)
    {
        if (backup.Status != BackupStatus.Verified)
            throw new InvalidOperationException($"Cannot restore backup {backup.BackupId}: status is {backup.Status}.");

        logger.LogInformation("Restoring backup {BackupId} to {Path}", backup.BackupId, targetPath);

        if (fs.DirectoryExists(targetPath))
            fs.DeleteDirectory(targetPath, recursive: true);

        fs.CreateDirectory(targetPath);
        fs.CopyDirectory(backup.BackupPath, targetPath, overwrite: true, ct);

        logger.LogInformation("Backup {BackupId} restored to {Path}", backup.BackupId, targetPath);
        return backup;
    }

    public async Task<IReadOnlyList<Backup>> ListBackupsAsync(
        string applicationName, string environmentName, string targetName, CancellationToken ct = default)
    {
        var (_, _, target) = await resolver.ResolveTargetAsync(applicationName, environmentName, targetName, ct);
        return await backupRepo.ListByTargetAsync(target.Id, ct);
    }

    public async Task ProtectBackupAsync(string backupId, bool protect, CancellationToken ct = default)
    {
        var backup = await backupRepo.GetByBackupIdAsync(backupId, ct)
            ?? throw new InvalidOperationException($"Backup '{backupId}' not found.");
        backup.IsProtected = protect;
        await backupRepo.UpdateAsync(backup, ct);
    }

    private async Task<bool> VerifyBackupAsync(Backup backup, string sourceDirectory, CancellationToken ct)
    {
        if (!fs.DirectoryExists(backup.BackupPath)) return false;
        var backupFiles = fs.EnumerateFiles(backup.BackupPath).ToList();
        if (backupFiles.Count != backup.FileCount) return false;

        foreach (var backupFile in backup.Files)
        {
            ct.ThrowIfCancellationRequested();
            var fullPath = fs.CombinePath(backup.BackupPath, backupFile.RelativePath);
            if (!fs.FileExists(fullPath)) return false;
            var actualHash = await checksum.ComputeSha256Async(fullPath, ct);
            if (!string.Equals(actualHash, backupFile.Sha256, StringComparison.OrdinalIgnoreCase)) return false;
        }

        return true;
    }
}
