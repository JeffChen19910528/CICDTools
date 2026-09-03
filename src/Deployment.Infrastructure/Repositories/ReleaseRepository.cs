using Deployment.Application.Interfaces;
using Deployment.Domain.Entities;
using Deployment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Deployment.Infrastructure.Repositories;

public class ReleaseRepository(DeploymentDbContext db) : IReleaseRepository
{
    public async Task<Release?> GetByReleaseIdAsync(string releaseId, CancellationToken ct = default)
        => await db.Releases.Include(r => r.Files).FirstOrDefaultAsync(r => r.ReleaseId == releaseId, ct);

    public async Task<Release?> GetByVersionAsync(int applicationId, string version, CancellationToken ct = default)
        => await db.Releases.Include(r => r.Files)
            .FirstOrDefaultAsync(r => r.ApplicationId == applicationId && r.Version == version, ct);

    public async Task<IReadOnlyList<Release>> ListAsync(int applicationId, CancellationToken ct = default)
        => await db.Releases.Where(r => r.ApplicationId == applicationId)
            .OrderByDescending(r => r.CreatedAt).ToListAsync(ct);

    public async Task AddAsync(Release release, CancellationToken ct = default)
    {
        db.Releases.Add(release);
        await db.SaveChangesAsync(ct);
    }
}
