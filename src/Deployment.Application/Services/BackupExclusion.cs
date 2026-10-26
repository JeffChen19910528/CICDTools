namespace Deployment.Application.Services;

/// <summary>Decides which files under a deployment directory are left out of backups.</summary>
public static class BackupExclusion
{
    public static string[] Parse(string? excludes)
        => (excludes ?? string.Empty)
            .Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => p.Replace('\\', '/').Trim('/'))
            .Where(p => p.Length > 0)
            .ToArray();

    /// <summary>A pattern without '/' matches a directory with that name at any depth;
    /// a pattern with '/' matches that relative path prefix. Case-insensitive.</summary>
    public static bool IsExcluded(string relativePath, IReadOnlyCollection<string> patterns)
    {
        if (patterns.Count == 0) return false;
        var segments = relativePath.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var dirs = segments.Take(segments.Length - 1).ToArray();
        var dirPath = string.Join('/', dirs);

        foreach (var pattern in patterns)
        {
            if (pattern.Contains('/'))
            {
                if (dirPath.Equals(pattern, StringComparison.OrdinalIgnoreCase)
                    || dirPath.StartsWith(pattern + "/", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            else if (dirs.Any(d => d.Equals(pattern, StringComparison.OrdinalIgnoreCase)))
                return true;
        }
        return false;
    }
}
