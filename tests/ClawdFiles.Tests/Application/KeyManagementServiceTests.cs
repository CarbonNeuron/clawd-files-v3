using ClawdFiles.Application.DTOs;
using ClawdFiles.Application.Interfaces;
using ClawdFiles.Application.Services;
using ClawdFiles.Domain.Entities;
using Moq;

namespace ClawdFiles.Tests.Application;

public class KeyManagementServiceTests
{
    private readonly Mock<IApiKeyRepository> _keyRepo = new();
    private readonly Mock<IApiKeyHasher> _hasher = new();
    private readonly KeyManagementService _sut;

    public KeyManagementServiceTests()
    {
        _hasher.Setup(h => h.GenerateKey()).Returns("test-key-1234567890abcdef");
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed-key");
        _keyRepo.Setup(r => r.CreateAsync(It.IsAny<ApiKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApiKey k, CancellationToken _) => k);
        _sut = new KeyManagementService(_keyRepo.Object, _hasher.Object);
    }

    [Fact]
    public async Task CreateKey_ReturnsKeyWithPrefixAndName()
    {
        var result = await _sut.CreateKeyAsync(new CreateKeyRequest("test-key"));
        Assert.Equal("test-key", result.Name);
        Assert.Equal(8, result.Prefix.Length);
        Assert.Equal("test-key-1234567890abcdef", result.Key);
    }

    [Fact]
    public async Task ListKeys_ReturnsAllKeysWithBucketCount()
    {
        var key = new ApiKey { Id = Guid.NewGuid(), Name = "my-key", KeyHash = "hash", Prefix = "test-key",
            Buckets = [new Bucket { Id = "abc", Name = "test", OwnerId = Guid.NewGuid() }] };
        _keyRepo.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([key]);
        var result = await _sut.ListKeysAsync();
        Assert.Single(result);
        Assert.Equal("my-key", result[0].Name);
        Assert.Equal(1, result[0].BucketCount);
    }

    [Fact]
    public async Task RevokeKey_ExistingPrefix_ReturnsTrue()
    {
        var key = new ApiKey { Id = Guid.NewGuid(), Name = "k", KeyHash = "h", Prefix = "test1234" };
        _keyRepo.Setup(r => r.FindByPrefixAsync("test1234", It.IsAny<CancellationToken>())).ReturnsAsync(key);
        Assert.True(await _sut.RevokeKeyAsync("test1234"));
    }

    [Fact]
    public async Task RevokeKey_NonExistentPrefix_ReturnsFalse()
    {
        _keyRepo.Setup(r => r.FindByPrefixAsync("noexist1", It.IsAny<CancellationToken>())).ReturnsAsync((ApiKey?)null);
        Assert.False(await _sut.RevokeKeyAsync("noexist1"));
    }
}
