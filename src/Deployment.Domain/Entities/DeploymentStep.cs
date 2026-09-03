namespace Deployment.Domain.Entities;

public enum StepStatus { Pending, Running, Success, Failed, Skipped }

public class DeploymentStep
{
    public int Id { get; set; }
    public int DeploymentId { get; set; }
    public int StepNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public StepStatus Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Message { get; set; }

    public DeploymentRecord Deployment { get; set; } = null!;
}
