using ClawdFiles.Domain.Entities;
namespace ClawdFiles.Application.Interfaces;

public interface IFileHeaderRepository
{
    Task<BucketFileHeader> CreateAsync(BucketFileHeader header, CancellationToken ct = default);
    Task<BucketFileHeader?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<BucketFileHeader?> FindByBucketAndPathAsync(string bucketId, string path, CancellationToken ct = default);
    Task<BucketFileHeader?> FindByShortCodeAsync(string shortCode, CancellationToken ct = default);
    Task<List<BucketFileHeader>> ListByBucketAsync(string bucketId, CancellationToken ct = default);
    Task DeleteAsync(BucketFileHeader header, CancellationToken ct = default);
    Task DeleteByBucketAsync(string bucketId, CancellationToken ct = default);
    Task UpdateAsync(BucketFileHeader header, CancellationToken ct = default);
}
