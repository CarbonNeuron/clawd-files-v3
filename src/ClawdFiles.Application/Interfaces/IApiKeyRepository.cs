using ClawdFiles.Domain.Entities;
namespace ClawdFiles.Application.Interfaces;

public interface IApiKeyRepository
{
    Task<ApiKey> CreateAsync(ApiKey key, CancellationToken ct = default);
    Task<ApiKey?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<ApiKey?> FindByPrefixAsync(string prefix, CancellationToken ct = default);
    Task<ApiKey?> FindByHashAsync(string keyHash, CancellationToken ct = default);
    Task<List<ApiKey>> ListAllAsync(CancellationToken ct = default);
    Task DeleteAsync(ApiKey key, CancellationToken ct = default);
    Task UpdateAsync(ApiKey key, CancellationToken ct = default);
}
