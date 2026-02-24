namespace ClawdFiles.Application.DTOs;

public record FileHeaderResponse(Guid Id, string Path, string ContentType, long SizeBytes, string ShortCode, string ShortUrl, DateTimeOffset UploadedAt);
public record UploadResponse(List<FileHeaderResponse> Uploaded);
public record DeleteFileRequest(string Path);
