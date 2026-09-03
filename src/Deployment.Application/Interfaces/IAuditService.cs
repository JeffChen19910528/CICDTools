using Deployment.Domain.Entities;

namespace Deployment.Application.Interfaces;

public interface IAuditService
{
    Task RecordAsync(AuditEvent evt, CancellationToken ct = default);
    Task<IReadOnlyList<AuditEvent>> GetByDeploymentAsync(string deploymentId, CancellationToken ct = default);
    Task<IReadOnlyList<AuditEvent>> GetRecentAsync(string? application, string? environment, int limit = 50, CancellationToken ct = default);
}
