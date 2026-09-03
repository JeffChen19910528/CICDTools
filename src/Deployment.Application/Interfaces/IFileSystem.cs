namespace Deployment.Application.Interfaces;

public interface IFileSystem
{
    bool DirectoryExists(string path);
    bool FileExists(string path);
    void CreateDirectory(string path);
    void DeleteDirectory(string path, bool recursive);
    void DeleteFile(string path);
    void CopyFile(string source, string destination, bool overwrite);
    void MoveDirectory(string source, string destination);

    /// <summary>Recursively copies every file from <paramref name="sourceDirectory"/> into
    /// <paramref name="destinationDirectory"/>, preserving relative paths and creating
    /// directories as needed. No-op if the source directory does not exist.</summary>
    void CopyDirectory(string sourceDirectory, string destinationDirectory, bool overwrite = true, CancellationToken ct = default);
    IEnumerable<string> EnumerateFiles(string path, bool recursive = true);
    long GetFileSize(string path);
    Stream OpenRead(string path);
    Stream OpenWrite(string path);
    string CombinePath(params string[] parts);
    string GetRelativePath(string basePath, string fullPath);
    bool IsBinaryFile(string path);
    string[] ReadAllLines(string path);
}
