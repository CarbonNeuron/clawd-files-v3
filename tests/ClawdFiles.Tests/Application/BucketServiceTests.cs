using ClawdFiles.Application.DTOs;
using ClawdFiles.Application.Interfaces;
using ClawdFiles.Application.Services;
using ClawdFiles.Domain.Entities;
using Moq;

namespace ClawdFiles.Tests.Application;

public class BucketServiceTests
{
    private readonly Mock<IBucketRepository> _bucketRepo = new();
    private readonly Mock<IFileHeaderRepository> _fileRepo = new();
    private readonly Mock<IFileStorage> _storage = new();
    private readonly BucketService _sut;
    private readonly Guid _ownerId = Guid.NewGuid();

    public BucketServiceTests()
    {
        _bucketRepo.Setup(r => r.CreateAsync(It.IsAny<Bucket>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Bucket b, CancellationToken _) => b);
        _sut = new BucketService(_bucketRepo.Object, _fileRepo.Object, _storage.Object);
    }

    [Fact]
    public async Task CreateBucket_SetsExpiryFromPreset()
    {
        var request = new CreateBucketRequest("test", null, null, "1d");
        var result = await _sut.CreateBucketAsync(request, _ownerId);
        Assert.Equal("test", result.Name);
        Assert.NotNull(result.ExpiresAt);
    }

    [Fact]
    public async Task CreateBucket_NeverExpiry_SetsNullExpiresAt()
    {
        var request = new CreateBucketRequest("test", null, null, "never");
        var result = await _sut.CreateBucketAsync(request, _ownerId);
        Assert.Null(result.ExpiresAt);
    }

    [Fact]
    public async Task GetBucket_ReturnsNullWhenNotFound()
    {
        _bucketRepo.Setup(r => r.FindByIdAsync("nope", It.IsAny<CancellationToken>())).ReturnsAsync((Bucket?)null);
        Assert.Null(await _sut.GetBucketAsync("nope"));
    }

    [Fact]
    public async Task DeleteBucket_WrongOwner_NotAdmin_ReturnsFalse()
    {
        var bucket = new Bucket { Id = "abc", Name = "test", OwnerId = Guid.NewGuid() };
        _bucketRepo.Setup(r => r.FindByIdAsync("abc", It.IsAny<CancellationToken>())).ReturnsAsync(bucket);
        Assert.False(await _sut.DeleteBucketAsync("abc", _ownerId, isAdmin: false));
    }

    [Fact]
    public async Task DeleteBucket_AsAdmin_ReturnsTrue()
    {
        var bucket = new Bucket { Id = "abc", Name = "test", OwnerId = Guid.NewGuid(), Files = [] };
        bucket.Owner = new ApiKey { Id = bucket.OwnerId, Name = "k", KeyHash = "h", Prefix = "12345678" };
        _bucketRepo.Setup(r => r.FindByIdAsync("abc", It.IsAny<CancellationToken>())).ReturnsAsync(bucket);
        Assert.True(await _sut.DeleteBucketAsync("abc", _ownerId, isAdmin: true));
    }
}
