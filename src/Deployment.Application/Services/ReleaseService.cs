using Deployment.Application.Interfaces;
using Deployment.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Deployment.Application.Services;

public class ReleaseService(
    IApplicationRepository appRepo,
    IReleaseRepository releaseRepo,
    IFileSystem fs,
    IChecksumService checksum,
    IAuditService audit,
    ILogger<ReleaseService> logger)
{
    public async Task<Release> CreateReleaseAsync(
        string applicationName,
        string version,
        string sourceDirectory,
        string releasesStorePath,
        string createdBy,
        string? commitId = null,
        string? buildId = null,
        string? notes = null,
        CancellationToken ct = default)
    {
        var app = await appRepo.GetByNameAsync(applicationName, ct)
            ?? throw new InvalidOperationException($"Application '{applicationName}' not found.");

        var existing = await releaseRepo.GetByVersionAsync(app.Id, version, ct);
        if (existing != null)
            throw new InvalidOperationException($"Release version '{version}' already exists for '{applicationName}'.");

        if (!fs.DirectoryExists(sourceDirectory))
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDirectory}");

        var releaseId = $"{applicationName}-{version}";
        var packagePath = fs.CombinePath(releasesStorePath, releaseId);
        fs.CreateDirectory(packagePath);

        logger.LogInformation("Creating release {ReleaseId} from {Source}", releaseId, sourceDirectory);

        var release = new Release
        {
            ApplicationId = app.Id,
            ReleaseId = releaseId,
            Version = version,
            CommitId = commitId,
            BuildId = buildId,
            PackagePath = packagePath,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            Notes = notes
        };

        var sourceFiles = fs.EnumerateFiles(sourceDirectory).ToList();
        var releaseFiles = new List<ReleaseFile>();

        foreach (var srcFile in sourceFiles)
        {
            var relativePath = fs.GetRelativePath(sourceDirectory, srcFile);
            var destFile = fs.CombinePath(packagePath, relativePath);

            var destDir = Path.GetDirectoryName(destFile)!;
            if (!fs.DirectoryExists(destDir))
                fs.CreateDirectory(destDir);

            fs.CopyFile(srcFile, destFile, overwrite: true);

            var sha256 = await checksum.ComputeSha256Async(destFile, ct);
            releaseFiles.Add(new ReleaseFile
            {
                RelativePath = relativePath,
                Size = fs.GetFileSize(destFile),
                Sha256 = sha256
            });
        }

        release.Files = releaseFiles;
        await releaseRepo.AddAsync(release, ct);

        await audit.RecordAsync(new AuditEvent
        {
            EventType = "RELEASE_CREATED",
            ReleaseId = releaseId,
            Application = applicationName,
            Operator = createdBy,
            Timestamp = DateTime.UtcNow,
            Result = "SUCCESS",
            Details = $"Version={version}, Files={releaseFiles.Count}"
        }, ct);

        logger.LogInformation("Release {ReleaseId} created with {Count} files", releaseId, releaseFiles.Count);
        return release;
    }

    public async Task<IReadOnlyList<Release>> ListReleasesAsync(string applicationName, CancellationToken ct = default)
    {
        var app = await appRepo.GetByNameAsync(applicationName, ct)
            ?? throw new InvalidOperationException($"Application '{applicationName}' not found.");
        return await releaseRepo.ListAsync(app.Id, ct);
    }

    public async Task<Release> GetReleaseAsync(string releaseId, CancellationToken ct = default)
    {
        return await releaseRepo.GetByReleaseIdAsync(releaseId, ct)
            ?? throw new InvalidOperationException($"Release '{releaseId}' not found.");
    }
}
