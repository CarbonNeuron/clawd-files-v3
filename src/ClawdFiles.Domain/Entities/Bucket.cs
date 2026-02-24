namespace ClawdFiles.Domain.Entities;

public class Bucket
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Purpose { get; set; }
    public Guid OwnerId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }

    public ApiKey? Owner { get; set; }
    public ICollection<BucketFileHeader> Files { get; set; } = [];
}
