# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Run

```bash
dotnet restore
dotnet build
dotnet run --project src/ClawdFiles.Web    # starts on http://localhost:5000
dotnet test                                 # run all tests
dotnet test --filter FullyQualifiedName~KeyManagementServiceTests   # run a single test class
dotnet test --filter FullyQualifiedName~CreateKey_ListKeys_RoundTrip  # run a single test method
```

.NET 10 SDK required. Solution file is `ClawdFiles.slnx` (XML-based format).

## Architecture

Four-layer clean architecture with strict dependency direction:

**Domain** (`src/ClawdFiles.Domain/`) — Entities (`ApiKey`, `Bucket`, `BucketFileHeader`) and value objects (`ExpiryPreset`). No dependencies on other projects.

**Application** (`src/ClawdFiles.Application/`) — Business logic services (`KeyManagementService`, `BucketService`, `FileService`, `BucketSummaryService`), DTOs (record types in `*Dtos.cs`), and interface contracts (`IApiKeyRepository`, `IBucketRepository`, `IFileHeaderRepository`, `IFileStorage`, `IApiKeyHasher`, `IShortCodeGenerator`). Depends only on Domain.

**Infrastructure** (`src/ClawdFiles.Infrastructure/`) — Implements Application interfaces: EF Core repositories (`EfApiKeyRepository`, etc.), `LocalFileStorage`, `Sha256ApiKeyHasher`, `RandomShortCodeGenerator`. Contains `ClawdFilesDbContext` with Fluent API configurations in `Data/Configurations/`. Background `BucketExpiryService` cleans up expired buckets. All DI registration happens in `DependencyInjection.cs` via `AddInfrastructure()` extension method.

**Web** (`src/ClawdFiles.Web/`) — ASP.NET Core host. REST controllers under `Controllers/`, custom `ApiKeyAuthenticationHandler` for Bearer token auth, Blazor Server UI under `Components/` using MudBlazor. Application services registered as scoped in `Program.cs`.

## Key Patterns

- **Repository pattern**: all data access through interfaces defined in Application, implemented in Infrastructure
- **DTOs as records**: API request/response types are C# records in `Application/DTOs/`
- **Custom auth**: `ApiKeyAuthenticationHandler` validates Bearer tokens against SHA-256 hashed keys; admin key set via `AdminApiKey` config; claims include `api_key_prefix`, `api_key_id`, and `Role`
- **Database**: SQLite via EF Core; auto-creates in Development, runs migrations in Production (`Program.cs:48-55`)
- **File storage**: files stored at `{Storage:RootPath}/{bucketId}/{filePath}` on local filesystem
- **Blazor components**: pages in `Components/Pages/`, shared components in `Components/Shared/` (e.g., `CodePreview.razor` for syntax highlighting via TextMateSharp)

## Testing

Tests in `tests/ClawdFiles.Tests/` using xUnit + Moq:

- `Application/` — Unit tests for services with mocked repositories
- `Infrastructure/` — Repository and storage tests (in-memory SQLite, temp directories)
- `Integration/` — Full-stack API tests using `WebApplicationFactory<Program>` with in-memory SQLite (shared connection pattern to persist data within test)
- `Domain/` — Entity and value object tests

Integration tests swap the DbContext to use an in-memory SQLite connection and override `AdminApiKey` via `UseSetting`.

## Configuration

Key settings in `appsettings.json` (override via env vars with `__` separator):

| Setting | Default |
|---------|---------|
| `ConnectionStrings:DefaultConnection` | `Data Source=clawdfiles.db` |
| `AdminApiKey` | `change-me-in-production` |
| `Storage:RootPath` | `./storage` |
| `Expiry:CleanupIntervalMinutes` | `5` |

## CI

GitHub Actions runs on push/PR to main: restore → build → test (`.github/workflows/test.yml`). Docker images published to `ghcr.io/carbonneuron/clawd-files-v3` via `.github/workflows/publish.yml`.
