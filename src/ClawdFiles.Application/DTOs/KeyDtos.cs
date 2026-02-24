namespace ClawdFiles.Application.DTOs;

public record CreateKeyRequest(string Name);
public record CreateKeyResponse(string Key, string Prefix, string Name);
public record KeyInfoResponse(string Prefix, string Name, DateTimeOffset CreatedAt, DateTimeOffset? LastUsedAt, int BucketCount);
