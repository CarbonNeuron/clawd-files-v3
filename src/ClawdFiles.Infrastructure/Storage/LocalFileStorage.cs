using ClawdFiles.Application.Interfaces;

namespace ClawdFiles.Infrastructure.Storage;

public class LocalFileStorage(string rootPath) : IFileStorage
{
    public async Task SaveFileAsync(string bucketId, string path, Stream content, CancellationToken ct = default)
    {
        var fullPath = GetFullPath(bucketId, path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        await content.CopyToAsync(fs, ct);
    }

    public Task<Stream?> GetFileStreamAsync(string bucketId, string path, CancellationToken ct = default)
    {
        var fullPath = GetFullPath(bucketId, path);
        if (!File.Exists(fullPath)) return Task.FromResult<Stream?>(null);
        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream?>(stream);
    }

    public Task<bool> DeleteFileAsync(string bucketId, string path, CancellationToken ct = default)
    {
        var fullPath = GetFullPath(bucketId, path);
        if (!File.Exists(fullPath)) return Task.FromResult(false);
        File.Delete(fullPath);
        return Task.FromResult(true);
    }

    public Task DeleteBucketFilesAsync(string bucketId, CancellationToken ct = default)
    {
        var dir = Path.Combine(rootPath, bucketId);
        if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        return Task.CompletedTask;
    }

    public string GetFullPath(string bucketId, string path) => Path.Combine(rootPath, bucketId, path);
}
