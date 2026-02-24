namespace ClawdFiles.Application.Interfaces;

public interface IApiKeyHasher
{
    string Hash(string rawKey);
    bool Verify(string rawKey, string hash);
    string GenerateKey();
}
