using ClawdFiles.Application.DTOs;
using ClawdFiles.Application.Interfaces;
using ClawdFiles.Domain.Entities;

namespace ClawdFiles.Application.Services;

public class FileService(IFileHeaderRepository fileRepo, IFileStorage storage, IShortCodeGenerator shortCodes, IBucketRepository bucketRepo)
{
    public async Task<FileHeaderResponse?> UploadFileAsync(string bucketId, string path, string contentType, Stream content, CancellationToken ct = default)
    {
        var bucket = await bucketRepo.FindByIdAsync(bucketId, ct);
        if (bucket is null) return null;

        await storage.SaveFileAsync(bucketId, path, content, ct);

        var existing = await fileRepo.FindByBucketAndPathAsync(bucketId, path, ct);
        if (existing is not null)
        {
            existing.ContentType = contentType;
            existing.SizeBytes = content.Length;
            existing.UploadedAt = DateTimeOffset.UtcNow;
            await fileRepo.UpdateAsync(existing, ct);
            return new FileHeaderResponse(existing.Id, existing.Path, existing.ContentType, existing.SizeBytes, existing.ShortCode, $"/s/{existing.ShortCode}", existing.UploadedAt);
        }

        var shortCode = await shortCodes.GenerateAsync(ct);
        var header = new BucketFileHeader { Id = Guid.NewGuid(), BucketId = bucketId, Path = path, ContentType = contentType, SizeBytes = content.Length, ShortCode = shortCode };
        await fileRepo.CreateAsync(header, ct);
        return new FileHeaderResponse(header.Id, header.Path, header.ContentType, header.SizeBytes, header.ShortCode, $"/s/{header.ShortCode}", header.UploadedAt);
    }

    public async Task<BucketFileHeader?> ResolveShortCodeAsync(string shortCode, CancellationToken ct = default)
        => await fileRepo.FindByShortCodeAsync(shortCode, ct);

    public async Task<bool> DeleteFileAsync(string bucketId, string path, CancellationToken ct = default)
    {
        var header = await fileRepo.FindByBucketAndPathAsync(bucketId, path, ct);
        if (header is null) return false;
        await storage.DeleteFileAsync(bucketId, path, ct);
        await fileRepo.DeleteAsync(header, ct);
        return true;
    }
}
