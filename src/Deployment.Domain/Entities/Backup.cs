namespace Deployment.Domain.Entities;

public enum BackupStatus { Creating, Verified, Failed, Deleted }

public class Backup
{
    public int Id { get; set; }
    public string BackupId { get; set; } = string.Empty;
    public int TargetId { get; set; }
    public string? ReleaseVersion { get; set; }
    public string BackupPath { get; set; } = string.Empty;
    public BackupStatus Status { get; set; }
    public bool IsProtected { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public long? FileCount { get; set; }
    public long? TotalBytes { get; set; }
    public string? Checksum { get; set; }
    public string? Description { get; set; }

    public DeploymentTarget Target { get; set; } = null!;
    public ICollection<BackupFile> Files { get; set; } = [];
}
