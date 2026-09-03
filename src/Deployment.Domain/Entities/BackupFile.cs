namespace Deployment.Domain.Entities;

public class BackupFile
{
    public int Id { get; set; }
    public int BackupId { get; set; }
    public string RelativePath { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Sha256 { get; set; } = string.Empty;

    public Backup Backup { get; set; } = null!;
}
