using Deployment.Application.Interfaces;
using Deployment.Application.Services;

namespace Deployment.Desktop.Services;

/// <summary>Bundle of the services every screen needs, resolved once from DI at startup.</summary>
public sealed record AppServices(
    IApplicationRepository AppRepo,
    IReleaseService ReleaseService,
    IDeploymentService DeploymentService,
    IBackupService BackupService,
    IRetentionService RetentionService,
    IDiffService DiffService,
    IAuditService AuditService,
    IDeploymentRepository DeploymentRepo,
    DeploymentOptions Options,
    string DataDir);
