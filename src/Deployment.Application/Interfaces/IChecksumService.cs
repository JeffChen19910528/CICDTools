namespace Deployment.Application.Interfaces;

public interface IChecksumService
{
    Task<string> ComputeSha256Async(string filePath, CancellationToken ct = default);
    Task<string> ComputeSha256Async(Stream stream, CancellationToken ct = default);
    Task<bool> VerifyAsync(string filePath, string expectedSha256, CancellationToken ct = default);
}
