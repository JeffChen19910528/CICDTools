using System.Security.Cryptography;
using Deployment.Application.Interfaces;

namespace Deployment.Infrastructure.Services;

public class Sha256ChecksumService : IChecksumService
{
    public async Task<string> ComputeSha256Async(string filePath, CancellationToken ct = default)
    {
        await using var stream = File.OpenRead(filePath);
        return await ComputeSha256Async(stream, ct);
    }

    public async Task<string> ComputeSha256Async(Stream stream, CancellationToken ct = default)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream, ct);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public async Task<bool> VerifyAsync(string filePath, string expectedSha256, CancellationToken ct = default)
    {
        var actual = await ComputeSha256Async(filePath, ct);
        return string.Equals(actual, expectedSha256, StringComparison.OrdinalIgnoreCase);
    }
}
