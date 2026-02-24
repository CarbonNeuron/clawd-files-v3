using ClawdFiles.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClawdFiles.Infrastructure.Data;

public class ClawdFilesDbContext(DbContextOptions<ClawdFilesDbContext> options) : DbContext(options)
{
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<Bucket> Buckets => Set<Bucket>();
    public DbSet<BucketFileHeader> FileHeaders => Set<BucketFileHeader>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClawdFilesDbContext).Assembly);
    }
}
