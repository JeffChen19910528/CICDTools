using Deployment.Application.Interfaces;
using Deployment.Domain.Entities;
using Deployment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Deployment.Infrastructure.Repositories;

public class DeploymentRepository(DeploymentDbContext db) : IDeploymentRepository
{
    private static readonly DeploymentStatus[] IncompleteStatuses =
    [
        DeploymentStatus.Created, DeploymentStatus.Validating, DeploymentStatus.DiffReady,
        DeploymentStatus.WaitingApproval, DeploymentStatus.BackingUp, DeploymentStatus.BackupVerified,
        DeploymentStatus.Deploying, DeploymentStatus.Verifying, DeploymentStatus.RollingBack
    ];

    public async Task<DeploymentRecord?> GetByDeploymentIdAsync(string deploymentId, CancellationToken ct = default)
        => await db.Deployments.Include(d => d.Steps).Include(d => d.Release).Include(d => d.Target)
            .FirstOrDefaultAsync(d => d.DeploymentId == deploymentId, ct);

    public async Task<IReadOnlyList<DeploymentRecord>> ListByTargetAsync(int targetId, int limit = 20, CancellationToken ct = default)
        => await db.Deployments.Include(d => d.Release)
            .Where(d => d.TargetId == targetId)
            .OrderByDescending(d => d.CreatedAt).Take(limit).ToListAsync(ct);

    public async Task AddAsync(DeploymentRecord deployment, CancellationToken ct = default)
    {
        db.Deployments.Add(deployment);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(DeploymentRecord deployment, CancellationToken ct = default)
    {
        db.Deployments.Update(deployment);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddStepAsync(DeploymentStep step, CancellationToken ct = default)
    {
        db.DeploymentSteps.Add(step);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateStepAsync(DeploymentStep step, CancellationToken ct = default)
    {
        db.DeploymentSteps.Update(step);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<DeploymentRecord>> GetIncompleteAsync(CancellationToken ct = default)
        => await db.Deployments.Include(d => d.Steps).Include(d => d.Release).Include(d => d.Target)
            .Where(d => IncompleteStatuses.Contains(d.Status)).ToListAsync(ct);
}
