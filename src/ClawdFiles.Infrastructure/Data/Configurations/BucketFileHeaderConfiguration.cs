using ClawdFiles.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClawdFiles.Infrastructure.Data.Configurations;

public class BucketFileHeaderConfiguration : IEntityTypeConfiguration<BucketFileHeader>
{
    public void Configure(EntityTypeBuilder<BucketFileHeader> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Path).IsRequired().HasMaxLength(500);
        builder.Property(f => f.ContentType).IsRequired().HasMaxLength(200);
        builder.Property(f => f.ShortCode).IsRequired().HasMaxLength(10);
        builder.HasIndex(f => f.ShortCode).IsUnique();
        builder.HasIndex(f => new { f.BucketId, f.Path }).IsUnique();
        builder.HasOne(f => f.Bucket).WithMany(b => b.Files).HasForeignKey(f => f.BucketId);
    }
}
