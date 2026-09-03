using Deployment.Application.Interfaces;
using Deployment.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Deployment.Application.Services;

public class DiffService(IFileSystem fs, IChecksumService checksum, ILogger<DiffService> logger) : IDiffService
{
    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".json", ".xml", ".yaml", ".yml", ".ini", ".config", ".conf",
        ".properties", ".env", ".sh", ".bat", ".cmd", ".ps1", ".py", ".js",
        ".ts", ".cs", ".java", ".go", ".rb", ".php", ".html", ".htm", ".css",
        ".sql", ".md", ".log", ".toml"
    };

    public async Task<DiffResult> ComputeAsync(
        string sourceDirectory,
        string targetDirectory,
        string sourceVersion,
        string targetVersion,
        CancellationToken ct = default)
    {
        logger.LogInformation("Computing diff: {Src} vs {Tgt}", sourceVersion, targetVersion);

        var sourceFiles = BuildFileMap(sourceDirectory);
        var targetFiles = BuildFileMap(targetDirectory);

        var allKeys = sourceFiles.Keys.Union(targetFiles.Keys).ToList();
        var diffs = new List<FileDiff>();

        foreach (var key in allKeys)
        {
            ct.ThrowIfCancellationRequested();
            var hasSrc = sourceFiles.TryGetValue(key, out var srcPath);
            var hasTgt = targetFiles.TryGetValue(key, out var tgtPath);

            if (hasSrc && !hasTgt)
            {
                diffs.Add(await BuildDiffAsync(key, srcPath!, null, FileChangeType.Added, ct));
            }
            else if (!hasSrc && hasTgt)
            {
                diffs.Add(await BuildDiffAsync(key, null, tgtPath!, FileChangeType.Deleted, ct));
            }
            else
            {
                var srcHash = await checksum.ComputeSha256Async(srcPath!, ct);
                var tgtHash = await checksum.ComputeSha256Async(tgtPath!, ct);

                var changeType = srcHash == tgtHash ? FileChangeType.Unchanged : FileChangeType.Modified;
                diffs.Add(await BuildDiffAsync(key, srcPath!, tgtPath!, changeType, ct));
            }
        }

        diffs.Sort((a, b) => string.Compare(a.RelativePath, b.RelativePath, StringComparison.OrdinalIgnoreCase));
        return new DiffResult(sourceVersion, targetVersion, diffs);
    }

    private Dictionary<string, string> BuildFileMap(string directory)
    {
        if (!fs.DirectoryExists(directory))
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        return fs.EnumerateFiles(directory)
            .ToDictionary(
                f => fs.GetRelativePath(directory, f),
                f => f,
                StringComparer.OrdinalIgnoreCase);
    }

    private async Task<FileDiff> BuildDiffAsync(
        string relativePath,
        string? srcPath,
        string? tgtPath,
        FileChangeType changeType,
        CancellationToken ct)
    {
        var srcSize = srcPath != null && fs.FileExists(srcPath) ? fs.GetFileSize(srcPath) : (long?)null;
        var tgtSize = tgtPath != null && fs.FileExists(tgtPath) ? fs.GetFileSize(tgtPath) : (long?)null;

        string? srcHash = srcPath != null ? await checksum.ComputeSha256Async(srcPath, ct) : null;
        string? tgtHash = tgtPath != null ? await checksum.ComputeSha256Async(tgtPath, ct) : null;

        var ext = Path.GetExtension(relativePath);
        var isBinary = !TextExtensions.Contains(ext) || fs.IsBinaryFile(srcPath ?? tgtPath!);

        IReadOnlyList<LineDiff>? lineDiffs = null;
        if (!isBinary && changeType == FileChangeType.Modified && srcPath != null && tgtPath != null)
        {
            lineDiffs = ComputeLineDiffs(srcPath, tgtPath);
        }

        return new FileDiff(relativePath, changeType, srcSize, tgtSize, srcHash, tgtHash, isBinary, lineDiffs);
    }

    private IReadOnlyList<LineDiff> ComputeLineDiffs(string srcPath, string tgtPath)
    {
        var srcLines = fs.ReadAllLines(srcPath);
        var tgtLines = fs.ReadAllLines(tgtPath);
        // old=tgt (current deployment), new=src (incoming release) so Added=coming in, Removed=going out
        return Myers.Diff(tgtLines, srcLines);
    }
}

internal static class Myers
{
    public static IReadOnlyList<LineDiff> Diff(string[] oldLines, string[] newLines)
    {
        var result = new List<LineDiff>();
        var lcs = ComputeLcs(oldLines, newLines);

        int oldIdx = 0, newIdx = 0, lcsIdx = 0;
        while (oldIdx < oldLines.Length || newIdx < newLines.Length)
        {
            if (lcsIdx < lcs.Count
                && oldIdx < oldLines.Length && newIdx < newLines.Length
                && oldLines[oldIdx] == lcs[lcsIdx] && newLines[newIdx] == lcs[lcsIdx])
            {
                result.Add(new LineDiff(oldIdx + 1, newIdx + 1, oldLines[oldIdx], LineDiffType.Context));
                oldIdx++; newIdx++; lcsIdx++;
            }
            else if (oldIdx < oldLines.Length && (lcsIdx >= lcs.Count || oldLines[oldIdx] != lcs[lcsIdx]))
            {
                result.Add(new LineDiff(oldIdx + 1, null, oldLines[oldIdx], LineDiffType.Removed));
                oldIdx++;
            }
            else
            {
                result.Add(new LineDiff(null, newIdx + 1, newLines[newIdx], LineDiffType.Added));
                newIdx++;
            }
        }
        return result;
    }

    private static List<string> ComputeLcs(string[] a, string[] b)
    {
        int m = a.Length, n = b.Length;
        var dp = new int[m + 1, n + 1];
        for (int i = m - 1; i >= 0; i--)
            for (int j = n - 1; j >= 0; j--)
                dp[i, j] = a[i] == b[j] ? dp[i + 1, j + 1] + 1 : Math.Max(dp[i + 1, j], dp[i, j + 1]);

        var lcs = new List<string>();
        int x = 0, y = 0;
        while (x < m && y < n)
        {
            if (a[x] == b[y]) { lcs.Add(a[x]); x++; y++; }
            else if (dp[x + 1, y] >= dp[x, y + 1]) x++;
            else y++;
        }
        return lcs;
    }
}
