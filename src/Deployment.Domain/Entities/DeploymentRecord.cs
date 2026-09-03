namespace Deployment.Domain.Entities;

public enum DeploymentStatus
{
    Created, Validating, DiffReady, WaitingApproval,
    BackingUp, BackupVerified, Deploying, Verifying,
    Completed, Failed, RollingBack, RolledBack, Cancelled
}

public class DeploymentRecord
{
    public int Id { get; set; }
    public string DeploymentId { get; set; } = string.Empty;
    public int TargetId { get; set; }
    public int ReleaseId { get; set; }
    public DeploymentStatus Status { get; set; }
    public string Operator { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovalComment { get; set; }
    public string? FailureReason { get; set; }
    public bool IsDryRun { get; set; }

    public DeploymentTarget Target { get; set; } = null!;
    public Release Release { get; set; } = null!;
    public ICollection<DeploymentStep> Steps { get; set; } = [];
}
