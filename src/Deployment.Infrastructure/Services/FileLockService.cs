using System.Text.Json;
using Deployment.Application.Interfaces;
using Deployment.Domain.Models;

namespace Deployment.Infrastructure.Services;

public class FileLockService(string lockDirectory) : ILockService
{
    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    private string LockFilePath(string app, string env, string target)
        => Path.Combine(lockDirectory, $"{Sanitize(app)}__{Sanitize(env)}__{Sanitize(target)}.lock");

    private static string Sanitize(string s) => string.Concat(s.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'));

    public async Task<bool> TryAcquireAsync(DeploymentLock lockInfo, CancellationToken ct = default)
    {
        await Semaphore.WaitAsync(ct);
        try
        {
            Directory.CreateDirectory(lockDirectory);
            var path = LockFilePath(lockInfo.Application, lockInfo.Environment, lockInfo.Target);
            if (File.Exists(path)) return false;

            var json = JsonSerializer.Serialize(lockInfo);
            await File.WriteAllTextAsync(path, json, ct);
            return true;
        }
        finally
        {
            Semaphore.Release();
        }
    }

    public async Task ReleaseAsync(string application, string environment, string target, CancellationToken ct = default)
    {
        await Semaphore.WaitAsync(ct);
        try
        {
            var path = LockFilePath(application, environment, target);
            if (File.Exists(path)) File.Delete(path);
        }
        finally
        {
            Semaphore.Release();
        }
    }

    public async Task<DeploymentLock?> GetCurrentLockAsync(string application, string environment, string target, CancellationToken ct = default)
    {
        var path = LockFilePath(application, environment, target);
        if (!File.Exists(path)) return null;
        var json = await File.ReadAllTextAsync(path, ct);
        return JsonSerializer.Deserialize<DeploymentLock>(json);
    }
}
