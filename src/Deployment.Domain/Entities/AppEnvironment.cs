namespace Deployment.Domain.Entities;

public class AppEnvironment
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool RequireApproval { get; set; }
    public bool AllowYesFlag { get; set; } = true;

    public Application Application { get; set; } = null!;
    public ICollection<DeploymentTarget> Targets { get; set; } = [];
    public RetentionPolicy? RetentionPolicy { get; set; }
}
