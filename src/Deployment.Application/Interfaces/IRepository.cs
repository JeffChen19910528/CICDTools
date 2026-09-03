using App = Deployment.Domain.Entities.Application;
using Deployment.Domain.Entities;

namespace Deployment.Application.Interfaces;

public interface IReleaseRepository
{
    Task<Release?> GetByReleaseIdAsync(string releaseId, CancellationToken ct = default);
    Task<Release?> GetByVersionAsync(int applicationId, string version, CancellationToken ct = default);
    Task<IReadOnlyList<Release>> ListAsync(int applicationId, CancellationToken ct = default);
    Task AddAsync(Release release, CancellationToken ct = default);
}

public interface IDeploymentRepository
{
    Task<DeploymentRecord?> GetByDeploymentIdAsync(string deploymentId, CancellationToken ct = default);
    Task<IReadOnlyList<DeploymentRecord>> ListByTargetAsync(int targetId, int limit = 20, CancellationToken ct = default);
    Task AddAsync(DeploymentRecord deployment, CancellationToken ct = default);
    Task UpdateAsync(DeploymentRecord deployment, CancellationToken ct = default);
    Task AddStepAsync(DeploymentStep step, CancellationToken ct = default);
    Task UpdateStepAsync(DeploymentStep step, CancellationToken ct = default);
    Task<IReadOnlyList<DeploymentRecord>> GetIncompleteAsync(CancellationToken ct = default);
}

public interface IBackupRepository
{
    Task<Backup?> GetByBackupIdAsync(string backupId, CancellationToken ct = default);
    Task<IReadOnlyList<Backup>> ListByTargetAsync(int targetId, CancellationToken ct = default);
    Task AddAsync(Backup backup, CancellationToken ct = default);
    Task UpdateAsync(Backup backup, CancellationToken ct = default);
    Task AddFilesAsync(IEnumerable<BackupFile> files, CancellationToken ct = default);
}

public interface IApplicationRepository
{
    Task<App?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<App>> ListAsync(CancellationToken ct = default);
    Task AddAsync(App app, CancellationToken ct = default);
    Task<AppEnvironment?> GetEnvironmentAsync(int appId, string envName, CancellationToken ct = default);
    Task<DeploymentTarget?> GetTargetAsync(int envId, string targetName, CancellationToken ct = default);
    Task AddEnvironmentAsync(AppEnvironment env, CancellationToken ct = default);
    Task AddTargetAsync(DeploymentTarget target, CancellationToken ct = default);
    Task UpdateTargetAsync(DeploymentTarget target, CancellationToken ct = default);
    Task<RetentionPolicy?> GetRetentionPolicyAsync(int environmentId, CancellationToken ct = default);
    Task UpsertRetentionPolicyAsync(RetentionPolicy policy, CancellationToken ct = default);
}
