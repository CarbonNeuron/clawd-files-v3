using ClawdFiles.Application.Interfaces;
using ClawdFiles.Domain.Entities;
using ClawdFiles.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClawdFiles.Infrastructure.Repositories;

public class EfApiKeyRepository(ClawdFilesDbContext db) : IApiKeyRepository
{
    public async Task<ApiKey> CreateAsync(ApiKey key, CancellationToken ct = default)
    { db.ApiKeys.Add(key); await db.SaveChangesAsync(ct); return key; }

    public async Task<ApiKey?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => await db.ApiKeys.Include(k => k.Buckets).FirstOrDefaultAsync(k => k.Id == id, ct);

    public async Task<ApiKey?> FindByPrefixAsync(string prefix, CancellationToken ct = default)
        => await db.ApiKeys.Include(k => k.Buckets).FirstOrDefaultAsync(k => k.Prefix == prefix, ct);

    public async Task<ApiKey?> FindByHashAsync(string keyHash, CancellationToken ct = default)
        => await db.ApiKeys.Include(k => k.Buckets).FirstOrDefaultAsync(k => k.KeyHash == keyHash, ct);

    public async Task<List<ApiKey>> ListAllAsync(CancellationToken ct = default)
        => await db.ApiKeys.Include(k => k.Buckets).OrderByDescending(k => k.CreatedAt).ToListAsync(ct);

    public async Task DeleteAsync(ApiKey key, CancellationToken ct = default)
    { db.ApiKeys.Remove(key); await db.SaveChangesAsync(ct); }

    public async Task UpdateAsync(ApiKey key, CancellationToken ct = default)
    { db.ApiKeys.Update(key); await db.SaveChangesAsync(ct); }
}
