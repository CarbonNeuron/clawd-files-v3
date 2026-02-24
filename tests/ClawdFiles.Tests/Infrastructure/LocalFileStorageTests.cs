using ClawdFiles.Infrastructure.Storage;

namespace ClawdFiles.Tests.Infrastructure;

public class LocalFileStorageTests : IDisposable
{
    private readonly string _root;
    private readonly LocalFileStorage _sut;

    public LocalFileStorageTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"clawdfiles-test-{Guid.NewGuid():N}");
        _sut = new LocalFileStorage(_root);
    }

    [Fact]
    public async Task SaveAndGet_RoundTrips()
    {
        using var input = new MemoryStream("hello world"u8.ToArray());
        await _sut.SaveFileAsync("bucket1", "test.txt", input);
        using var output = await _sut.GetFileStreamAsync("bucket1", "test.txt");
        Assert.NotNull(output);
        using var reader = new StreamReader(output);
        Assert.Equal("hello world", await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task Delete_RemovesFile()
    {
        using var input = new MemoryStream("data"u8.ToArray());
        await _sut.SaveFileAsync("bucket1", "test.txt", input);
        Assert.True(await _sut.DeleteFileAsync("bucket1", "test.txt"));
        Assert.Null(await _sut.GetFileStreamAsync("bucket1", "test.txt"));
    }

    [Fact]
    public async Task DeleteBucketFiles_RemovesDirectory()
    {
        using var input = new MemoryStream("data"u8.ToArray());
        await _sut.SaveFileAsync("bucket2", "a.txt", input);
        input.Position = 0;
        await _sut.SaveFileAsync("bucket2", "b.txt", input);
        await _sut.DeleteBucketFilesAsync("bucket2");
        Assert.False(Directory.Exists(Path.Combine(_root, "bucket2")));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}
