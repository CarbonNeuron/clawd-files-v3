# Clawd Files Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Scaffold and implement a Clawd Files rewrite with .NET 10, ASP.NET, Blazor Server, clean architecture, SQLite, and local filesystem storage.

**Architecture:** Classic Clean Architecture with 4 projects (Domain, Application, Infrastructure, Web) plus a test project. The Web layer hosts both API controllers and Blazor Server pages. Dependencies flow inward: Web -> Application <- Infrastructure -> Domain.

**Tech Stack:** .NET 10, ASP.NET Core, Blazor Server (interactive), EF Core + SQLite, xUnit for tests.

---

### Task 1: Create Solution and Project Structure

**Files:**
- Create: `ClawdFiles.sln`
- Create: `src/ClawdFiles.Domain/ClawdFiles.Domain.csproj`
- Create: `src/ClawdFiles.Application/ClawdFiles.Application.csproj`
- Create: `src/ClawdFiles.Infrastructure/ClawdFiles.Infrastructure.csproj`
- Create: `src/ClawdFiles.Web/ClawdFiles.Web.csproj`
- Create: `tests/ClawdFiles.Tests/ClawdFiles.Tests.csproj`

**Step 1: Create solution and projects**

```bash
cd /home/carbon/clawd-files-rewrite
dotnet new sln -n ClawdFiles
mkdir -p src tests
dotnet new classlib -n ClawdFiles.Domain -o src/ClawdFiles.Domain -f net10.0
dotnet new classlib -n ClawdFiles.Application -o src/ClawdFiles.Application -f net10.0
dotnet new classlib -n ClawdFiles.Infrastructure -o src/ClawdFiles.Infrastructure -f net10.0
dotnet new blazor -n ClawdFiles.Web -o src/ClawdFiles.Web -f net10.0 --interactivity Server --empty
dotnet new xunit -n ClawdFiles.Tests -o tests/ClawdFiles.Tests -f net10.0
```

**Step 2: Add projects to solution**

```bash
dotnet sln add src/ClawdFiles.Domain/ClawdFiles.Domain.csproj
dotnet sln add src/ClawdFiles.Application/ClawdFiles.Application.csproj
dotnet sln add src/ClawdFiles.Infrastructure/ClawdFiles.Infrastructure.csproj
dotnet sln add src/ClawdFiles.Web/ClawdFiles.Web.csproj
dotnet sln add tests/ClawdFiles.Tests/ClawdFiles.Tests.csproj
```

**Step 3: Set up project references (dependency flow)**

```bash
# Application depends on Domain
dotnet add src/ClawdFiles.Application reference src/ClawdFiles.Domain

# Infrastructure depends on Application and Domain
dotnet add src/ClawdFiles.Infrastructure reference src/ClawdFiles.Application
dotnet add src/ClawdFiles.Infrastructure reference src/ClawdFiles.Domain

# Web depends on Application and Infrastructure
dotnet add src/ClawdFiles.Web reference src/ClawdFiles.Application
dotnet add src/ClawdFiles.Web reference src/ClawdFiles.Infrastructure

# Tests depend on all layers
dotnet add tests/ClawdFiles.Tests reference src/ClawdFiles.Domain
dotnet add tests/ClawdFiles.Tests reference src/ClawdFiles.Application
dotnet add tests/ClawdFiles.Tests reference src/ClawdFiles.Infrastructure
dotnet add tests/ClawdFiles.Tests reference src/ClawdFiles.Web
```

**Step 4: Add NuGet packages**

```bash
# Infrastructure: EF Core + SQLite
dotnet add src/ClawdFiles.Infrastructure package Microsoft.EntityFrameworkCore.Sqlite
dotnet add src/ClawdFiles.Infrastructure package Microsoft.EntityFrameworkCore.Design

# Web: EF Core tools for migrations at startup (optional), and controllers support
dotnet add src/ClawdFiles.Web package Microsoft.EntityFrameworkCore.Sqlite

# Tests: EF Core in-memory for testing, plus Moq
dotnet add tests/ClawdFiles.Tests package Microsoft.EntityFrameworkCore.InMemory
dotnet add tests/ClawdFiles.Tests package Microsoft.EntityFrameworkCore.Sqlite
dotnet add tests/ClawdFiles.Tests package Moq
```

**Step 5: Clean up template files**

Delete `Class1.cs` from Domain, Application, Infrastructure. Delete the default test file from Tests. Delete the template Blazor pages/components that come with `--empty` (review what was generated first).

**Step 6: Verify solution builds**

```bash
dotnet build
```

Expected: Build succeeded with 0 errors.

**Step 7: Add a .gitignore and commit**

Create a .gitignore for .NET projects. Then:

```bash
git add -A
git commit -m "feat: scaffold solution with clean architecture project structure"
```

---

### Task 2: Domain Entities

**Files:**
- Create: `src/ClawdFiles.Domain/Entities/ApiKey.cs`
- Create: `src/ClawdFiles.Domain/Entities/Bucket.cs`
- Create: `src/ClawdFiles.Domain/Entities/BucketFileHeader.cs`
- Create: `src/ClawdFiles.Domain/ValueObjects/ExpiryPreset.cs`
- Test: `tests/ClawdFiles.Tests/Domain/ExpiryPresetTests.cs`

**Step 1: Write ExpiryPreset tests**

```csharp
// tests/ClawdFiles.Tests/Domain/ExpiryPresetTests.cs
using ClawdFiles.Domain.ValueObjects;

namespace ClawdFiles.Tests.Domain;

public class ExpiryPresetTests
{
    [Theory]
    [InlineData("1h", 1)]
    [InlineData("6h", 6)]
    [InlineData("12h", 12)]
    public void Parse_HourPresets_ReturnsCorrectTimeSpan(string preset, int expectedHours)
    {
        var result = ExpiryPreset.Parse(preset);
        Assert.Equal(TimeSpan.FromHours(expectedHours), result);
    }

    [Theory]
    [InlineData("1d", 1)]
    [InlineData("3d", 3)]
    public void Parse_DayPresets_ReturnsCorrectTimeSpan(string preset, int expectedDays)
    {
        var result = ExpiryPreset.Parse(preset);
        Assert.Equal(TimeSpan.FromDays(expectedDays), result);
    }

    [Theory]
    [InlineData("1w", 7)]
    [InlineData("2w", 14)]
    public void Parse_WeekPresets_ReturnsCorrectTimeSpan(string preset, int expectedDays)
    {
        var result = ExpiryPreset.Parse(preset);
        Assert.Equal(TimeSpan.FromDays(expectedDays), result);
    }

    [Fact]
    public void Parse_MonthPreset_Returns30Days()
    {
        var result = ExpiryPreset.Parse("1m");
        Assert.Equal(TimeSpan.FromDays(30), result);
    }

    [Fact]
    public void Parse_Never_ReturnsNull()
    {
        var result = ExpiryPreset.Parse("never");
        Assert.Null(result);
    }

    [Fact]
    public void Parse_RawSeconds_ReturnsCorrectTimeSpan()
    {
        var result = ExpiryPreset.Parse("3600");
        Assert.Equal(TimeSpan.FromSeconds(3600), result);
    }

    [Fact]
    public void Parse_NullOrEmpty_ReturnsDefault7Days()
    {
        var result = ExpiryPreset.Parse(null);
        Assert.Equal(TimeSpan.FromDays(7), result);

        result = ExpiryPreset.Parse("");
        Assert.Equal(TimeSpan.FromDays(7), result);
    }

    [Fact]
    public void Parse_InvalidPreset_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ExpiryPreset.Parse("invalid"));
    }
}
```

**Step 2: Run tests to verify they fail**

```bash
dotnet test tests/ClawdFiles.Tests --filter "FullyQualifiedName~ExpiryPresetTests" -v minimal
```

Expected: FAIL — `ExpiryPreset` does not exist yet.

**Step 3: Implement domain entities and ExpiryPreset**

```csharp
// src/ClawdFiles.Domain/Entities/ApiKey.cs
namespace ClawdFiles.Domain.Entities;

public class ApiKey
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string KeyHash { get; set; }
    public required string Prefix { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastUsedAt { get; set; }

    public ICollection<Bucket> Buckets { get; set; } = [];
}
```

```csharp
// src/ClawdFiles.Domain/Entities/Bucket.cs
namespace ClawdFiles.Domain.Entities;

public class Bucket
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Purpose { get; set; }
    public Guid OwnerId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }

    public ApiKey? Owner { get; set; }
    public ICollection<BucketFileHeader> Files { get; set; } = [];
}
```

```csharp
// src/ClawdFiles.Domain/Entities/BucketFileHeader.cs
namespace ClawdFiles.Domain.Entities;

public class BucketFileHeader
{
    public Guid Id { get; set; }
    public required string BucketId { get; set; }
    public required string Path { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }
    public required string ShortCode { get; set; }
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;

    public Bucket? Bucket { get; set; }
}
```

```csharp
// src/ClawdFiles.Domain/ValueObjects/ExpiryPreset.cs
using System.Text.RegularExpressions;

namespace ClawdFiles.Domain.ValueObjects;

public static partial class ExpiryPreset
{
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromDays(7);

    private static readonly Dictionary<string, TimeSpan?> KnownPresets = new()
    {
        ["1h"] = TimeSpan.FromHours(1),
        ["6h"] = TimeSpan.FromHours(6),
        ["12h"] = TimeSpan.FromHours(12),
        ["1d"] = TimeSpan.FromDays(1),
        ["3d"] = TimeSpan.FromDays(3),
        ["1w"] = TimeSpan.FromDays(7),
        ["2w"] = TimeSpan.FromDays(14),
        ["1m"] = TimeSpan.FromDays(30),
        ["never"] = null,
    };

    public static TimeSpan? Parse(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return DefaultExpiry;

        if (KnownPresets.TryGetValue(value.ToLowerInvariant(), out var preset))
            return preset;

        if (long.TryParse(value, out var seconds))
            return TimeSpan.FromSeconds(seconds);

        throw new ArgumentException($"Invalid expiry preset: '{value}'");
    }
}
```

**Step 4: Run tests to verify they pass**

```bash
dotnet test tests/ClawdFiles.Tests --filter "FullyQualifiedName~ExpiryPresetTests" -v minimal
```

Expected: All tests PASS.

**Step 5: Commit**

```bash
git add src/ClawdFiles.Domain/ tests/ClawdFiles.Tests/Domain/
git commit -m "feat: add domain entities (ApiKey, Bucket, BucketFileHeader) and ExpiryPreset value object"
```

---

### Task 3: Application Interfaces

**Files:**
- Create: `src/ClawdFiles.Application/Interfaces/IApiKeyRepository.cs`
- Create: `src/ClawdFiles.Application/Interfaces/IBucketRepository.cs`
- Create: `src/ClawdFiles.Application/Interfaces/IFileHeaderRepository.cs`
- Create: `src/ClawdFiles.Application/Interfaces/IFileStorage.cs`
- Create: `src/ClawdFiles.Application/Interfaces/IShortCodeGenerator.cs`
- Create: `src/ClawdFiles.Application/Interfaces/IApiKeyHasher.cs`

**Step 1: Create all interfaces**

```csharp
// src/ClawdFiles.Application/Interfaces/IApiKeyRepository.cs
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
```

```csharp
// src/ClawdFiles.Application/Interfaces/IBucketRepository.cs
using ClawdFiles.Domain.Entities;

namespace ClawdFiles.Application.Interfaces;

public interface IBucketRepository
{
    Task<Bucket> CreateAsync(Bucket bucket, CancellationToken ct = default);
    Task<Bucket?> FindByIdAsync(string id, CancellationToken ct = default);
    Task<List<Bucket>> ListByOwnerAsync(Guid ownerId, CancellationToken ct = default);
    Task<List<Bucket>> ListAllAsync(CancellationToken ct = default);
    Task<List<Bucket>> FindExpiredAsync(DateTimeOffset now, CancellationToken ct = default);
    Task UpdateAsync(Bucket bucket, CancellationToken ct = default);
    Task DeleteAsync(Bucket bucket, CancellationToken ct = default);
}
```

```csharp
// src/ClawdFiles.Application/Interfaces/IFileHeaderRepository.cs
using ClawdFiles.Domain.Entities;

namespace ClawdFiles.Application.Interfaces;

public interface IFileHeaderRepository
{
    Task<BucketFileHeader> CreateAsync(BucketFileHeader header, CancellationToken ct = default);
    Task<BucketFileHeader?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<BucketFileHeader?> FindByBucketAndPathAsync(string bucketId, string path, CancellationToken ct = default);
    Task<BucketFileHeader?> FindByShortCodeAsync(string shortCode, CancellationToken ct = default);
    Task<List<BucketFileHeader>> ListByBucketAsync(string bucketId, CancellationToken ct = default);
    Task DeleteAsync(BucketFileHeader header, CancellationToken ct = default);
    Task DeleteByBucketAsync(string bucketId, CancellationToken ct = default);
    Task UpdateAsync(BucketFileHeader header, CancellationToken ct = default);
}
```

```csharp
// src/ClawdFiles.Application/Interfaces/IFileStorage.cs
namespace ClawdFiles.Application.Interfaces;

public interface IFileStorage
{
    Task SaveFileAsync(string bucketId, string path, Stream content, CancellationToken ct = default);
    Task<Stream?> GetFileStreamAsync(string bucketId, string path, CancellationToken ct = default);
    Task<bool> DeleteFileAsync(string bucketId, string path, CancellationToken ct = default);
    Task DeleteBucketFilesAsync(string bucketId, CancellationToken ct = default);
    string GetFullPath(string bucketId, string path);
}
```

```csharp
// src/ClawdFiles.Application/Interfaces/IShortCodeGenerator.cs
namespace ClawdFiles.Application.Interfaces;

public interface IShortCodeGenerator
{
    Task<string> GenerateAsync(CancellationToken ct = default);
}
```

```csharp
// src/ClawdFiles.Application/Interfaces/IApiKeyHasher.cs
namespace ClawdFiles.Application.Interfaces;

public interface IApiKeyHasher
{
    string Hash(string rawKey);
    bool Verify(string rawKey, string hash);
    string GenerateKey();
}
```

**Step 2: Verify build**

```bash
dotnet build
```

Expected: Build succeeded.

**Step 3: Commit**

```bash
git add src/ClawdFiles.Application/
git commit -m "feat: add application layer interfaces (repositories, file storage, hasher, short codes)"
```

---

### Task 4: Application DTOs

**Files:**
- Create: `src/ClawdFiles.Application/DTOs/KeyDtos.cs`
- Create: `src/ClawdFiles.Application/DTOs/BucketDtos.cs`
- Create: `src/ClawdFiles.Application/DTOs/FileDtos.cs`
- Create: `src/ClawdFiles.Application/DTOs/ErrorResponse.cs`

**Step 1: Create DTOs**

```csharp
// src/ClawdFiles.Application/DTOs/KeyDtos.cs
namespace ClawdFiles.Application.DTOs;

public record CreateKeyRequest(string Name);

public record CreateKeyResponse(string Key, string Prefix, string Name);

public record KeyInfoResponse(
    string Prefix,
    string Name,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastUsedAt,
    int BucketCount
);
```

```csharp
// src/ClawdFiles.Application/DTOs/BucketDtos.cs
namespace ClawdFiles.Application.DTOs;

public record CreateBucketRequest(string Name, string? Description, string? Purpose, string? ExpiresIn);

public record UpdateBucketRequest(string? Name, string? Description, string? Purpose);

public record BucketResponse(
    string Id,
    string Name,
    string? Description,
    string? Purpose,
    string OwnerPrefix,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    List<FileHeaderResponse> Files
);

public record BucketListItemResponse(
    string Id,
    string Name,
    string? Description,
    string? Purpose,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    int FileCount
);
```

```csharp
// src/ClawdFiles.Application/DTOs/FileDtos.cs
namespace ClawdFiles.Application.DTOs;

public record FileHeaderResponse(
    Guid Id,
    string Path,
    string ContentType,
    long SizeBytes,
    string ShortCode,
    string ShortUrl,
    DateTimeOffset UploadedAt
);

public record UploadResponse(List<FileHeaderResponse> Uploaded);

public record DeleteFileRequest(string Path);
```

```csharp
// src/ClawdFiles.Application/DTOs/ErrorResponse.cs
namespace ClawdFiles.Application.DTOs;

public record ErrorResponse(string Error, string? Hint = null);
```

**Step 2: Verify build**

```bash
dotnet build
```

**Step 3: Commit**

```bash
git add src/ClawdFiles.Application/DTOs/
git commit -m "feat: add application DTOs for keys, buckets, files, and errors"
```

---

### Task 5: Application Services — KeyManagementService

**Files:**
- Create: `src/ClawdFiles.Application/Services/KeyManagementService.cs`
- Test: `tests/ClawdFiles.Tests/Application/KeyManagementServiceTests.cs`

**Step 1: Write failing tests**

```csharp
// tests/ClawdFiles.Tests/Application/KeyManagementServiceTests.cs
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
    public async Task CreateKey_ReturnsKeyWithPrefix()
    {
        var result = await _sut.CreateKeyAsync(new CreateKeyRequest("test-key"));

        Assert.NotNull(result);
        Assert.Equal("test-key", result.Name);
        Assert.Equal("test-key", result.Key[..8] is { Length: > 0 } ? result.Name : ""); // name matches
        Assert.Equal(8, result.Prefix.Length);
    }

    [Fact]
    public async Task ListKeys_ReturnsAllKeysWithBucketCount()
    {
        var key = new ApiKey
        {
            Id = Guid.NewGuid(),
            Name = "my-key",
            KeyHash = "hash",
            Prefix = "test-key",
            Buckets = [new Bucket { Id = "abc", Name = "test", OwnerId = Guid.NewGuid() }]
        };
        _keyRepo.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([key]);

        var result = await _sut.ListKeysAsync();

        Assert.Single(result);
        Assert.Equal("my-key", result[0].Name);
        Assert.Equal(1, result[0].BucketCount);
    }

    [Fact]
    public async Task RevokeKey_ExistingPrefix_ReturnsTrue()
    {
        var key = new ApiKey { Id = Guid.NewGuid(), Name = "k", KeyHash = "h", Prefix = "test1234" };
        _keyRepo.Setup(r => r.FindByPrefixAsync("test1234", It.IsAny<CancellationToken>()))
            .ReturnsAsync(key);

        var result = await _sut.RevokeKeyAsync("test1234");
        Assert.True(result);
    }

    [Fact]
    public async Task RevokeKey_NonExistentPrefix_ReturnsFalse()
    {
        _keyRepo.Setup(r => r.FindByPrefixAsync("noexist1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApiKey?)null);

        var result = await _sut.RevokeKeyAsync("noexist1");
        Assert.False(result);
    }
}
```

**Step 2: Run tests to verify they fail**

```bash
dotnet test tests/ClawdFiles.Tests --filter "FullyQualifiedName~KeyManagementServiceTests" -v minimal
```

Expected: FAIL — `KeyManagementService` does not exist.

**Step 3: Implement KeyManagementService**

```csharp
// src/ClawdFiles.Application/Services/KeyManagementService.cs
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

        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            KeyHash = hash,
            Prefix = prefix,
        };

        await keyRepo.CreateAsync(apiKey, ct);

        return new CreateKeyResponse(rawKey, prefix, request.Name);
    }

    public async Task<List<KeyInfoResponse>> ListKeysAsync(CancellationToken ct = default)
    {
        var keys = await keyRepo.ListAllAsync(ct);
        return keys.Select(k => new KeyInfoResponse(
            k.Prefix,
            k.Name,
            k.CreatedAt,
            k.LastUsedAt,
            k.Buckets.Count
        )).ToList();
    }

    public async Task<bool> RevokeKeyAsync(string prefix, CancellationToken ct = default)
    {
        var key = await keyRepo.FindByPrefixAsync(prefix, ct);
        if (key is null) return false;

        await keyRepo.DeleteAsync(key, ct);
        return true;
    }
}
```

**Step 4: Run tests to verify they pass**

```bash
dotnet test tests/ClawdFiles.Tests --filter "FullyQualifiedName~KeyManagementServiceTests" -v minimal
```

Expected: All PASS.

**Step 5: Commit**

```bash
git add src/ClawdFiles.Application/Services/KeyManagementService.cs tests/ClawdFiles.Tests/Application/
git commit -m "feat: add KeyManagementService with create, list, and revoke"
```

---

### Task 6: Application Services — BucketService

**Files:**
- Create: `src/ClawdFiles.Application/Services/BucketService.cs`
- Test: `tests/ClawdFiles.Tests/Application/BucketServiceTests.cs`

**Step 1: Write failing tests**

```csharp
// tests/ClawdFiles.Tests/Application/BucketServiceTests.cs
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

        Assert.NotNull(result);
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
        _bucketRepo.Setup(r => r.FindByIdAsync("nope", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Bucket?)null);

        var result = await _sut.GetBucketAsync("nope");
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteBucket_WrongOwner_NotAdmin_ReturnsFalse()
    {
        var bucket = new Bucket { Id = "abc", Name = "test", OwnerId = Guid.NewGuid() };
        _bucketRepo.Setup(r => r.FindByIdAsync("abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(bucket);

        var result = await _sut.DeleteBucketAsync("abc", _ownerId, isAdmin: false);
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteBucket_AsAdmin_ReturnsTrue()
    {
        var bucket = new Bucket { Id = "abc", Name = "test", OwnerId = Guid.NewGuid(), Files = [] };
        bucket.Owner = new ApiKey { Id = bucket.OwnerId, Name = "k", KeyHash = "h", Prefix = "12345678" };
        _bucketRepo.Setup(r => r.FindByIdAsync("abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(bucket);

        var result = await _sut.DeleteBucketAsync("abc", _ownerId, isAdmin: true);
        Assert.True(result);
    }
}
```

**Step 2: Run tests to verify they fail**

```bash
dotnet test tests/ClawdFiles.Tests --filter "FullyQualifiedName~BucketServiceTests" -v minimal
```

**Step 3: Implement BucketService**

```csharp
// src/ClawdFiles.Application/Services/BucketService.cs
using System.Security.Cryptography;
using ClawdFiles.Application.DTOs;
using ClawdFiles.Application.Interfaces;
using ClawdFiles.Domain.Entities;
using ClawdFiles.Domain.ValueObjects;

namespace ClawdFiles.Application.Services;

public class BucketService(
    IBucketRepository bucketRepo,
    IFileHeaderRepository fileRepo,
    IFileStorage storage)
{
    private static readonly char[] IdChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

    public async Task<BucketListItemResponse> CreateBucketAsync(CreateBucketRequest request, Guid ownerId, CancellationToken ct = default)
    {
        var expiry = ExpiryPreset.Parse(request.ExpiresIn);
        var bucket = new Bucket
        {
            Id = GenerateId(5),
            Name = request.Name,
            Description = request.Description,
            Purpose = request.Purpose,
            OwnerId = ownerId,
            ExpiresAt = expiry.HasValue ? DateTimeOffset.UtcNow + expiry.Value : null,
        };

        await bucketRepo.CreateAsync(bucket, ct);

        return new BucketListItemResponse(bucket.Id, bucket.Name, bucket.Description, bucket.Purpose, bucket.CreatedAt, bucket.ExpiresAt, 0);
    }

    public async Task<BucketResponse?> GetBucketAsync(string id, CancellationToken ct = default)
    {
        var bucket = await bucketRepo.FindByIdAsync(id, ct);
        if (bucket is null) return null;

        var files = await fileRepo.ListByBucketAsync(id, ct);
        var ownerPrefix = bucket.Owner?.Prefix ?? "unknown";

        return new BucketResponse(
            bucket.Id, bucket.Name, bucket.Description, bucket.Purpose,
            ownerPrefix, bucket.CreatedAt, bucket.ExpiresAt,
            files.Select(f => new FileHeaderResponse(
                f.Id, f.Path, f.ContentType, f.SizeBytes, f.ShortCode,
                $"/s/{f.ShortCode}", f.UploadedAt
            )).ToList()
        );
    }

    public async Task<List<BucketListItemResponse>> ListBucketsAsync(Guid? ownerId, bool isAdmin, CancellationToken ct = default)
    {
        var buckets = isAdmin
            ? await bucketRepo.ListAllAsync(ct)
            : await bucketRepo.ListByOwnerAsync(ownerId!.Value, ct);

        return buckets.Select(b => new BucketListItemResponse(
            b.Id, b.Name, b.Description, b.Purpose, b.CreatedAt, b.ExpiresAt, b.Files.Count
        )).ToList();
    }

    public async Task<BucketListItemResponse?> UpdateBucketAsync(string id, UpdateBucketRequest request, Guid callerId, bool isAdmin, CancellationToken ct = default)
    {
        var bucket = await bucketRepo.FindByIdAsync(id, ct);
        if (bucket is null) return null;
        if (bucket.OwnerId != callerId && !isAdmin) return null;

        if (request.Name is not null) bucket.Name = request.Name;
        if (request.Description is not null) bucket.Description = request.Description;
        if (request.Purpose is not null) bucket.Purpose = request.Purpose;

        await bucketRepo.UpdateAsync(bucket, ct);

        return new BucketListItemResponse(bucket.Id, bucket.Name, bucket.Description, bucket.Purpose, bucket.CreatedAt, bucket.ExpiresAt, bucket.Files.Count);
    }

    public async Task<bool> DeleteBucketAsync(string id, Guid callerId, bool isAdmin, CancellationToken ct = default)
    {
        var bucket = await bucketRepo.FindByIdAsync(id, ct);
        if (bucket is null) return false;
        if (bucket.OwnerId != callerId && !isAdmin) return false;

        await fileRepo.DeleteByBucketAsync(id, ct);
        await storage.DeleteBucketFilesAsync(id, ct);
        await bucketRepo.DeleteAsync(bucket, ct);
        return true;
    }

    private static string GenerateId(int length)
    {
        return string.Create(length, (object?)null, (span, _) =>
        {
            Span<byte> random = stackalloc byte[length];
            RandomNumberGenerator.Fill(random);
            for (var i = 0; i < length; i++)
                span[i] = IdChars[random[i] % IdChars.Length];
        });
    }
}
```

**Step 4: Run tests to verify they pass**

```bash
dotnet test tests/ClawdFiles.Tests --filter "FullyQualifiedName~BucketServiceTests" -v minimal
```

**Step 5: Commit**

```bash
git add src/ClawdFiles.Application/Services/BucketService.cs tests/ClawdFiles.Tests/Application/BucketServiceTests.cs
git commit -m "feat: add BucketService with create, get, list, update, delete"
```

---

### Task 7: Application Services — FileService

**Files:**
- Create: `src/ClawdFiles.Application/Services/FileService.cs`
- Test: `tests/ClawdFiles.Tests/Application/FileServiceTests.cs`

**Step 1: Write failing tests**

```csharp
// tests/ClawdFiles.Tests/Application/FileServiceTests.cs
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
        _shortCodes.Setup(s => s.GenerateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("xK9mQ2");
        _fileRepo.Setup(r => r.CreateAsync(It.IsAny<BucketFileHeader>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BucketFileHeader h, CancellationToken _) => h);

        _sut = new FileService(_fileRepo.Object, _storage.Object, _shortCodes.Object, _bucketRepo.Object);
    }

    [Fact]
    public async Task UploadFile_CreatesHeaderAndStoresFile()
    {
        var bucket = new Bucket { Id = "abc", Name = "test", OwnerId = Guid.NewGuid() };
        _bucketRepo.Setup(r => r.FindByIdAsync("abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(bucket);

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
        _bucketRepo.Setup(r => r.FindByIdAsync("nope", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Bucket?)null);

        using var stream = new MemoryStream([1, 2, 3]);
        var result = await _sut.UploadFileAsync("nope", "test.txt", "text/plain", stream);

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveShortCode_ReturnsFileHeader()
    {
        var header = new BucketFileHeader
        {
            Id = Guid.NewGuid(), BucketId = "abc", Path = "test.txt",
            ContentType = "text/plain", ShortCode = "xK9mQ2"
        };
        _fileRepo.Setup(r => r.FindByShortCodeAsync("xK9mQ2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(header);

        var result = await _sut.ResolveShortCodeAsync("xK9mQ2");
        Assert.NotNull(result);
        Assert.Equal("abc", result.BucketId);
    }

    [Fact]
    public async Task DeleteFile_ExistingFile_ReturnsTrue()
    {
        var header = new BucketFileHeader
        {
            Id = Guid.NewGuid(), BucketId = "abc", Path = "test.txt",
            ContentType = "text/plain", ShortCode = "xK9mQ2"
        };
        _fileRepo.Setup(r => r.FindByBucketAndPathAsync("abc", "test.txt", It.IsAny<CancellationToken>()))
            .ReturnsAsync(header);
        _storage.Setup(s => s.DeleteFileAsync("abc", "test.txt", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.DeleteFileAsync("abc", "test.txt");
        Assert.True(result);
    }
}
```

**Step 2: Run tests to verify they fail**

```bash
dotnet test tests/ClawdFiles.Tests --filter "FullyQualifiedName~FileServiceTests" -v minimal
```

**Step 3: Implement FileService**

```csharp
// src/ClawdFiles.Application/Services/FileService.cs
using ClawdFiles.Application.DTOs;
using ClawdFiles.Application.Interfaces;
using ClawdFiles.Domain.Entities;

namespace ClawdFiles.Application.Services;

public class FileService(
    IFileHeaderRepository fileRepo,
    IFileStorage storage,
    IShortCodeGenerator shortCodes,
    IBucketRepository bucketRepo)
{
    public async Task<FileHeaderResponse?> UploadFileAsync(
        string bucketId, string path, string contentType, Stream content,
        CancellationToken ct = default)
    {
        var bucket = await bucketRepo.FindByIdAsync(bucketId, ct);
        if (bucket is null) return null;

        await storage.SaveFileAsync(bucketId, path, content, ct);

        // Check if file already exists (overwrite scenario)
        var existing = await fileRepo.FindByBucketAndPathAsync(bucketId, path, ct);
        if (existing is not null)
        {
            existing.ContentType = contentType;
            existing.SizeBytes = content.Length;
            existing.UploadedAt = DateTimeOffset.UtcNow;
            await fileRepo.UpdateAsync(existing, ct);
            return new FileHeaderResponse(existing.Id, existing.Path, existing.ContentType,
                existing.SizeBytes, existing.ShortCode, $"/s/{existing.ShortCode}", existing.UploadedAt);
        }

        var shortCode = await shortCodes.GenerateAsync(ct);
        var header = new BucketFileHeader
        {
            Id = Guid.NewGuid(),
            BucketId = bucketId,
            Path = path,
            ContentType = contentType,
            SizeBytes = content.Length,
            ShortCode = shortCode,
        };

        await fileRepo.CreateAsync(header, ct);

        return new FileHeaderResponse(header.Id, header.Path, header.ContentType,
            header.SizeBytes, header.ShortCode, $"/s/{header.ShortCode}", header.UploadedAt);
    }

    public async Task<BucketFileHeader?> ResolveShortCodeAsync(string shortCode, CancellationToken ct = default)
    {
        return await fileRepo.FindByShortCodeAsync(shortCode, ct);
    }

    public async Task<bool> DeleteFileAsync(string bucketId, string path, CancellationToken ct = default)
    {
        var header = await fileRepo.FindByBucketAndPathAsync(bucketId, path, ct);
        if (header is null) return false;

        await storage.DeleteFileAsync(bucketId, path, ct);
        await fileRepo.DeleteAsync(header, ct);
        return true;
    }
}
```

**Step 4: Run tests to verify they pass**

```bash
dotnet test tests/ClawdFiles.Tests --filter "FullyQualifiedName~FileServiceTests" -v minimal
```

**Step 5: Commit**

```bash
git add src/ClawdFiles.Application/Services/FileService.cs tests/ClawdFiles.Tests/Application/FileServiceTests.cs
git commit -m "feat: add FileService with upload, delete, and short code resolution"
```

---

### Task 8: Application Services — BucketSummaryService

**Files:**
- Create: `src/ClawdFiles.Application/Services/BucketSummaryService.cs`
- Test: `tests/ClawdFiles.Tests/Application/BucketSummaryServiceTests.cs`

**Step 1: Write failing tests**

```csharp
// tests/ClawdFiles.Tests/Application/BucketSummaryServiceTests.cs
using ClawdFiles.Application.Interfaces;
using ClawdFiles.Application.Services;
using ClawdFiles.Domain.Entities;
using Moq;

namespace ClawdFiles.Tests.Application;

public class BucketSummaryServiceTests
{
    private readonly Mock<IBucketRepository> _bucketRepo = new();
    private readonly Mock<IFileHeaderRepository> _fileRepo = new();
    private readonly Mock<IFileStorage> _storage = new();
    private readonly BucketSummaryService _sut;

    public BucketSummaryServiceTests()
    {
        _sut = new BucketSummaryService(_bucketRepo.Object, _fileRepo.Object, _storage.Object);
    }

    [Fact]
    public async Task GetSummary_ReturnsPlainTextWithFileList()
    {
        var bucket = new Bucket
        {
            Id = "abc", Name = "Test Bucket", Description = "A test",
            OwnerId = Guid.NewGuid()
        };
        bucket.Owner = new ApiKey { Id = bucket.OwnerId, Name = "k", KeyHash = "h", Prefix = "12345678" };

        _bucketRepo.Setup(r => r.FindByIdAsync("abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(bucket);
        _fileRepo.Setup(r => r.ListByBucketAsync("abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new BucketFileHeader { Id = Guid.NewGuid(), BucketId = "abc", Path = "readme.md", ContentType = "text/markdown", SizeBytes = 100, ShortCode = "abc123" },
                new BucketFileHeader { Id = Guid.NewGuid(), BucketId = "abc", Path = "data.json", ContentType = "application/json", SizeBytes = 500, ShortCode = "def456" },
            ]);
        _storage.Setup(s => s.GetFileStreamAsync("abc", "readme.md", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream("# Hello World"u8.ToArray()));

        var result = await _sut.GetSummaryAsync("abc");

        Assert.NotNull(result);
        Assert.Contains("Test Bucket", result);
        Assert.Contains("readme.md", result);
        Assert.Contains("data.json", result);
        Assert.Contains("# Hello World", result);
    }

    [Fact]
    public async Task GetSummary_BucketNotFound_ReturnsNull()
    {
        _bucketRepo.Setup(r => r.FindByIdAsync("nope", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Bucket?)null);

        var result = await _sut.GetSummaryAsync("nope");
        Assert.Null(result);
    }
}
```

**Step 2: Run tests to verify they fail**

```bash
dotnet test tests/ClawdFiles.Tests --filter "FullyQualifiedName~BucketSummaryServiceTests" -v minimal
```

**Step 3: Implement BucketSummaryService**

```csharp
// src/ClawdFiles.Application/Services/BucketSummaryService.cs
using System.Text;
using ClawdFiles.Application.Interfaces;

namespace ClawdFiles.Application.Services;

public class BucketSummaryService(
    IBucketRepository bucketRepo,
    IFileHeaderRepository fileRepo,
    IFileStorage storage)
{
    public async Task<string?> GetSummaryAsync(string bucketId, CancellationToken ct = default)
    {
        var bucket = await bucketRepo.FindByIdAsync(bucketId, ct);
        if (bucket is null) return null;

        var files = await fileRepo.ListByBucketAsync(bucketId, ct);
        var sb = new StringBuilder();

        sb.AppendLine($"# {bucket.Name}");
        if (!string.IsNullOrEmpty(bucket.Description))
            sb.AppendLine(bucket.Description);
        sb.AppendLine();

        sb.AppendLine($"Bucket ID: {bucket.Id}");
        if (!string.IsNullOrEmpty(bucket.Purpose))
            sb.AppendLine($"Purpose: {bucket.Purpose}");
        sb.AppendLine($"Created: {bucket.CreatedAt:u}");
        if (bucket.ExpiresAt.HasValue)
            sb.AppendLine($"Expires: {bucket.ExpiresAt:u}");
        sb.AppendLine($"Files: {files.Count}");
        sb.AppendLine();

        sb.AppendLine("## File Listing");
        foreach (var file in files)
        {
            sb.AppendLine($"- {file.Path} ({file.ContentType}, {file.SizeBytes} bytes)");
        }
        sb.AppendLine();

        // Include README content if present
        var readme = files.FirstOrDefault(f =>
            f.Path.Equals("README.md", StringComparison.OrdinalIgnoreCase) ||
            f.Path.Equals("readme.md", StringComparison.OrdinalIgnoreCase));

        if (readme is not null)
        {
            var stream = await storage.GetFileStreamAsync(bucketId, readme.Path, ct);
            if (stream is not null)
            {
                using var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync(ct);
                sb.AppendLine("## README Content");
                sb.AppendLine(content);
            }
        }

        return sb.ToString();
    }
}
```

**Step 4: Run tests to verify they pass**

```bash
dotnet test tests/ClawdFiles.Tests --filter "FullyQualifiedName~BucketSummaryServiceTests" -v minimal
```

**Step 5: Commit**

```bash
git add src/ClawdFiles.Application/Services/BucketSummaryService.cs tests/ClawdFiles.Tests/Application/BucketSummaryServiceTests.cs
git commit -m "feat: add BucketSummaryService for LLM-friendly plain-text summaries"
```

---

### Task 9: Infrastructure — DbContext and EF Core Configuration

**Files:**
- Create: `src/ClawdFiles.Infrastructure/Data/ClawdFilesDbContext.cs`
- Create: `src/ClawdFiles.Infrastructure/Data/Configurations/ApiKeyConfiguration.cs`
- Create: `src/ClawdFiles.Infrastructure/Data/Configurations/BucketConfiguration.cs`
- Create: `src/ClawdFiles.Infrastructure/Data/Configurations/BucketFileHeaderConfiguration.cs`

**Step 1: Implement DbContext with entity configurations**

```csharp
// src/ClawdFiles.Infrastructure/Data/ClawdFilesDbContext.cs
using ClawdFiles.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClawdFiles.Infrastructure.Data;

public class ClawdFilesDbContext(DbContextOptions<ClawdFilesDbContext> options) : DbContext(options)
{
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<Bucket> Buckets => Set<Bucket>();
    public DbSet<BucketFileHeader> FileHeaders => Set<BucketFileHeader>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClawdFilesDbContext).Assembly);
    }
}
```

```csharp
// src/ClawdFiles.Infrastructure/Data/Configurations/ApiKeyConfiguration.cs
using ClawdFiles.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClawdFiles.Infrastructure.Data.Configurations;

public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.HasKey(k => k.Id);
        builder.Property(k => k.Name).IsRequired().HasMaxLength(200);
        builder.Property(k => k.KeyHash).IsRequired().HasMaxLength(128);
        builder.Property(k => k.Prefix).IsRequired().HasMaxLength(8);
        builder.HasIndex(k => k.Prefix).IsUnique();
        builder.HasIndex(k => k.KeyHash).IsUnique();
    }
}
```

```csharp
// src/ClawdFiles.Infrastructure/Data/Configurations/BucketConfiguration.cs
using ClawdFiles.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClawdFiles.Infrastructure.Data.Configurations;

public class BucketConfiguration : IEntityTypeConfiguration<Bucket>
{
    public void Configure(EntityTypeBuilder<Bucket> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasMaxLength(10);
        builder.Property(b => b.Name).IsRequired().HasMaxLength(200);
        builder.Property(b => b.Description).HasMaxLength(1000);
        builder.Property(b => b.Purpose).HasMaxLength(200);
        builder.HasOne(b => b.Owner).WithMany(k => k.Buckets).HasForeignKey(b => b.OwnerId);
        builder.HasIndex(b => b.ExpiresAt);
    }
}
```

```csharp
// src/ClawdFiles.Infrastructure/Data/Configurations/BucketFileHeaderConfiguration.cs
using ClawdFiles.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClawdFiles.Infrastructure.Data.Configurations;

public class BucketFileHeaderConfiguration : IEntityTypeConfiguration<BucketFileHeader>
{
    public void Configure(EntityTypeBuilder<BucketFileHeader> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Path).IsRequired().HasMaxLength(500);
        builder.Property(f => f.ContentType).IsRequired().HasMaxLength(200);
        builder.Property(f => f.ShortCode).IsRequired().HasMaxLength(10);
        builder.HasIndex(f => f.ShortCode).IsUnique();
        builder.HasIndex(f => new { f.BucketId, f.Path }).IsUnique();
        builder.HasOne(f => f.Bucket).WithMany(b => b.Files).HasForeignKey(f => f.BucketId);
    }
}
```

**Step 2: Verify build**

```bash
dotnet build
```

**Step 3: Commit**

```bash
git add src/ClawdFiles.Infrastructure/Data/
git commit -m "feat: add EF Core DbContext with entity configurations for SQLite"
```

---

### Task 10: Infrastructure — Repository Implementations

**Files:**
- Create: `src/ClawdFiles.Infrastructure/Repositories/EfApiKeyRepository.cs`
- Create: `src/ClawdFiles.Infrastructure/Repositories/EfBucketRepository.cs`
- Create: `src/ClawdFiles.Infrastructure/Repositories/EfFileHeaderRepository.cs`
- Test: `tests/ClawdFiles.Tests/Infrastructure/EfApiKeyRepositoryTests.cs`

**Step 1: Write a focused test for EfApiKeyRepository**

```csharp
// tests/ClawdFiles.Tests/Infrastructure/EfApiKeyRepositoryTests.cs
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

        var found = await _sut.FindByPrefixAsync("ijkl9012");
        Assert.Null(found);
    }

    public void Dispose() => _db.Dispose();
}
```

**Step 2: Run tests to verify they fail**

```bash
dotnet test tests/ClawdFiles.Tests --filter "FullyQualifiedName~EfApiKeyRepositoryTests" -v minimal
```

**Step 3: Implement all three repositories**

```csharp
// src/ClawdFiles.Infrastructure/Repositories/EfApiKeyRepository.cs
using ClawdFiles.Application.Interfaces;
using ClawdFiles.Domain.Entities;
using ClawdFiles.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClawdFiles.Infrastructure.Repositories;

public class EfApiKeyRepository(ClawdFilesDbContext db) : IApiKeyRepository
{
    public async Task<ApiKey> CreateAsync(ApiKey key, CancellationToken ct = default)
    {
        db.ApiKeys.Add(key);
        await db.SaveChangesAsync(ct);
        return key;
    }

    public async Task<ApiKey?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => await db.ApiKeys.Include(k => k.Buckets).FirstOrDefaultAsync(k => k.Id == id, ct);

    public async Task<ApiKey?> FindByPrefixAsync(string prefix, CancellationToken ct = default)
        => await db.ApiKeys.Include(k => k.Buckets).FirstOrDefaultAsync(k => k.Prefix == prefix, ct);

    public async Task<ApiKey?> FindByHashAsync(string keyHash, CancellationToken ct = default)
        => await db.ApiKeys.Include(k => k.Buckets).FirstOrDefaultAsync(k => k.KeyHash == keyHash, ct);

    public async Task<List<ApiKey>> ListAllAsync(CancellationToken ct = default)
        => await db.ApiKeys.Include(k => k.Buckets).OrderByDescending(k => k.CreatedAt).ToListAsync(ct);

    public async Task DeleteAsync(ApiKey key, CancellationToken ct = default)
    {
        db.ApiKeys.Remove(key);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ApiKey key, CancellationToken ct = default)
    {
        db.ApiKeys.Update(key);
        await db.SaveChangesAsync(ct);
    }
}
```

```csharp
// src/ClawdFiles.Infrastructure/Repositories/EfBucketRepository.cs
using ClawdFiles.Application.Interfaces;
using ClawdFiles.Domain.Entities;
using ClawdFiles.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClawdFiles.Infrastructure.Repositories;

public class EfBucketRepository(ClawdFilesDbContext db) : IBucketRepository
{
    public async Task<Bucket> CreateAsync(Bucket bucket, CancellationToken ct = default)
    {
        db.Buckets.Add(bucket);
        await db.SaveChangesAsync(ct);
        return bucket;
    }

    public async Task<Bucket?> FindByIdAsync(string id, CancellationToken ct = default)
        => await db.Buckets.Include(b => b.Owner).Include(b => b.Files).FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<List<Bucket>> ListByOwnerAsync(Guid ownerId, CancellationToken ct = default)
        => await db.Buckets.Include(b => b.Files).Where(b => b.OwnerId == ownerId).OrderByDescending(b => b.CreatedAt).ToListAsync(ct);

    public async Task<List<Bucket>> ListAllAsync(CancellationToken ct = default)
        => await db.Buckets.Include(b => b.Files).OrderByDescending(b => b.CreatedAt).ToListAsync(ct);

    public async Task<List<Bucket>> FindExpiredAsync(DateTimeOffset now, CancellationToken ct = default)
        => await db.Buckets.Include(b => b.Files).Where(b => b.ExpiresAt != null && b.ExpiresAt <= now).ToListAsync(ct);

    public async Task UpdateAsync(Bucket bucket, CancellationToken ct = default)
    {
        db.Buckets.Update(bucket);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Bucket bucket, CancellationToken ct = default)
    {
        db.Buckets.Remove(bucket);
        await db.SaveChangesAsync(ct);
    }
}
```

```csharp
// src/ClawdFiles.Infrastructure/Repositories/EfFileHeaderRepository.cs
using ClawdFiles.Application.Interfaces;
using ClawdFiles.Domain.Entities;
using ClawdFiles.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClawdFiles.Infrastructure.Repositories;

public class EfFileHeaderRepository(ClawdFilesDbContext db) : IFileHeaderRepository
{
    public async Task<BucketFileHeader> CreateAsync(BucketFileHeader header, CancellationToken ct = default)
    {
        db.FileHeaders.Add(header);
        await db.SaveChangesAsync(ct);
        return header;
    }

    public async Task<BucketFileHeader?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => await db.FileHeaders.FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<BucketFileHeader?> FindByBucketAndPathAsync(string bucketId, string path, CancellationToken ct = default)
        => await db.FileHeaders.FirstOrDefaultAsync(f => f.BucketId == bucketId && f.Path == path, ct);

    public async Task<BucketFileHeader?> FindByShortCodeAsync(string shortCode, CancellationToken ct = default)
        => await db.FileHeaders.FirstOrDefaultAsync(f => f.ShortCode == shortCode, ct);

    public async Task<List<BucketFileHeader>> ListByBucketAsync(string bucketId, CancellationToken ct = default)
        => await db.FileHeaders.Where(f => f.BucketId == bucketId).OrderBy(f => f.Path).ToListAsync(ct);

    public async Task DeleteAsync(BucketFileHeader header, CancellationToken ct = default)
    {
        db.FileHeaders.Remove(header);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteByBucketAsync(string bucketId, CancellationToken ct = default)
    {
        var headers = await db.FileHeaders.Where(f => f.BucketId == bucketId).ToListAsync(ct);
        db.FileHeaders.RemoveRange(headers);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(BucketFileHeader header, CancellationToken ct = default)
    {
        db.FileHeaders.Update(header);
        await db.SaveChangesAsync(ct);
    }
}
```

**Step 4: Run tests to verify they pass**

```bash
dotnet test tests/ClawdFiles.Tests --filter "FullyQualifiedName~EfApiKeyRepositoryTests" -v minimal
```

**Step 5: Commit**

```bash
git add src/ClawdFiles.Infrastructure/Repositories/ tests/ClawdFiles.Tests/Infrastructure/
git commit -m "feat: add EF Core repository implementations for ApiKey, Bucket, BucketFileHeader"
```

---

### Task 11: Infrastructure — LocalFileStorage

**Files:**
- Create: `src/ClawdFiles.Infrastructure/Storage/LocalFileStorage.cs`
- Test: `tests/ClawdFiles.Tests/Infrastructure/LocalFileStorageTests.cs`

**Step 1: Write failing tests**

```csharp
// tests/ClawdFiles.Tests/Infrastructure/LocalFileStorageTests.cs
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
        var data = "hello world"u8.ToArray();
        using var input = new MemoryStream(data);
        await _sut.SaveFileAsync("bucket1", "test.txt", input);

        using var output = await _sut.GetFileStreamAsync("bucket1", "test.txt");
        Assert.NotNull(output);

        using var reader = new StreamReader(output);
        var content = await reader.ReadToEndAsync();
        Assert.Equal("hello world", content);
    }

    [Fact]
    public async Task Delete_RemovesFile()
    {
        using var input = new MemoryStream("data"u8.ToArray());
        await _sut.SaveFileAsync("bucket1", "test.txt", input);

        var deleted = await _sut.DeleteFileAsync("bucket1", "test.txt");
        Assert.True(deleted);

        var stream = await _sut.GetFileStreamAsync("bucket1", "test.txt");
        Assert.Null(stream);
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
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
```

**Step 2: Run tests to verify they fail**

```bash
dotnet test tests/ClawdFiles.Tests --filter "FullyQualifiedName~LocalFileStorageTests" -v minimal
```

**Step 3: Implement LocalFileStorage**

```csharp
// src/ClawdFiles.Infrastructure/Storage/LocalFileStorage.cs
using ClawdFiles.Application.Interfaces;

namespace ClawdFiles.Infrastructure.Storage;

public class LocalFileStorage(string rootPath) : IFileStorage
{
    public async Task SaveFileAsync(string bucketId, string path, Stream content, CancellationToken ct = default)
    {
        var fullPath = GetFullPath(bucketId, path);
        var dir = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(dir);

        await using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        await content.CopyToAsync(fs, ct);
    }

    public Task<Stream?> GetFileStreamAsync(string bucketId, string path, CancellationToken ct = default)
    {
        var fullPath = GetFullPath(bucketId, path);
        if (!File.Exists(fullPath)) return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream?>(stream);
    }

    public Task<bool> DeleteFileAsync(string bucketId, string path, CancellationToken ct = default)
    {
        var fullPath = GetFullPath(bucketId, path);
        if (!File.Exists(fullPath)) return Task.FromResult(false);

        File.Delete(fullPath);
        return Task.FromResult(true);
    }

    public Task DeleteBucketFilesAsync(string bucketId, CancellationToken ct = default)
    {
        var dir = Path.Combine(rootPath, bucketId);
        if (Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);

        return Task.CompletedTask;
    }

    public string GetFullPath(string bucketId, string path)
        => Path.Combine(rootPath, bucketId, path);
}
```

**Step 4: Run tests to verify they pass**

```bash
dotnet test tests/ClawdFiles.Tests --filter "FullyQualifiedName~LocalFileStorageTests" -v minimal
```

**Step 5: Commit**

```bash
git add src/ClawdFiles.Infrastructure/Storage/ tests/ClawdFiles.Tests/Infrastructure/LocalFileStorageTests.cs
git commit -m "feat: add LocalFileStorage for filesystem-based file storage"
```

---

### Task 12: Infrastructure — Security and Utilities

**Files:**
- Create: `src/ClawdFiles.Infrastructure/Security/Sha256ApiKeyHasher.cs`
- Create: `src/ClawdFiles.Infrastructure/Services/RandomShortCodeGenerator.cs`
- Test: `tests/ClawdFiles.Tests/Infrastructure/Sha256ApiKeyHasherTests.cs`

**Step 1: Write failing tests**

```csharp
// tests/ClawdFiles.Tests/Infrastructure/Sha256ApiKeyHasherTests.cs
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
        var hash1 = _sut.Hash("test-key");
        var hash2 = _sut.Hash("test-key");
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Verify_CorrectKey_ReturnsTrue()
    {
        var key = _sut.GenerateKey();
        var hash = _sut.Hash(key);
        Assert.True(_sut.Verify(key, hash));
    }

    [Fact]
    public void Verify_WrongKey_ReturnsFalse()
    {
        var hash = _sut.Hash("correct-key");
        Assert.False(_sut.Verify("wrong-key", hash));
    }

    [Fact]
    public void GenerateKey_ProducesUniqueKeys()
    {
        var key1 = _sut.GenerateKey();
        var key2 = _sut.GenerateKey();
        Assert.NotEqual(key1, key2);
    }
}
```

**Step 2: Run tests to verify they fail**

```bash
dotnet test tests/ClawdFiles.Tests --filter "FullyQualifiedName~Sha256ApiKeyHasherTests" -v minimal
```

**Step 3: Implement Sha256ApiKeyHasher and RandomShortCodeGenerator**

```csharp
// src/ClawdFiles.Infrastructure/Security/Sha256ApiKeyHasher.cs
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

    public bool Verify(string rawKey, string hash)
    {
        return Hash(rawKey) == hash;
    }
}
```

```csharp
// src/ClawdFiles.Infrastructure/Services/RandomShortCodeGenerator.cs
using System.Security.Cryptography;
using ClawdFiles.Application.Interfaces;

namespace ClawdFiles.Infrastructure.Services;

public class RandomShortCodeGenerator(IFileHeaderRepository fileRepo) : IShortCodeGenerator
{
    private static readonly char[] Chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

    public async Task<string> GenerateAsync(CancellationToken ct = default)
    {
        const int maxAttempts = 10;
        for (var i = 0; i < maxAttempts; i++)
        {
            var code = GenerateCode(6);
            var existing = await fileRepo.FindByShortCodeAsync(code, ct);
            if (existing is null) return code;
        }

        throw new InvalidOperationException("Failed to generate a unique short code after multiple attempts");
    }

    private static string GenerateCode(int length)
    {
        return string.Create(length, (object?)null, (span, _) =>
        {
            Span<byte> random = stackalloc byte[length];
            RandomNumberGenerator.Fill(random);
            for (var i = 0; i < length; i++)
                span[i] = Chars[random[i] % Chars.Length];
        });
    }
}
```

**Step 4: Run tests to verify they pass**

```bash
dotnet test tests/ClawdFiles.Tests --filter "FullyQualifiedName~Sha256ApiKeyHasherTests" -v minimal
```

**Step 5: Commit**

```bash
git add src/ClawdFiles.Infrastructure/Security/ src/ClawdFiles.Infrastructure/Services/ tests/ClawdFiles.Tests/Infrastructure/Sha256ApiKeyHasherTests.cs
git commit -m "feat: add Sha256ApiKeyHasher and RandomShortCodeGenerator"
```

---

### Task 13: Infrastructure — BucketExpiryService

**Files:**
- Create: `src/ClawdFiles.Infrastructure/Services/BucketExpiryService.cs`

**Step 1: Implement the background service**

```csharp
// src/ClawdFiles.Infrastructure/Services/BucketExpiryService.cs
using ClawdFiles.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClawdFiles.Infrastructure.Services;

public class ExpiryOptions
{
    public int CleanupIntervalMinutes { get; set; } = 5;
}

public class BucketExpiryService(
    IServiceScopeFactory scopeFactory,
    IOptions<ExpiryOptions> options,
    ILogger<BucketExpiryService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(options.Value.CleanupIntervalMinutes);
        logger.LogInformation("Bucket expiry service started. Cleanup interval: {Interval}", interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredBucketsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error during bucket cleanup");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task CleanupExpiredBucketsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var bucketRepo = scope.ServiceProvider.GetRequiredService<IBucketRepository>();
        var fileRepo = scope.ServiceProvider.GetRequiredService<IFileHeaderRepository>();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();

        var expired = await bucketRepo.FindExpiredAsync(DateTimeOffset.UtcNow, ct);
        if (expired.Count == 0) return;

        logger.LogInformation("Found {Count} expired buckets to clean up", expired.Count);

        foreach (var bucket in expired)
        {
            logger.LogInformation("Deleting expired bucket {BucketId} ({Name})", bucket.Id, bucket.Name);
            await fileRepo.DeleteByBucketAsync(bucket.Id, ct);
            await storage.DeleteBucketFilesAsync(bucket.Id, ct);
            await bucketRepo.DeleteAsync(bucket, ct);
        }
    }
}
```

**Step 2: Verify build**

```bash
dotnet build
```

**Step 3: Commit**

```bash
git add src/ClawdFiles.Infrastructure/Services/BucketExpiryService.cs
git commit -m "feat: add BucketExpiryService background service for cleaning up expired buckets"
```

---

### Task 14: Infrastructure — DependencyInjection

**Files:**
- Create: `src/ClawdFiles.Infrastructure/DependencyInjection.cs`

**Step 1: Create the DI extension**

```csharp
// src/ClawdFiles.Infrastructure/DependencyInjection.cs
using ClawdFiles.Application.Interfaces;
using ClawdFiles.Infrastructure.Data;
using ClawdFiles.Infrastructure.Repositories;
using ClawdFiles.Infrastructure.Security;
using ClawdFiles.Infrastructure.Services;
using ClawdFiles.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClawdFiles.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<ClawdFilesDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped<IApiKeyRepository, EfApiKeyRepository>();
        services.AddScoped<IBucketRepository, EfBucketRepository>();
        services.AddScoped<IFileHeaderRepository, EfFileHeaderRepository>();

        // File storage
        var storagePath = configuration.GetValue<string>("Storage:RootPath") ?? "./storage";
        services.AddSingleton<IFileStorage>(new LocalFileStorage(storagePath));

        // Security
        services.AddSingleton<IApiKeyHasher, Sha256ApiKeyHasher>();

        // Short codes
        services.AddScoped<IShortCodeGenerator, RandomShortCodeGenerator>();

        // Background services
        services.Configure<ExpiryOptions>(configuration.GetSection("Expiry"));
        services.AddHostedService<BucketExpiryService>();

        return services;
    }
}
```

**Step 2: Verify build**

```bash
dotnet build
```

**Step 3: Commit**

```bash
git add src/ClawdFiles.Infrastructure/DependencyInjection.cs
git commit -m "feat: add infrastructure DI registration extension method"
```

---

### Task 15: Web — Program.cs and Configuration

**Files:**
- Modify: `src/ClawdFiles.Web/Program.cs`
- Modify: `src/ClawdFiles.Web/appsettings.json`

**Step 1: Configure appsettings.json**

Replace the default appsettings.json content with:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=clawdfiles.db"
  },
  "AdminApiKey": "change-me-in-production",
  "Storage": {
    "RootPath": "./storage"
  },
  "Expiry": {
    "CleanupIntervalMinutes": 5
  }
}
```

**Step 2: Configure Program.cs**

Replace the default Program.cs with:

```csharp
using ClawdFiles.Application.Services;
using ClawdFiles.Infrastructure;
using ClawdFiles.Infrastructure.Data;
using ClawdFiles.Web.Components;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add infrastructure (DbContext, repositories, storage, background services)
builder.Services.AddInfrastructure(builder.Configuration);

// Add application services
builder.Services.AddScoped<KeyManagementService>();
builder.Services.AddScoped<BucketService>();
builder.Services.AddScoped<FileService>();
builder.Services.AddScoped<BucketSummaryService>();

// Add API controllers
builder.Services.AddControllers();

// Add Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Auto-migrate database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ClawdFilesDbContext>();
    await db.Database.MigrateAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

// Map API controllers before Blazor to avoid route conflicts
app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
```

**Step 3: Create initial EF Core migration**

```bash
dotnet ef migrations add InitialCreate --project src/ClawdFiles.Infrastructure --startup-project src/ClawdFiles.Web
```

**Step 4: Verify the app starts**

```bash
cd /home/carbon/clawd-files-rewrite && dotnet run --project src/ClawdFiles.Web &
sleep 5
curl -s http://localhost:5000 || curl -s http://localhost:5198 || echo "Check port"
kill %1 2>/dev/null
```

Expected: App starts without errors (may return HTML or a 200 response).

**Step 5: Commit**

```bash
git add src/ClawdFiles.Web/ src/ClawdFiles.Infrastructure/Data/Migrations/
git commit -m "feat: configure Program.cs with DI, migrations, and Blazor + API routing"
```

---

### Task 16: Web — API Key Authentication Handler

**Files:**
- Create: `src/ClawdFiles.Web/Authentication/ApiKeyAuthenticationHandler.cs`
- Modify: `src/ClawdFiles.Web/Program.cs` (add authentication registration)

**Step 1: Implement the authentication handler**

```csharp
// src/ClawdFiles.Web/Authentication/ApiKeyAuthenticationHandler.cs
using System.Security.Claims;
using System.Text.Encodings.Web;
using ClawdFiles.Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ClawdFiles.Web.Authentication;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string SchemeName = "ApiKey";
}

public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IApiKeyRepository keyRepo,
    IApiKeyHasher hasher,
    IConfiguration configuration)
    : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
            return AuthenticateResult.NoResult();

        var headerValue = authHeader.ToString();
        if (!headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var rawKey = headerValue["Bearer ".Length..].Trim();
        if (string.IsNullOrEmpty(rawKey))
            return AuthenticateResult.Fail("Empty API key");

        var adminKey = configuration.GetValue<string>("AdminApiKey");
        var isAdmin = !string.IsNullOrEmpty(adminKey) && rawKey == adminKey;

        var hash = hasher.Hash(rawKey);
        var apiKey = await keyRepo.FindByHashAsync(hash);

        if (apiKey is null && !isAdmin)
            return AuthenticateResult.Fail("Invalid API key");

        var claims = new List<Claim>
        {
            new("api_key_prefix", apiKey?.Prefix ?? "admin"),
        };

        if (apiKey is not null)
        {
            claims.Add(new Claim("api_key_id", apiKey.Id.ToString()));
            apiKey.LastUsedAt = DateTimeOffset.UtcNow;
            await keyRepo.UpdateAsync(apiKey);
        }

        if (isAdmin)
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
```

**Step 2: Register authentication in Program.cs**

Add before `builder.Services.AddControllers()`:

```csharp
using ClawdFiles.Web.Authentication;

// Add authentication
builder.Services.AddAuthentication(ApiKeyAuthenticationOptions.SchemeName)
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationOptions.SchemeName, _ => { });
builder.Services.AddAuthorization();
```

Add after `app.UseAntiforgery()`:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

**Step 3: Verify build**

```bash
dotnet build
```

**Step 4: Commit**

```bash
git add src/ClawdFiles.Web/
git commit -m "feat: add API key authentication handler with admin key support from config"
```

---

### Task 17: Web — API Controllers (Keys, Buckets, Files)

**Files:**
- Create: `src/ClawdFiles.Web/Controllers/KeysController.cs`
- Create: `src/ClawdFiles.Web/Controllers/BucketsController.cs`
- Create: `src/ClawdFiles.Web/Controllers/FilesController.cs`

**Step 1: Implement KeysController**

```csharp
// src/ClawdFiles.Web/Controllers/KeysController.cs
using ClawdFiles.Application.DTOs;
using ClawdFiles.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClawdFiles.Web.Controllers;

[ApiController]
[Route("api/keys")]
[Authorize(Roles = "Admin")]
public class KeysController(KeyManagementService keyService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateKeyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new ErrorResponse("name is required", "Provide a name for the API key"));

        var result = await keyService.CreateKeyAsync(request);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var keys = await keyService.ListKeysAsync();
        return Ok(keys);
    }

    [HttpDelete("{prefix}")]
    public async Task<IActionResult> Revoke(string prefix)
    {
        var result = await keyService.RevokeKeyAsync(prefix);
        if (!result) return NotFound(new ErrorResponse("Key not found", $"No key with prefix '{prefix}'"));
        return Ok(new { message = "Key revoked" });
    }
}
```

**Step 2: Implement BucketsController**

```csharp
// src/ClawdFiles.Web/Controllers/BucketsController.cs
using System.IO.Compression;
using System.Security.Claims;
using ClawdFiles.Application.DTOs;
using ClawdFiles.Application.Interfaces;
using ClawdFiles.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClawdFiles.Web.Controllers;

[ApiController]
[Route("api/buckets")]
public class BucketsController(
    BucketService bucketService,
    BucketSummaryService summaryService,
    IFileHeaderRepository fileRepo,
    IFileStorage storage) : ControllerBase
{
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateBucketRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new ErrorResponse("name is required", "Provide a bucket name"));

        var ownerId = GetCallerId();
        if (ownerId is null) return Unauthorized(new ErrorResponse("Invalid authentication"));

        var result = await bucketService.CreateBucketAsync(request, ownerId.Value);
        return Ok(result);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> List()
    {
        var isAdmin = User.IsInRole("Admin");
        var ownerId = GetCallerId();
        var result = await bucketService.ListBucketsAsync(ownerId, isAdmin);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(string id)
    {
        var result = await bucketService.GetBucketAsync(id);
        if (result is null) return NotFound(new ErrorResponse("Bucket not found"));
        return Ok(result);
    }

    [HttpPatch("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateBucketRequest request)
    {
        var ownerId = GetCallerId();
        if (ownerId is null) return Unauthorized(new ErrorResponse("Invalid authentication"));

        var isAdmin = User.IsInRole("Admin");
        var result = await bucketService.UpdateBucketAsync(id, request, ownerId.Value, isAdmin);
        if (result is null) return NotFound(new ErrorResponse("Bucket not found or access denied"));
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(string id)
    {
        var ownerId = GetCallerId();
        if (ownerId is null) return Unauthorized(new ErrorResponse("Invalid authentication"));

        var isAdmin = User.IsInRole("Admin");
        var result = await bucketService.DeleteBucketAsync(id, ownerId.Value, isAdmin);
        if (!result) return NotFound(new ErrorResponse("Bucket not found or access denied"));
        return Ok(new { message = "Bucket deleted" });
    }

    [HttpGet("{id}/summary")]
    [AllowAnonymous]
    public async Task<IActionResult> Summary(string id)
    {
        var summary = await summaryService.GetSummaryAsync(id);
        if (summary is null) return NotFound(new ErrorResponse("Bucket not found"));
        return Content(summary, "text/plain");
    }

    [HttpGet("{id}/zip")]
    [AllowAnonymous]
    public async Task<IActionResult> Zip(string id)
    {
        var bucket = await bucketService.GetBucketAsync(id);
        if (bucket is null) return NotFound(new ErrorResponse("Bucket not found"));

        var files = await fileRepo.ListByBucketAsync(id);
        var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in files)
            {
                var entry = archive.CreateEntry(file.Path, CompressionLevel.Fastest);
                await using var entryStream = entry.Open();
                var fileStream = await storage.GetFileStreamAsync(id, file.Path);
                if (fileStream is not null)
                {
                    await using (fileStream)
                    {
                        await fileStream.CopyToAsync(entryStream);
                    }
                }
            }
        }

        memoryStream.Position = 0;
        return File(memoryStream, "application/zip", $"{id}.zip");
    }

    private Guid? GetCallerId()
    {
        var idClaim = User.FindFirst("api_key_id")?.Value;
        return idClaim is not null && Guid.TryParse(idClaim, out var id) ? id : null;
    }
}
```

**Step 3: Implement FilesController**

```csharp
// src/ClawdFiles.Web/Controllers/FilesController.cs
using ClawdFiles.Application.DTOs;
using ClawdFiles.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace ClawdFiles.Web.Controllers;

[ApiController]
[Route("api/buckets/{bucketId}")]
public class FilesController(FileService fileService) : ControllerBase
{
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

    [HttpPost("upload")]
    [Authorize]
    [RequestSizeLimit(100_000_000)] // 100 MB
    public async Task<IActionResult> Upload(string bucketId)
    {
        if (!Request.HasFormContentType)
            return BadRequest(new ErrorResponse("Expected multipart/form-data"));

        var form = await Request.ReadFormAsync();
        var uploaded = new List<FileHeaderResponse>();

        foreach (var file in form.Files)
        {
            var fileName = !string.IsNullOrEmpty(file.FileName) ? file.FileName : file.Name;
            if (!ContentTypeProvider.TryGetContentType(fileName, out var contentType))
                contentType = file.ContentType ?? "application/octet-stream";

            using var stream = file.OpenReadStream();
            var result = await fileService.UploadFileAsync(bucketId, fileName, contentType, stream);
            if (result is null)
                return NotFound(new ErrorResponse("Bucket not found", $"No bucket with id '{bucketId}'"));

            uploaded.Add(result);
        }

        return Ok(new UploadResponse(uploaded));
    }

    [HttpDelete("files")]
    [Authorize]
    public async Task<IActionResult> Delete(string bucketId, [FromBody] DeleteFileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Path))
            return BadRequest(new ErrorResponse("path is required"));

        var result = await fileService.DeleteFileAsync(bucketId, request.Path);
        if (!result) return NotFound(new ErrorResponse("File not found"));
        return Ok(new { message = "File deleted" });
    }
}
```

**Step 4: Verify build**

```bash
dotnet build
```

**Step 5: Commit**

```bash
git add src/ClawdFiles.Web/Controllers/
git commit -m "feat: add API controllers for keys, buckets, and files"
```

---

### Task 18: Web — Raw File and Short URL Controllers

**Files:**
- Create: `src/ClawdFiles.Web/Controllers/RawController.cs`
- Create: `src/ClawdFiles.Web/Controllers/ShortUrlController.cs`
- Create: `src/ClawdFiles.Web/Controllers/DocsController.cs`

**Step 1: Implement RawController**

```csharp
// src/ClawdFiles.Web/Controllers/RawController.cs
using ClawdFiles.Application.DTOs;
using ClawdFiles.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClawdFiles.Web.Controllers;

[ApiController]
public class RawController(IFileStorage storage, IFileHeaderRepository fileRepo) : ControllerBase
{
    [HttpGet("raw/{bucketId}/{**filePath}")]
    public async Task<IActionResult> GetRaw(string bucketId, string filePath)
    {
        var header = await fileRepo.FindByBucketAndPathAsync(bucketId, filePath);
        if (header is null) return NotFound(new ErrorResponse("File not found"));

        var stream = await storage.GetFileStreamAsync(bucketId, filePath);
        if (stream is null) return NotFound(new ErrorResponse("File not found on disk"));

        return File(stream, header.ContentType, enableRangeProcessing: true);
    }
}
```

**Step 2: Implement ShortUrlController**

```csharp
// src/ClawdFiles.Web/Controllers/ShortUrlController.cs
using ClawdFiles.Application.DTOs;
using ClawdFiles.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClawdFiles.Web.Controllers;

[ApiController]
public class ShortUrlController(FileService fileService) : ControllerBase
{
    [HttpGet("s/{shortCode}")]
    public async Task<IActionResult> Redirect(string shortCode)
    {
        var header = await fileService.ResolveShortCodeAsync(shortCode);
        if (header is null) return NotFound(new ErrorResponse("Short URL not found"));

        var rawUrl = $"/raw/{header.BucketId}/{header.Path}";
        return RedirectPreserveMethod(rawUrl);
    }
}
```

**Step 3: Implement DocsController**

```csharp
// src/ClawdFiles.Web/Controllers/DocsController.cs
using Microsoft.AspNetCore.Mvc;

namespace ClawdFiles.Web.Controllers;

[ApiController]
public class DocsController : ControllerBase
{
    private const string LlmsTxt = """
        # Clawd Files API

        ## Overview
        Clawd Files is a file-sharing platform with bucket-based organization, API key authentication, and LLM-friendly endpoints.

        ## Authentication
        Write operations require: Authorization: Bearer <api_key>
        Admin endpoints need the admin key. Public read access requires no authentication.

        ## Endpoints
        - POST /api/keys - Create API key (admin)
        - GET /api/keys - List keys (admin)
        - DELETE /api/keys/{prefix} - Revoke key (admin)
        - POST /api/buckets - Create bucket
        - GET /api/buckets - List buckets
        - GET /api/buckets/{id} - Get bucket details (public)
        - PATCH /api/buckets/{id} - Update bucket
        - DELETE /api/buckets/{id} - Delete bucket
        - POST /api/buckets/{id}/upload - Upload files
        - DELETE /api/buckets/{id}/files - Delete file
        - GET /raw/{bucket_id}/{file_path} - Download raw file (public)
        - GET /api/buckets/{id}/zip - Download ZIP (public)
        - GET /api/buckets/{id}/summary - LLM-friendly summary (public)
        - GET /s/{short_code} - Short URL redirect (public)
        """;

    [HttpGet("llms.txt")]
    public IActionResult GetLlmsTxt()
    {
        return Content(LlmsTxt, "text/plain");
    }

    [HttpGet("docs/api.md")]
    public IActionResult GetApiDocs()
    {
        // For now, return the same content as llms.txt in markdown format
        return Content(LlmsTxt, "text/markdown");
    }
}
```

**Step 4: Verify build**

```bash
dotnet build
```

**Step 5: Commit**

```bash
git add src/ClawdFiles.Web/Controllers/
git commit -m "feat: add raw file, short URL, and docs controllers"
```

---

### Task 19: Web — Blazor Layout and Home Page

**Files:**
- Modify: `src/ClawdFiles.Web/Components/App.razor` (should already exist from template)
- Create: `src/ClawdFiles.Web/Components/Layout/MainLayout.razor`
- Create: `src/ClawdFiles.Web/Components/Layout/NavMenu.razor`
- Create: `src/ClawdFiles.Web/Components/Pages/Home.razor`

**Step 1: Set up the layout and home page**

Review existing template files in `src/ClawdFiles.Web/Components/` first. Then create/update:

`MainLayout.razor`:
```razor
@inherits LayoutComponentBase

<div class="app">
    <NavMenu />
    <main class="content">
        @Body
    </main>
</div>
```

`NavMenu.razor`:
```razor
<nav class="navbar">
    <a href="/">Clawd Files</a>
    <a href="/admin">Admin</a>
</nav>
```

`Home.razor`:
```razor
@page "/"

<PageTitle>Clawd Files</PageTitle>

<h1>Clawd Files</h1>
<p>A file-sharing platform with bucket-based organization.</p>
```

**Step 2: Verify the app starts and renders the home page**

```bash
dotnet run --project src/ClawdFiles.Web &
sleep 5
curl -s http://localhost:5000 | head -20
kill %1 2>/dev/null
```

**Step 3: Commit**

```bash
git add src/ClawdFiles.Web/Components/
git commit -m "feat: add Blazor layout, navigation, and home page"
```

---

### Task 20: Web — Blazor Bucket View Page

**Files:**
- Create: `src/ClawdFiles.Web/Components/Pages/BucketView.razor`

**Step 1: Implement BucketView page**

```razor
@page "/{BucketId}"
@using ClawdFiles.Application.Services
@inject BucketService BucketService

<PageTitle>@(bucket?.Name ?? "Bucket")</PageTitle>

@if (bucket is null)
{
    <p>Loading...</p>
}
else
{
    <h1>@bucket.Name</h1>
    @if (!string.IsNullOrEmpty(bucket.Description))
    {
        <p>@bucket.Description</p>
    }

    <div class="bucket-meta">
        <span>ID: @bucket.Id</span>
        @if (!string.IsNullOrEmpty(bucket.Purpose))
        {
            <span>Purpose: @bucket.Purpose</span>
        }
        <span>Created: @bucket.CreatedAt.ToString("u")</span>
        @if (bucket.ExpiresAt.HasValue)
        {
            <span>Expires: @bucket.ExpiresAt.Value.ToString("u")</span>
        }
    </div>

    <h2>Files (@bucket.Files.Count)</h2>
    @if (bucket.Files.Count == 0)
    {
        <p>No files in this bucket.</p>
    }
    else
    {
        <table>
            <thead>
                <tr>
                    <th>Path</th>
                    <th>Type</th>
                    <th>Size</th>
                    <th>Actions</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var file in bucket.Files)
                {
                    <tr>
                        <td><a href="/@bucket.Id/@file.Path">@file.Path</a></td>
                        <td>@file.ContentType</td>
                        <td>@FormatSize(file.SizeBytes)</td>
                        <td><a href="/raw/@bucket.Id/@file.Path">Download</a></td>
                    </tr>
                }
            </tbody>
        </table>
        <p><a href="/api/buckets/@bucket.Id/zip">Download all as ZIP</a></p>
    }
}

@code {
    [Parameter] public string BucketId { get; set; } = "";

    private ClawdFiles.Application.DTOs.BucketResponse? bucket;

    protected override async Task OnInitializedAsync()
    {
        bucket = await BucketService.GetBucketAsync(BucketId);
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F1} GB"
    };
}
```

**Step 2: Verify build**

```bash
dotnet build
```

**Step 3: Commit**

```bash
git add src/ClawdFiles.Web/Components/Pages/BucketView.razor
git commit -m "feat: add Blazor BucketView page with file listing"
```

---

### Task 21: Web — Blazor File View Page

**Files:**
- Create: `src/ClawdFiles.Web/Components/Pages/FileView.razor`

**Step 1: Implement FileView page**

```razor
@page "/{BucketId}/{*FilePath}"
@using ClawdFiles.Application.Interfaces
@inject IFileHeaderRepository FileRepo

<PageTitle>@FilePath</PageTitle>

@if (fileHeader is null)
{
    <p>File not found.</p>
}
else
{
    <h1>@fileHeader.Path</h1>
    <div class="file-meta">
        <span>Type: @fileHeader.ContentType</span>
        <span>Size: @FormatSize(fileHeader.SizeBytes)</span>
        <span>Uploaded: @fileHeader.UploadedAt.ToString("u")</span>
        <span>Short URL: <a href="/s/@fileHeader.ShortCode">/s/@fileHeader.ShortCode</a></span>
    </div>

    @if (fileHeader.ContentType.StartsWith("image/"))
    {
        <img src="/raw/@BucketId/@FilePath" alt="@fileHeader.Path" style="max-width: 100%;" />
    }

    <p><a href="/raw/@BucketId/@FilePath">Download</a></p>
    <p><a href="/@BucketId">Back to bucket</a></p>
}

@code {
    [Parameter] public string BucketId { get; set; } = "";
    [Parameter] public string FilePath { get; set; } = "";

    private ClawdFiles.Domain.Entities.BucketFileHeader? fileHeader;

    protected override async Task OnInitializedAsync()
    {
        fileHeader = await FileRepo.FindByBucketAndPathAsync(BucketId, FilePath);
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F1} GB"
    };
}
```

**Step 2: Verify build**

```bash
dotnet build
```

**Step 3: Commit**

```bash
git add src/ClawdFiles.Web/Components/Pages/FileView.razor
git commit -m "feat: add Blazor FileView page with preview and download"
```

---

### Task 22: Web — Admin Dashboard Page

**Files:**
- Create: `src/ClawdFiles.Web/Components/Pages/Admin/Dashboard.razor`

**Step 1: Implement admin dashboard**

This is a placeholder admin page that displays key and bucket stats. Full admin interactivity (key creation forms, bucket management) can be built incrementally.

```razor
@page "/admin"
@using ClawdFiles.Application.Services
@inject KeyManagementService KeyService
@inject BucketService BucketService

<PageTitle>Admin Dashboard</PageTitle>

<h1>Admin Dashboard</h1>

<h2>API Keys</h2>
@if (keys is null)
{
    <p>Loading...</p>
}
else
{
    <table>
        <thead>
            <tr><th>Prefix</th><th>Name</th><th>Created</th><th>Last Used</th><th>Buckets</th></tr>
        </thead>
        <tbody>
            @foreach (var key in keys)
            {
                <tr>
                    <td>@key.Prefix</td>
                    <td>@key.Name</td>
                    <td>@key.CreatedAt.ToString("u")</td>
                    <td>@(key.LastUsedAt?.ToString("u") ?? "Never")</td>
                    <td>@key.BucketCount</td>
                </tr>
            }
        </tbody>
    </table>
}

<h2>All Buckets</h2>
@if (buckets is null)
{
    <p>Loading...</p>
}
else
{
    <table>
        <thead>
            <tr><th>ID</th><th>Name</th><th>Files</th><th>Created</th><th>Expires</th></tr>
        </thead>
        <tbody>
            @foreach (var b in buckets)
            {
                <tr>
                    <td><a href="/@b.Id">@b.Id</a></td>
                    <td>@b.Name</td>
                    <td>@b.FileCount</td>
                    <td>@b.CreatedAt.ToString("u")</td>
                    <td>@(b.ExpiresAt?.ToString("u") ?? "Never")</td>
                </tr>
            }
        </tbody>
    </table>
}

@code {
    private List<ClawdFiles.Application.DTOs.KeyInfoResponse>? keys;
    private List<ClawdFiles.Application.DTOs.BucketListItemResponse>? buckets;

    protected override async Task OnInitializedAsync()
    {
        keys = await KeyService.ListKeysAsync();
        // Admin sees all buckets — pass null ownerId, isAdmin = true
        buckets = await BucketService.ListBucketsAsync(null, isAdmin: true);
    }
}
```

**Step 2: Verify build**

```bash
dotnet build
```

**Step 3: Commit**

```bash
git add src/ClawdFiles.Web/Components/Pages/Admin/
git commit -m "feat: add admin dashboard page with key and bucket overview"
```

---

### Task 23: Integration Test — Full API Round-Trip

**Files:**
- Test: `tests/ClawdFiles.Tests/Integration/ApiIntegrationTests.cs`

**Step 1: Write integration test**

```csharp
// tests/ClawdFiles.Tests/Integration/ApiIntegrationTests.cs
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClawdFiles.Application.DTOs;
using ClawdFiles.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClawdFiles.Tests.Integration;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private const string AdminKey = "test-admin-key";

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        var customFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace DB with in-memory SQLite
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ClawdFilesDbContext>));
                if (descriptor is not null) services.Remove(descriptor);

                services.AddDbContext<ClawdFilesDbContext>(options =>
                    options.UseSqlite("Data Source=:memory:"));
            });
            builder.UseSetting("AdminApiKey", AdminKey);
        });
        _client = customFactory.CreateClient();
    }

    [Fact]
    public async Task CreateKey_ListKeys_RoundTrip()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AdminKey);

        var createResponse = await _client.PostAsJsonAsync("/api/keys", new { name = "test-key" });
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var listResponse = await _client.GetAsync("/api/keys");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_AdminEndpoint_Returns401()
    {
        var response = await _client.GetAsync("/api/keys");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetBucket_NotFound_Returns404()
    {
        var response = await _client.GetAsync("/api/buckets/nonexistent");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
```

Note: You may need to add `<InternalsVisibleTo Include="ClawdFiles.Tests" />` to the Web project, or expose `Program` as a partial class. A simple approach: add to the bottom of `Program.cs`:

```csharp
// Make Program accessible for integration tests
public partial class Program;
```

**Step 2: Run integration tests**

```bash
dotnet test tests/ClawdFiles.Tests --filter "FullyQualifiedName~ApiIntegrationTests" -v minimal
```

**Step 3: Commit**

```bash
git add tests/ClawdFiles.Tests/Integration/ src/ClawdFiles.Web/Program.cs
git commit -m "feat: add API integration tests for key management and error responses"
```

---

### Task 24: Final — Run All Tests and Verify

**Step 1: Run all tests**

```bash
dotnet test --verbosity minimal
```

Expected: All tests pass.

**Step 2: Start the app and do a manual smoke test**

```bash
dotnet run --project src/ClawdFiles.Web
```

In another terminal, test:
```bash
# Health check — home page
curl -s http://localhost:5000

# Create a key (admin)
curl -s -X POST http://localhost:5000/api/keys \
  -H "Authorization: Bearer change-me-in-production" \
  -H "Content-Type: application/json" \
  -d '{"name":"test"}'

# LLM docs
curl -s http://localhost:5000/llms.txt
```

**Step 3: Final commit**

```bash
git add -A
git commit -m "chore: final cleanup and verification"
```
