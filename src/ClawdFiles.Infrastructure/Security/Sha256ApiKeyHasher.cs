using System.Security.Cryptography;
using ClawdFiles.Application.Interfaces;

namespace ClawdFiles.Infrastructure.Security;

public class Sha256ApiKeyHasher : IApiKeyHasher
{
    public string GenerateKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    public string Hash(string rawKey)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(rawKey);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }

    public bool Verify(string rawKey, string hash) => Hash(rawKey) == hash;
}
