namespace ClawdFiles.Domain.Entities;

public class ApiKey
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string KeyHash { get; set; }
    public required string Prefix { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastUsedAt { get; set; }

    public ICollection<Bucket> Buckets { get; set; } = [];
}
