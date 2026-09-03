namespace Deployment.Domain.Entities;

public class Release
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public string ReleaseId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string? CommitId { get; set; }
    public string? BuildId { get; set; }
    public string PackagePath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public Application Application { get; set; } = null!;
    public ICollection<ReleaseFile> Files { get; set; } = [];
}
