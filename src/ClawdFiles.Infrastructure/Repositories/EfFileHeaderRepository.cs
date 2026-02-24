using ClawdFiles.Application.Interfaces;
using ClawdFiles.Domain.Entities;
using ClawdFiles.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClawdFiles.Infrastructure.Repositories;

public class EfFileHeaderRepository(ClawdFilesDbContext db) : IFileHeaderRepository
{
    public async Task<BucketFileHeader> CreateAsync(BucketFileHeader header, CancellationToken ct = default)
    { db.FileHeaders.Add(header); await db.SaveChangesAsync(ct); return header; }

    public async Task<BucketFileHeader?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => await db.FileHeaders.FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<BucketFileHeader?> FindByBucketAndPathAsync(string bucketId, string path, CancellationToken ct = default)
        => await db.FileHeaders.FirstOrDefaultAsync(f => f.BucketId == bucketId && f.Path == path, ct);

    public async Task<BucketFileHeader?> FindByShortCodeAsync(string shortCode, CancellationToken ct = default)
        => await db.FileHeaders.FirstOrDefaultAsync(f => f.ShortCode == shortCode, ct);

    public async Task<List<BucketFileHeader>> ListByBucketAsync(string bucketId, CancellationToken ct = default)
        => await db.FileHeaders.Where(f => f.BucketId == bucketId).OrderBy(f => f.Path).ToListAsync(ct);

    public async Task DeleteAsync(BucketFileHeader header, CancellationToken ct = default)
    { db.FileHeaders.Remove(header); await db.SaveChangesAsync(ct); }

    public async Task DeleteByBucketAsync(string bucketId, CancellationToken ct = default)
    {
        var headers = await db.FileHeaders.Where(f => f.BucketId == bucketId).ToListAsync(ct);
        db.FileHeaders.RemoveRange(headers);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(BucketFileHeader header, CancellationToken ct = default)
    { db.FileHeaders.Update(header); await db.SaveChangesAsync(ct); }
}
