using ClawdFiles.Application.DTOs;
using ClawdFiles.Application.Interfaces;
using ClawdFiles.Domain.Entities;

namespace ClawdFiles.Application.Services;

public class KeyManagementService(IApiKeyRepository keyRepo, IApiKeyHasher hasher)
{
    public async Task<CreateKeyResponse> CreateKeyAsync(CreateKeyRequest request, CancellationToken ct = default)
    {
        var rawKey = hasher.GenerateKey();
        var prefix = rawKey[..8];
        var hash = hasher.Hash(rawKey);
        var apiKey = new ApiKey { Id = Guid.NewGuid(), Name = request.Name, KeyHash = hash, Prefix = prefix };
        await keyRepo.CreateAsync(apiKey, ct);
        return new CreateKeyResponse(rawKey, prefix, request.Name);
    }

    public async Task<List<KeyInfoResponse>> ListKeysAsync(CancellationToken ct = default)
    {
        var keys = await keyRepo.ListAllAsync(ct);
        return keys.Select(k => new KeyInfoResponse(k.Prefix, k.Name, k.CreatedAt, k.LastUsedAt, k.Buckets.Count)).ToList();
    }

    public async Task<bool> RevokeKeyAsync(string prefix, CancellationToken ct = default)
    {
        var key = await keyRepo.FindByPrefixAsync(prefix, ct);
        if (key is null) return false;
        await keyRepo.DeleteAsync(key, ct);
        return true;
    }
}
