using ClawdFiles.Application.Interfaces;
using ClawdFiles.Domain.Entities;
using ClawdFiles.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClawdFiles.Infrastructure.Repositories;

public class EfBucketRepository(ClawdFilesDbContext db) : IBucketRepository
{
    public async Task<Bucket> CreateAsync(Bucket bucket, CancellationToken ct = default)
    { db.Buckets.Add(bucket); await db.SaveChangesAsync(ct); return bucket; }

    public async Task<Bucket?> FindByIdAsync(string id, CancellationToken ct = default)
        => await db.Buckets.Include(b => b.Owner).Include(b => b.Files).FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<List<Bucket>> ListByOwnerAsync(Guid ownerId, CancellationToken ct = default)
    {
        var buckets = await db.Buckets.Include(b => b.Files).Where(b => b.OwnerId == ownerId).ToListAsync(ct);
        return buckets.OrderByDescending(b => b.CreatedAt).ToList();
    }

    public async Task<List<Bucket>> ListAllAsync(CancellationToken ct = default)
    {
        var buckets = await db.Buckets.Include(b => b.Files).ToListAsync(ct);
        return buckets.OrderByDescending(b => b.CreatedAt).ToList();
    }

    public async Task<List<Bucket>> FindExpiredAsync(DateTimeOffset now, CancellationToken ct = default)
    {
        var buckets = await db.Buckets.Include(b => b.Files).ToListAsync(ct);
        return buckets.Where(b => b.ExpiresAt != null && b.ExpiresAt <= now).ToList();
    }

    public async Task UpdateAsync(Bucket bucket, CancellationToken ct = default)
    { db.Buckets.Update(bucket); await db.SaveChangesAsync(ct); }

    public async Task DeleteAsync(Bucket bucket, CancellationToken ct = default)
    { db.Buckets.Remove(bucket); await db.SaveChangesAsync(ct); }
}
