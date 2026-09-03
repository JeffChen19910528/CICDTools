namespace Deployment.Domain.Models;

public enum FileChangeType { Added, Modified, Deleted, Unchanged, Renamed }

public record FileDiff(
    string RelativePath,
    FileChangeType ChangeType,
    long? SourceSize,
    long? TargetSize,
    string? SourceSha256,
    string? TargetSha256,
    bool IsBinary,
    IReadOnlyList<LineDiff>? LineDiffs
);

public record LineDiff(int? OldLineNumber, int? NewLineNumber, string Content, LineDiffType Type);

public enum LineDiffType { Context, Added, Removed }

public record DiffResult(
    string SourceVersion,
    string TargetVersion,
    IReadOnlyList<FileDiff> Files
)
{
    public int Added => Files.Count(f => f.ChangeType == FileChangeType.Added);
    public int Modified => Files.Count(f => f.ChangeType == FileChangeType.Modified);
    public int Deleted => Files.Count(f => f.ChangeType == FileChangeType.Deleted);
    public int Unchanged => Files.Count(f => f.ChangeType == FileChangeType.Unchanged);
    public bool HasChanges => Added > 0 || Modified > 0 || Deleted > 0;
}
