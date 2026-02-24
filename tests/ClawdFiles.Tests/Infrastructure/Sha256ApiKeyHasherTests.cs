using ClawdFiles.Infrastructure.Security;

namespace ClawdFiles.Tests.Infrastructure;

public class Sha256ApiKeyHasherTests
{
    private readonly Sha256ApiKeyHasher _sut = new();

    [Fact]
    public void GenerateKey_ReturnsNonEmptyString()
    {
        var key = _sut.GenerateKey();
        Assert.False(string.IsNullOrEmpty(key));
        Assert.True(key.Length >= 32);
    }

    [Fact]
    public void Hash_ProducesDeterministicOutput()
    {
        Assert.Equal(_sut.Hash("test-key"), _sut.Hash("test-key"));
    }

    [Fact]
    public void Verify_CorrectKey_ReturnsTrue()
    {
        var key = _sut.GenerateKey();
        Assert.True(_sut.Verify(key, _sut.Hash(key)));
    }

    [Fact]
    public void Verify_WrongKey_ReturnsFalse()
    {
        Assert.False(_sut.Verify("wrong-key", _sut.Hash("correct-key")));
    }

    [Fact]
    public void GenerateKey_ProducesUniqueKeys()
    {
        Assert.NotEqual(_sut.GenerateKey(), _sut.GenerateKey());
    }
}
