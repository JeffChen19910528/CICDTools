namespace Deployment.Domain.Entities;

public class RetentionPolicy
{
    public int Id { get; set; }
    public int EnvironmentId { get; set; }
    public int? RetainDays { get; set; }
    public int? RetainCount { get; set; }
    public int MinimumCount { get; set; } = 3;

    public AppEnvironment Environment { get; set; } = null!;
}
