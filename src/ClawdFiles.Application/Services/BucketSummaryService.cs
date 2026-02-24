using System.Text;
using ClawdFiles.Application.Interfaces;

namespace ClawdFiles.Application.Services;

public class BucketSummaryService(IBucketRepository bucketRepo, IFileHeaderRepository fileRepo, IFileStorage storage)
{
    public async Task<string?> GetSummaryAsync(string bucketId, CancellationToken ct = default)
    {
        var bucket = await bucketRepo.FindByIdAsync(bucketId, ct);
        if (bucket is null) return null;

        var files = await fileRepo.ListByBucketAsync(bucketId, ct);
        var sb = new StringBuilder();
        sb.AppendLine($"# {bucket.Name}");
        if (!string.IsNullOrEmpty(bucket.Description)) sb.AppendLine(bucket.Description);
        sb.AppendLine();
        sb.AppendLine($"Bucket ID: {bucket.Id}");
        if (!string.IsNullOrEmpty(bucket.Purpose)) sb.AppendLine($"Purpose: {bucket.Purpose}");
        sb.AppendLine($"Created: {bucket.CreatedAt:u}");
        if (bucket.ExpiresAt.HasValue) sb.AppendLine($"Expires: {bucket.ExpiresAt:u}");
        sb.AppendLine($"Files: {files.Count}");
        sb.AppendLine();
        sb.AppendLine("## File Listing");
        foreach (var file in files)
            sb.AppendLine($"- {file.Path} ({file.ContentType}, {file.SizeBytes} bytes)");
        sb.AppendLine();

        var readme = files.FirstOrDefault(f => f.Path.Equals("README.md", StringComparison.OrdinalIgnoreCase) || f.Path.Equals("readme.md", StringComparison.OrdinalIgnoreCase));
        if (readme is not null)
        {
            var stream = await storage.GetFileStreamAsync(bucketId, readme.Path, ct);
            if (stream is not null)
            {
                using var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync(ct);
                sb.AppendLine("## README Content");
                sb.AppendLine(content);
            }
        }
        return sb.ToString();
    }
}
