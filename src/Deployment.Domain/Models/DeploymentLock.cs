namespace Deployment.Domain.Models;

public record DeploymentLock(
    string Application,
    string Environment,
    string Target,
    string DeploymentId,
    string Operator,
    DateTime AcquiredAt
);
