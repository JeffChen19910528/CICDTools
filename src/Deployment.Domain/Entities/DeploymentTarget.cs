namespace Deployment.Domain.Entities;

public enum TargetOS { Windows, Linux }

public class DeploymentTarget
{
    public int Id { get; set; }
    public int EnvironmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public TargetOS OS { get; set; }
    public string Host { get; set; } = "localhost";
    public string DeploymentPath { get; set; } = string.Empty;
    public string? CurrentRelease { get; set; }
    public string? PreviousRelease { get; set; }
    public DateTime? LastDeploymentAt { get; set; }

    public AppEnvironment Environment { get; set; } = null!;
    public ICollection<DeploymentRecord> Deployments { get; set; } = [];
    public ICollection<Backup> Backups { get; set; } = [];
}
