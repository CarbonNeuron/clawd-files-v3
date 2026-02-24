using ClawdFiles.Domain.Entities;
using ClawdFiles.Infrastructure.Data;
using ClawdFiles.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ClawdFiles.Tests.Infrastructure;

public class EfApiKeyRepositoryTests : IDisposable
{
    private readonly ClawdFilesDbContext _db;
    private readonly EfApiKeyRepository _sut;

    public EfApiKeyRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ClawdFilesDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        _db = new ClawdFilesDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
        _sut = new EfApiKeyRepository(_db);
    }

    [Fact]
    public async Task CreateAndFindByPrefix_Works()
    {
        var key = new ApiKey { Id = Guid.NewGuid(), Name = "test", KeyHash = "hash1", Prefix = "abcd1234" };
        await _sut.CreateAsync(key);
        var found = await _sut.FindByPrefixAsync("abcd1234");
        Assert.NotNull(found);
        Assert.Equal("test", found.Name);
    }

    [Fact]
    public async Task FindByHash_Works()
    {
        var key = new ApiKey { Id = Guid.NewGuid(), Name = "test", KeyHash = "unique-hash", Prefix = "efgh5678" };
        await _sut.CreateAsync(key);
        var found = await _sut.FindByHashAsync("unique-hash");
        Assert.NotNull(found);
        Assert.Equal("efgh5678", found.Prefix);
    }

    [Fact]
    public async Task Delete_RemovesKey()
    {
        var key = new ApiKey { Id = Guid.NewGuid(), Name = "test", KeyHash = "hash2", Prefix = "ijkl9012" };
        await _sut.CreateAsync(key);
        await _sut.DeleteAsync(key);
        Assert.Null(await _sut.FindByPrefixAsync("ijkl9012"));
    }

    public void Dispose() => _db.Dispose();
}
