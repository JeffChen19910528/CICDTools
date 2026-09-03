using Deployment.Domain.Entities;
using Deployment.Domain.Models;

namespace Deployment.Application.Interfaces;

public interface IReleaseService
{
    Task<Release> CreateReleaseAsync(
        string applicationName,
        string version,
        string sourceDirectory,
        string releasesStorePath,
        string createdBy,
        string? commitId = null,
        string? buildId = null,
        string? notes = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<Release>> ListReleasesAsync(string applicationName, CancellationToken ct = default);

    Task<Release> GetReleaseAsync(string releaseId, CancellationToken ct = default);
}

public interface IBackupService
{
    Task<Backup> CreateBackupAsync(
        string applicationName,
        string environmentName,
        string targetName,
        string backupStorePath,
        string createdBy,
        CancellationToken ct = default);

    Task<Backup> RestoreBackupAsync(Backup backup, string targetPath, CancellationToken ct = default);

    Task<IReadOnlyList<Backup>> ListBackupsAsync(
        string applicationName, string environmentName, string targetName, CancellationToken ct = default);

    Task ProtectBackupAsync(string backupId, bool protect, CancellationToken ct = default);
}

public interface IDiffService
{
    Task<DiffResult> ComputeAsync(
        string sourceDirectory,
        string targetDirectory,
        string sourceVersion,
        string targetVersion,
        CancellationToken ct = default);
}

public interface IDeploymentService
{
    Task<DeploymentRecord> StartDeploymentAsync(
        string applicationName,
        string environmentName,
        string targetName,
        string releaseVersion,
        string @operator,
        bool isDryRun = false,
        CancellationToken ct = default);

    Task<DeploymentRecord> RollbackAsync(
        string applicationName,
        string environmentName,
        string targetName,
        string targetVersion,
        string @operator,
        CancellationToken ct = default);

    Task<IReadOnlyList<DeploymentRecord>> GetHistoryAsync(
        string applicationName, string environmentName, string targetName, CancellationToken ct = default);

    Task<IReadOnlyList<DeploymentRecord>> GetIncompleteDeploymentsAsync(CancellationToken ct = default);
}

public interface IRetentionService
{
    Task ApplyRetentionAsync(
        string applicationName,
        string environmentName,
        string targetName,
        string @operator,
        CancellationToken ct = default);
}
