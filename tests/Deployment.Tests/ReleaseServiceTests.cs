using Deployment.Application.Services;
using Deployment.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Deployment.Tests;

public class ReleaseServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    [Fact]
    public async Task CreateRelease_CreatesReleaseWithCorrectFileCount()
    {
        var db = TestHelper.CreateDb();
        var svc = BuildService(db);

        var sourceDir = Path.Combine(_tempDir, "source");
        TestHelper.CreateFiles(sourceDir, new Dictionary<string, string>
        {
            ["app.exe"] = "binary",
            ["config.json"] = """{"key":"value"}""",
            ["sub/data.txt"] = "hello world"
        });

        var releasesDir = Path.Combine(_tempDir, "releases");
        await TestHelper.CreateAppAsync(svc.AppRepo);

        var release = await svc.Service.CreateReleaseAsync("TestApp", "1.0.0", sourceDir, releasesDir, "testuser");

        Assert.Equal("TestApp-1.0.0", release.ReleaseId);
        Assert.Equal(3, release.Files.Count);
        Assert.All(release.Files, f => Assert.NotEmpty(f.Sha256));
    }

    [Fact]
    public async Task CreateRelease_ThrowsIfVersionAlreadyExists()
    {
        var db = TestHelper.CreateDb();
        var svc = BuildService(db);

        var sourceDir = Path.Combine(_tempDir, "source");
        TestHelper.CreateFiles(sourceDir, new Dictionary<string, string> { ["app.exe"] = "v1" });
        var releasesDir = Path.Combine(_tempDir, "releases");
        await TestHelper.CreateAppAsync(svc.AppRepo);

        await svc.Service.CreateReleaseAsync("TestApp", "1.0.0", sourceDir, releasesDir, "testuser");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.Service.CreateReleaseAsync("TestApp", "1.0.0", sourceDir, releasesDir, "testuser"));
    }

    [Fact]
    public async Task ListReleases_ReturnsAllReleasesForApp()
    {
        var db = TestHelper.CreateDb();
        var svc = BuildService(db);

        var sourceDir = Path.Combine(_tempDir, "source");
        TestHelper.CreateFiles(sourceDir, new Dictionary<string, string> { ["app.exe"] = "v1" });
        var releasesDir = Path.Combine(_tempDir, "releases");
        await TestHelper.CreateAppAsync(svc.AppRepo);

        await svc.Service.CreateReleaseAsync("TestApp", "1.0.0", sourceDir, releasesDir, "testuser");
        await svc.Service.CreateReleaseAsync("TestApp", "1.1.0", sourceDir, releasesDir, "testuser");

        var releases = await svc.Service.ListReleasesAsync("TestApp");
        Assert.Equal(2, releases.Count);
    }

    private (ReleaseService Service, Deployment.Application.Interfaces.IApplicationRepository AppRepo)
        BuildService(Deployment.Infrastructure.Data.DeploymentDbContext db)
    {
        var svcs = TestHelper.CreateServices(db, _tempDir);
        var service = new ReleaseService(
            svcs.Resolver, svcs.ReleaseRepo, svcs.Fs, svcs.Checksum,
            svcs.Audit, NullLogger<ReleaseService>.Instance);
        return (service, svcs.AppRepo);
    }
}
