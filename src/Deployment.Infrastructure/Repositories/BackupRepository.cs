using Deployment.Application.Interfaces;
using Deployment.Domain.Entities;
using Deployment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Deployment.Infrastructure.Repositories;

public class BackupRepository(DeploymentDbContext db) : IBackupRepository
{
    public async Task<Backup?> GetByBackupIdAsync(string backupId, CancellationToken ct = default)
        => await db.Backups.Include(b => b.Files).FirstOrDefaultAsync(b => b.BackupId == backupId, ct);

    public async Task<IReadOnlyList<Backup>> ListByTargetAsync(int targetId, CancellationToken ct = default)
        => await db.Backups.Where(b => b.TargetId == targetId).OrderByDescending(b => b.CreatedAt).ToListAsync(ct);

    public async Task AddAsync(Backup backup, CancellationToken ct = default)
    {
        db.Backups.Add(backup);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Backup backup, CancellationToken ct = default)
    {
        db.Backups.Update(backup);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddFilesAsync(IEnumerable<BackupFile> files, CancellationToken ct = default)
    {
        db.BackupFiles.AddRange(files);
        await db.SaveChangesAsync(ct);
    }
}
