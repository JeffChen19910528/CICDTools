using Deployment.Application.Interfaces;
using Deployment.Domain.Entities;
using Deployment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Deployment.Infrastructure.Repositories;

public class AuditRepository(DeploymentDbContext db) : IAuditService
{
    public async Task RecordAsync(AuditEvent evt, CancellationToken ct = default)
    {
        db.AuditEvents.Add(evt);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AuditEvent>> GetByDeploymentAsync(string deploymentId, CancellationToken ct = default)
        => await db.AuditEvents.Where(e => e.DeploymentId == deploymentId)
            .OrderBy(e => e.Timestamp).ToListAsync(ct);

    public async Task<IReadOnlyList<AuditEvent>> GetRecentAsync(string? application, string? environment, int limit = 50, CancellationToken ct = default)
    {
        var query = db.AuditEvents.AsQueryable();
        if (application != null) query = query.Where(e => e.Application == application);
        if (environment != null) query = query.Where(e => e.Environment == environment);
        return await query.OrderByDescending(e => e.Timestamp).Take(limit).ToListAsync(ct);
    }
}
