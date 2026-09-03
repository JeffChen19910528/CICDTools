using Deployment.Domain.Models;

namespace Deployment.Application.Interfaces;

public interface ILockService
{
    Task<bool> TryAcquireAsync(DeploymentLock lockInfo, CancellationToken ct = default);
    Task ReleaseAsync(string application, string environment, string target, CancellationToken ct = default);
    Task<DeploymentLock?> GetCurrentLockAsync(string application, string environment, string target, CancellationToken ct = default);
}
