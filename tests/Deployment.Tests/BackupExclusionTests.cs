using Deployment.Application.Services;

namespace Deployment.Tests;

public class BackupExclusionTests
{
    [Theory]
    [InlineData("logs/app.log", "logs", true)]
    [InlineData("a/Logs/app.log", "logs", true)]
    [InlineData("logs.txt", "logs", false)]
    [InlineData("data/cache/x.bin", "data/cache", true)]
    [InlineData("other/data/cache/x.bin", "data/cache", false)]
    public void BackupExclusion_MatchesDirectories(string path, string pattern, bool expected)
        => Assert.Equal(expected, BackupExclusion.IsExcluded(path, BackupExclusion.Parse(pattern)));
}
