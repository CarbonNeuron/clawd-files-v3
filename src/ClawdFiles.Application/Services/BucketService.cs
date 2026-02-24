using System.Security.Cryptography;
using ClawdFiles.Application.DTOs;
using ClawdFiles.Application.Interfaces;
using ClawdFiles.Domain.Entities;
using ClawdFiles.Domain.ValueObjects;

namespace ClawdFiles.Application.Services;

public class BucketService(IBucketRepository bucketRepo, IFileHeaderRepository fileRepo, IFileStorage storage)
{
    private static readonly char[] IdChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

    public async Task<BucketListItemResponse> CreateBucketAsync(CreateBucketRequest request, Guid ownerId, CancellationToken ct = default)
    {
        var expiry = ExpiryPreset.Parse(request.ExpiresIn);
        var bucket = new Bucket
        {
            Id = GenerateId(5), Name = request.Name, Description = request.Description,
            Purpose = request.Purpose, OwnerId = ownerId,
            ExpiresAt = expiry.HasValue ? DateTimeOffset.UtcNow + expiry.Value : null,
        };
        await bucketRepo.CreateAsync(bucket, ct);
        return new BucketListItemResponse(bucket.Id, bucket.Name, bucket.Description, bucket.Purpose, bucket.CreatedAt, bucket.ExpiresAt, 0);
    }

    public async Task<BucketResponse?> GetBucketAsync(string id, CancellationToken ct = default)
    {
        var bucket = await bucketRepo.FindByIdAsync(id, ct);
        if (bucket is null) return null;
        var files = await fileRepo.ListByBucketAsync(id, ct);
        var ownerPrefix = bucket.Owner?.Prefix ?? "unknown";
        return new BucketResponse(bucket.Id, bucket.Name, bucket.Description, bucket.Purpose,
            ownerPrefix, bucket.CreatedAt, bucket.ExpiresAt,
            files.Select(f => new FileHeaderResponse(f.Id, f.Path, f.ContentType, f.SizeBytes, f.ShortCode, $"/s/{f.ShortCode}", f.UploadedAt)).ToList());
    }

    public async Task<List<BucketListItemResponse>> ListBucketsAsync(Guid? ownerId, bool isAdmin, CancellationToken ct = default)
    {
        var buckets = isAdmin ? await bucketRepo.ListAllAsync(ct) : await bucketRepo.ListByOwnerAsync(ownerId!.Value, ct);
        return buckets.Select(b => new BucketListItemResponse(b.Id, b.Name, b.Description, b.Purpose, b.CreatedAt, b.ExpiresAt, b.Files.Count)).ToList();
    }

    public async Task<BucketListItemResponse?> UpdateBucketAsync(string id, UpdateBucketRequest request, Guid callerId, bool isAdmin, CancellationToken ct = default)
    {
        var bucket = await bucketRepo.FindByIdAsync(id, ct);
        if (bucket is null || (bucket.OwnerId != callerId && !isAdmin)) return null;
        if (request.Name is not null) bucket.Name = request.Name;
        if (request.Description is not null) bucket.Description = request.Description;
        if (request.Purpose is not null) bucket.Purpose = request.Purpose;
        await bucketRepo.UpdateAsync(bucket, ct);
        return new BucketListItemResponse(bucket.Id, bucket.Name, bucket.Description, bucket.Purpose, bucket.CreatedAt, bucket.ExpiresAt, bucket.Files.Count);
    }

    public async Task<bool> DeleteBucketAsync(string id, Guid callerId, bool isAdmin, CancellationToken ct = default)
    {
        var bucket = await bucketRepo.FindByIdAsync(id, ct);
        if (bucket is null || (bucket.OwnerId != callerId && !isAdmin)) return false;
        await fileRepo.DeleteByBucketAsync(id, ct);
        await storage.DeleteBucketFilesAsync(id, ct);
        await bucketRepo.DeleteAsync(bucket, ct);
        return true;
    }

    private static string GenerateId(int length)
    {
        return string.Create(length, (object?)null, (span, _) =>
        {
            Span<byte> random = stackalloc byte[length];
            RandomNumberGenerator.Fill(random);
            for (var i = 0; i < length; i++)
                span[i] = IdChars[random[i] % IdChars.Length];
        });
    }
}
