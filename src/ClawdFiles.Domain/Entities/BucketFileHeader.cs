namespace ClawdFiles.Domain.Entities;

public class BucketFileHeader
{
    public Guid Id { get; set; }
    public required string BucketId { get; set; }
    public required string Path { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }
    public required string ShortCode { get; set; }
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;

    public Bucket? Bucket { get; set; }
}
