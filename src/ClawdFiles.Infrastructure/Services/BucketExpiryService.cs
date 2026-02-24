using ClawdFiles.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClawdFiles.Infrastructure.Services;

public class ExpiryOptions
{
    public int CleanupIntervalMinutes { get; set; } = 5;
}

public class BucketExpiryService(
    IServiceScopeFactory scopeFactory,
    IOptions<ExpiryOptions> options,
    ILogger<BucketExpiryService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(options.Value.CleanupIntervalMinutes);
        logger.LogInformation("Bucket expiry service started. Cleanup interval: {Interval}", interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await CleanupExpiredBucketsAsync(stoppingToken); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            { logger.LogError(ex, "Error during bucket cleanup"); }
            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task CleanupExpiredBucketsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var bucketRepo = scope.ServiceProvider.GetRequiredService<IBucketRepository>();
        var fileRepo = scope.ServiceProvider.GetRequiredService<IFileHeaderRepository>();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
        var expired = await bucketRepo.FindExpiredAsync(DateTimeOffset.UtcNow, ct);
        if (expired.Count == 0) return;
        logger.LogInformation("Found {Count} expired buckets to clean up", expired.Count);
        foreach (var bucket in expired)
        {
            logger.LogInformation("Deleting expired bucket {BucketId} ({Name})", bucket.Id, bucket.Name);
            await fileRepo.DeleteByBucketAsync(bucket.Id, ct);
            await storage.DeleteBucketFilesAsync(bucket.Id, ct);
            await bucketRepo.DeleteAsync(bucket, ct);
        }
    }
}
