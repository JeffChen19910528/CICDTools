using Deployment.Application.Interfaces;
using Deployment.Domain.Entities;
using Deployment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Deployment.Infrastructure.Repositories;

public class ApplicationRepository(DeploymentDbContext db) : IApplicationRepository
{
    public async Task<Domain.Entities.Application?> GetByNameAsync(string name, CancellationToken ct = default)
        => await db.Applications.Include(a => a.Environments).ThenInclude(e => e.Targets)
            .FirstOrDefaultAsync(a => a.Name == name, ct);

    public async Task<IReadOnlyList<Domain.Entities.Application>> ListAsync(CancellationToken ct = default)
        => await db.Applications.Include(a => a.Environments).ThenInclude(e => e.Targets).ToListAsync(ct);

    public async Task AddAsync(Domain.Entities.Application app, CancellationToken ct = default)
    {
        app.CreatedAt = DateTime.UtcNow;
        db.Applications.Add(app);
        await db.SaveChangesAsync(ct);
    }

    public async Task<AppEnvironment?> GetEnvironmentAsync(int appId, string envName, CancellationToken ct = default)
        => await db.Environments.Include(e => e.Targets)
            .FirstOrDefaultAsync(e => e.ApplicationId == appId && e.Name == envName, ct);

    public async Task<DeploymentTarget?> GetTargetAsync(int envId, string targetName, CancellationToken ct = default)
        => await db.Targets.FirstOrDefaultAsync(t => t.EnvironmentId == envId && t.Name == targetName, ct);

    public async Task AddEnvironmentAsync(AppEnvironment env, CancellationToken ct = default)
    {
        db.Environments.Add(env);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddTargetAsync(DeploymentTarget target, CancellationToken ct = default)
    {
        db.Targets.Add(target);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateTargetAsync(DeploymentTarget target, CancellationToken ct = default)
    {
        db.Targets.Update(target);
        await db.SaveChangesAsync(ct);
    }

    public async Task<RetentionPolicy?> GetRetentionPolicyAsync(int environmentId, CancellationToken ct = default)
        => await db.RetentionPolicies.FirstOrDefaultAsync(p => p.EnvironmentId == environmentId, ct);

    public async Task UpsertRetentionPolicyAsync(RetentionPolicy policy, CancellationToken ct = default)
    {
        var existing = await db.RetentionPolicies.FirstOrDefaultAsync(p => p.EnvironmentId == policy.EnvironmentId, ct);
        if (existing == null) db.RetentionPolicies.Add(policy);
        else { existing.RetainDays = policy.RetainDays; existing.RetainCount = policy.RetainCount; existing.MinimumCount = policy.MinimumCount; }
        await db.SaveChangesAsync(ct);
    }
}
