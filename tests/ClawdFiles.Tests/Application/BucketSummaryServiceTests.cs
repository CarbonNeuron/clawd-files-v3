using ClawdFiles.Application.Interfaces;
using ClawdFiles.Application.Services;
using ClawdFiles.Domain.Entities;
using Moq;

namespace ClawdFiles.Tests.Application;

public class BucketSummaryServiceTests
{
    private readonly Mock<IBucketRepository> _bucketRepo = new();
    private readonly Mock<IFileHeaderRepository> _fileRepo = new();
    private readonly Mock<IFileStorage> _storage = new();
    private readonly BucketSummaryService _sut;

    public BucketSummaryServiceTests()
    {
        _sut = new BucketSummaryService(_bucketRepo.Object, _fileRepo.Object, _storage.Object);
    }

    [Fact]
    public async Task GetSummary_ReturnsPlainTextWithFileList()
    {
        var bucket = new Bucket { Id = "abc", Name = "Test Bucket", Description = "A test", OwnerId = Guid.NewGuid() };
        bucket.Owner = new ApiKey { Id = bucket.OwnerId, Name = "k", KeyHash = "h", Prefix = "12345678" };
        _bucketRepo.Setup(r => r.FindByIdAsync("abc", It.IsAny<CancellationToken>())).ReturnsAsync(bucket);
        _fileRepo.Setup(r => r.ListByBucketAsync("abc", It.IsAny<CancellationToken>())).ReturnsAsync([
            new BucketFileHeader { Id = Guid.NewGuid(), BucketId = "abc", Path = "readme.md", ContentType = "text/markdown", SizeBytes = 100, ShortCode = "abc123" },
            new BucketFileHeader { Id = Guid.NewGuid(), BucketId = "abc", Path = "data.json", ContentType = "application/json", SizeBytes = 500, ShortCode = "def456" },
        ]);
        _storage.Setup(s => s.GetFileStreamAsync("abc", "readme.md", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream("# Hello World"u8.ToArray()));

        var result = await _sut.GetSummaryAsync("abc");
        Assert.NotNull(result);
        Assert.Contains("Test Bucket", result);
        Assert.Contains("readme.md", result);
        Assert.Contains("data.json", result);
        Assert.Contains("# Hello World", result);
    }

    [Fact]
    public async Task GetSummary_BucketNotFound_ReturnsNull()
    {
        _bucketRepo.Setup(r => r.FindByIdAsync("nope", It.IsAny<CancellationToken>())).ReturnsAsync((Bucket?)null);
        Assert.Null(await _sut.GetSummaryAsync("nope"));
    }
}
