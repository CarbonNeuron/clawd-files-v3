namespace ClawdFiles.Application.Interfaces;

public interface IFileStorage
{
    Task SaveFileAsync(string bucketId, string path, Stream content, CancellationToken ct = default);
    Task<Stream?> GetFileStreamAsync(string bucketId, string path, CancellationToken ct = default);
    Task<bool> DeleteFileAsync(string bucketId, string path, CancellationToken ct = default);
    Task DeleteBucketFilesAsync(string bucketId, CancellationToken ct = default);
    string GetFullPath(string bucketId, string path);
}
