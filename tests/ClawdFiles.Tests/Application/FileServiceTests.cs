using ClawdFiles.Application.Interfaces;
using ClawdFiles.Application.Services;
using ClawdFiles.Domain.Entities;
using Moq;

namespace ClawdFiles.Tests.Application;

public class FileServiceTests
{
    private readonly Mock<IFileHeaderRepository> _fileRepo = new();
    private readonly Mock<IFileStorage> _storage = new();
    private readonly Mock<IShortCodeGenerator> _shortCodes = new();
    private readonly Mock<IBucketRepository> _bucketRepo = new();
    private readonly FileService _sut;

    public FileServiceTests()
    {
        _shortCodes.Setup(s => s.GenerateAsync(It.IsAny<CancellationToken>())).ReturnsAsync("xK9mQ2");
        _fileRepo.Setup(r => r.CreateAsync(It.IsAny<BucketFileHeader>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BucketFileHeader h, CancellationToken _) => h);
        _sut = new FileService(_fileRepo.Object, _storage.Object, _shortCodes.Object, _bucketRepo.Object);
    }

    [Fact]
    public async Task UploadFile_CreatesHeaderAndStoresFile()
    {
        _bucketRepo.Setup(r => r.FindByIdAsync("abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Bucket { Id = "abc", Name = "test", OwnerId = Guid.NewGuid() });
        using var stream = new MemoryStream([1, 2, 3]);
        var result = await _sut.UploadFileAsync("abc", "test.txt", "text/plain", stream);
        Assert.NotNull(result);
        Assert.Equal("test.txt", result.Path);
        Assert.Equal("xK9mQ2", result.ShortCode);
        _storage.Verify(s => s.SaveFileAsync("abc", "test.txt", It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadFile_BucketNotFound_ReturnsNull()
    {
        _bucketRepo.Setup(r => r.FindByIdAsync("nope", It.IsAny<CancellationToken>())).ReturnsAsync((Bucket?)null);
        using var stream = new MemoryStream([1, 2, 3]);
        Assert.Null(await _sut.UploadFileAsync("nope", "test.txt", "text/plain", stream));
    }

    [Fact]
    public async Task ResolveShortCode_ReturnsFileHeader()
    {
        var header = new BucketFileHeader { Id = Guid.NewGuid(), BucketId = "abc", Path = "test.txt", ContentType = "text/plain", ShortCode = "xK9mQ2" };
        _fileRepo.Setup(r => r.FindByShortCodeAsync("xK9mQ2", It.IsAny<CancellationToken>())).ReturnsAsync(header);
        var result = await _sut.ResolveShortCodeAsync("xK9mQ2");
        Assert.NotNull(result);
        Assert.Equal("abc", result.BucketId);
    }

    [Fact]
    public async Task DeleteFile_ExistingFile_ReturnsTrue()
    {
        var header = new BucketFileHeader { Id = Guid.NewGuid(), BucketId = "abc", Path = "test.txt", ContentType = "text/plain", ShortCode = "xK9mQ2" };
        _fileRepo.Setup(r => r.FindByBucketAndPathAsync("abc", "test.txt", It.IsAny<CancellationToken>())).ReturnsAsync(header);
        _storage.Setup(s => s.DeleteFileAsync("abc", "test.txt", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        Assert.True(await _sut.DeleteFileAsync("abc", "test.txt"));
    }
}
