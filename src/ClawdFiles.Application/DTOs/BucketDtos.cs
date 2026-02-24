namespace ClawdFiles.Application.DTOs;

public record CreateBucketRequest(string Name, string? Description, string? Purpose, string? ExpiresIn);
public record UpdateBucketRequest(string? Name, string? Description, string? Purpose);
public record BucketResponse(string Id, string Name, string? Description, string? Purpose, string OwnerPrefix, DateTimeOffset CreatedAt, DateTimeOffset? ExpiresAt, List<FileHeaderResponse> Files);
public record BucketListItemResponse(string Id, string Name, string? Description, string? Purpose, DateTimeOffset CreatedAt, DateTimeOffset? ExpiresAt, int FileCount);
