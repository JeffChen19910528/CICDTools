using Deployment.Application.Services;
using Deployment.Domain.Models;
using Deployment.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Deployment.Tests;

public class DiffServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly DiffService _svc;

    public DiffServiceTests()
    {
        _svc = new DiffService(new LocalFileSystem(), new Sha256ChecksumService(), NullLogger<DiffService>.Instance);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    [Fact]
    public async Task Diff_AddedFiles_DetectsCorrectly()
    {
        var src = CreateDir("src", new() { ["newfile.txt"] = "new" });
        var tgt = CreateDir("tgt", new());

        var result = await _svc.ComputeAsync(src, tgt, "v2", "v1");

        Assert.Equal(1, result.Added);
        Assert.Equal(0, result.Modified);
        Assert.Equal(0, result.Deleted);
        Assert.True(result.HasChanges);
    }

    [Fact]
    public async Task Diff_DeletedFiles_DetectsCorrectly()
    {
        var src = CreateDir("src", new());
        var tgt = CreateDir("tgt", new() { ["oldfile.txt"] = "old" });

        var result = await _svc.ComputeAsync(src, tgt, "v2", "v1");

        Assert.Equal(0, result.Added);
        Assert.Equal(1, result.Deleted);
    }

    [Fact]
    public async Task Diff_ModifiedFile_DetectsLineDifferences()
    {
        var src = CreateDir("src", new() { ["config.txt"] = "ConnectionTimeout=60\nMaxRetry=5" });
        var tgt = CreateDir("tgt", new() { ["config.txt"] = "ConnectionTimeout=30\nMaxRetry=5" });

        var result = await _svc.ComputeAsync(src, tgt, "v2", "v1");

        Assert.Equal(1, result.Modified);
        var diff = result.Files.First(f => f.ChangeType == FileChangeType.Modified);
        Assert.False(diff.IsBinary);
        Assert.NotNull(diff.LineDiffs);
        Assert.Contains(diff.LineDiffs!, l => l.Type == LineDiffType.Added && l.Content.Contains("60"));
        Assert.Contains(diff.LineDiffs!, l => l.Type == LineDiffType.Removed && l.Content.Contains("30"));
    }

    [Fact]
    public async Task Diff_UnchangedFile_IsUnchanged()
    {
        var content = "same content";
        var src = CreateDir("src", new() { ["file.txt"] = content });
        var tgt = CreateDir("tgt", new() { ["file.txt"] = content });

        var result = await _svc.ComputeAsync(src, tgt, "v2", "v1");

        Assert.Equal(1, result.Unchanged);
        Assert.False(result.HasChanges);
    }

    [Fact]
    public async Task Diff_EmptyTargetDirectory_AllFilesAreAdded()
    {
        var src = CreateDir("src", new()
        {
            ["a.txt"] = "a",
            ["b.txt"] = "b",
            ["sub/c.txt"] = "c"
        });
        var tgt = CreateDir("tgt", new());

        var result = await _svc.ComputeAsync(src, tgt, "v1", "EMPTY");

        Assert.Equal(3, result.Added);
    }

    private string CreateDir(string name, Dictionary<string, string> files)
    {
        var dir = Path.Combine(_tempDir, name);
        Directory.CreateDirectory(dir);
        foreach (var (path, content) in files)
        {
            var fullPath = Path.Combine(dir, path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, content);
        }
        return dir;
    }
}
