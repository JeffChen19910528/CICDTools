using Deployment.Application.Interfaces;

namespace Deployment.Infrastructure.Services;

public class LocalFileSystem : IFileSystem
{
    public bool DirectoryExists(string path) => Directory.Exists(path);
    public bool FileExists(string path) => File.Exists(path);
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public void DeleteDirectory(string path, bool recursive) => Directory.Delete(path, recursive);
    public void DeleteFile(string path) => File.Delete(path);

    public void CopyFile(string source, string destination, bool overwrite) => File.Copy(source, destination, overwrite);

    public void MoveDirectory(string source, string destination) => Directory.Move(source, destination);

    public void CopyDirectory(string sourceDirectory, string destinationDirectory, bool overwrite = true, CancellationToken ct = default)
    {
        if (!Directory.Exists(sourceDirectory)) return;
        if (!Directory.Exists(destinationDirectory)) Directory.CreateDirectory(destinationDirectory);

        foreach (var srcFile in EnumerateFiles(sourceDirectory))
        {
            ct.ThrowIfCancellationRequested();
            var relativePath = GetRelativePath(sourceDirectory, srcFile);
            var destFile = CombinePath(destinationDirectory, relativePath);
            var destDir = Path.GetDirectoryName(destFile)!;
            if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);
            CopyFile(srcFile, destFile, overwrite);
        }
    }

    public IEnumerable<string> EnumerateFiles(string path, bool recursive = true)
        => Directory.EnumerateFiles(path, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

    public long GetFileSize(string path) => new FileInfo(path).Length;

    public Stream OpenRead(string path) => File.OpenRead(path);
    public Stream OpenWrite(string path) => File.OpenWrite(path);

    public string CombinePath(params string[] parts) => Path.Combine(parts);

    public string GetRelativePath(string basePath, string fullPath)
        => Path.GetRelativePath(basePath, fullPath);

    public bool IsBinaryFile(string path)
    {
        if (!File.Exists(path)) return false;
        try
        {
            Span<byte> buffer = stackalloc byte[8192];
            using var stream = File.OpenRead(path);
            int read = stream.Read(buffer);
            for (int i = 0; i < read; i++)
                if (buffer[i] == 0) return true;
            return false;
        }
        catch
        {
            return true;
        }
    }

    public string[] ReadAllLines(string path) => File.ReadAllLines(path);
}
