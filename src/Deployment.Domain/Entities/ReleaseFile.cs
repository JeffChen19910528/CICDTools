namespace Deployment.Domain.Entities;

public class ReleaseFile
{
    public int Id { get; set; }
    public int ReleaseId { get; set; }
    public string RelativePath { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Sha256 { get; set; } = string.Empty;

    public Release Release { get; set; } = null!;
}
