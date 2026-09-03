namespace Deployment.Domain.Entities;

public class AuditEvent
{
    public int Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? DeploymentId { get; set; }
    public string? BackupId { get; set; }
    public string? ReleaseId { get; set; }
    public string? Application { get; set; }
    public string? Environment { get; set; }
    public string? Target { get; set; }
    public string? Operator { get; set; }
    public DateTime Timestamp { get; set; }
    public string Result { get; set; } = string.Empty;
    public string? Details { get; set; }
}
