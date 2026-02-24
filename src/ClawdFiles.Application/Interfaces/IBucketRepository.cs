using ClawdFiles.Domain.Entities;
namespace ClawdFiles.Application.Interfaces;

public interface IBucketRepository
{
    Task<Bucket> CreateAsync(Bucket bucket, CancellationToken ct = default);
    Task<Bucket?> FindByIdAsync(string id, CancellationToken ct = default);
    Task<List<Bucket>> ListByOwnerAsync(Guid ownerId, CancellationToken ct = default);
    Task<List<Bucket>> ListAllAsync(CancellationToken ct = default);
    Task<List<Bucket>> FindExpiredAsync(DateTimeOffset now, CancellationToken ct = default);
    Task UpdateAsync(Bucket bucket, CancellationToken ct = default);
    Task DeleteAsync(Bucket bucket, CancellationToken ct = default);
}
