using ClawdFiles.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClawdFiles.Infrastructure.Data.Configurations;

public class BucketConfiguration : IEntityTypeConfiguration<Bucket>
{
    public void Configure(EntityTypeBuilder<Bucket> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasMaxLength(10);
        builder.Property(b => b.Name).IsRequired().HasMaxLength(200);
        builder.Property(b => b.Description).HasMaxLength(1000);
        builder.Property(b => b.Purpose).HasMaxLength(200);
        builder.HasOne(b => b.Owner).WithMany(k => k.Buckets).HasForeignKey(b => b.OwnerId);
        builder.HasIndex(b => b.ExpiresAt);
    }
}
