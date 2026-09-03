using Deployment.Application.Interfaces;
using Deployment.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Deployment.Application.Services;

public class RetentionService(
    ITargetResolver resolver,
    IApplicationRepository appRepo,
    IBackupRepository backupRepo,
    IFileSystem fs,
    IAuditService audit,
    ILogger<RetentionService> logger) : IRetentionService
{
    public async Task ApplyRetentionAsync(
        string applicationName,
        string environmentName,
        string targetName,
        string @operator,
        CancellationToken ct = default)
    {
        var (_, env, target) = await resolver.ResolveTargetAsync(applicationName, environmentName, targetName, ct);
        var policy = await appRepo.GetRetentionPolicyAsync(env.Id, ct);

        if (policy == null)
        {
            logger.LogInformation("No retention policy defined for {App}/{Env}/{Target}. Skipping.", applicationName, environmentName, targetName);
            return;
        }

        var backups = (await backupRepo.ListByTargetAsync(target.Id, ct))
            .Where(b => b.Status == BackupStatus.Verified)
            .OrderByDescending(b => b.CreatedAt)
            .ToList();

        var toDelete = DetermineBackupsToDelete(backups, policy);

        foreach (var backup in toDelete)
        {
            ct.ThrowIfCancellationRequested();

            if (backup.IsProtected)
            {
                logger.LogWarning("Skipping protected backup {Id}", backup.BackupId);
                continue;
            }

            logger.LogInformation("Deleting backup {Id} (retention cleanup)", backup.BackupId);

            if (fs.DirectoryExists(backup.BackupPath))
                fs.DeleteDirectory(backup.BackupPath, recursive: true);

            backup.Status = BackupStatus.Deleted;
            await backupRepo.UpdateAsync(backup, ct);

            await audit.RecordAsync(new AuditEvent
            {
                EventType = "BACKUP_DELETED",
                BackupId = backup.BackupId,
                Application = applicationName,
                Environment = environmentName,
                Target = targetName,
                Operator = @operator,
                Timestamp = DateTime.UtcNow,
                Result = "SUCCESS",
                Details = "Retention cleanup"
            }, ct);
        }

        logger.LogInformation("Retention cleanup complete. Deleted {Count} backups.", toDelete.Count);
    }

    private List<Backup> DetermineBackupsToDelete(List<Backup> backups, RetentionPolicy policy)
    {
        var toDelete = new HashSet<int>();
        var cutoff = policy.RetainDays.HasValue ? DateTime.UtcNow.AddDays(-policy.RetainDays.Value) : (DateTime?)null;

        for (int i = 0; i < backups.Count; i++)
        {
            var b = backups[i];

            if (b.IsProtected) continue;

            bool withinMinimum = i < policy.MinimumCount;
            bool withinCount = !policy.RetainCount.HasValue || i < policy.RetainCount.Value;
            bool withinDays = !cutoff.HasValue || b.CreatedAt >= cutoff;

            if (withinMinimum) continue;
            if (withinDays && withinCount) continue;

            toDelete.Add(b.Id);
        }

        return backups.Where(b => toDelete.Contains(b.Id)).ToList();
    }
}
