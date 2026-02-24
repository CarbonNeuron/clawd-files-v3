# Clawd Files Rewrite — Design Document

## Overview

Clawd Files is a file-sharing platform with bucket-based organization, API key authentication, and LLM-friendly endpoints. This is a rewrite using .NET 10, ASP.NET, and Blazor Server, inspired by the existing spec at https://clawd-files.plexus.video/llms.txt but not required to be API-identical.

## Key Decisions

| Decision | Choice |
|---|---|
| Architecture | Classic Clean Architecture (4 projects) |
| Database | SQLite via EF Core |
| File storage | Local filesystem |
| UI | Blazor Server — full public UI + admin dashboard |
| Expiry | Background hosted service |
| Admin key | From appsettings.json config (overridable via env var for Docker) |

## Domain Layer (`ClawdFiles.Domain`)

### Entities

**ApiKey**
- `Id` (Guid) — primary key
- `Name` (string) — human-readable label
- `KeyHash` (string) — SHA-256 hash of the key
- `Prefix` (string) — first 8 chars, used for display and lookup
- `CreatedAt` (DateTimeOffset)
- `LastUsedAt` (DateTimeOffset?)

**Bucket**
- `Id` (string) — short random ID (e.g., "aB3xK")
- `Name` (string)
- `Description` (string?) — optional
- `Purpose` (string?) — optional label
- `OwnerId` (Guid) — FK to ApiKey
- `CreatedAt` (DateTimeOffset)
- `ExpiresAt` (DateTimeOffset?) — null means never expires

**BucketFileHeader**
- `Id` (Guid) — primary key
- `BucketId` (string) — FK to Bucket
- `Path` (string) — relative path within the bucket
- `ContentType` (string)
- `SizeBytes` (long)
- `ShortCode` (string) — 6-char alphanumeric for short URLs
- `UploadedAt` (DateTimeOffset)

### Value Objects

**ExpiryPreset** — parses preset strings ("1h", "6h", "12h", "1d", "3d", "1w", "2w", "1m") into TimeSpan values. Also accepts raw seconds. Default is 7 days when omitted.

## Application Layer (`ClawdFiles.Application`)

### Interfaces

- `IApiKeyRepository` — CRUD for API keys, `FindByPrefix`, `FindByHash`
- `IBucketRepository` — CRUD for buckets, `ListByOwner`, `ListAll`, `FindExpired`
- `IFileHeaderRepository` — CRUD for file headers, `ListByBucket`, `FindByShortCode`
- `IFileStorage` — `SaveFileAsync`, `GetFileStreamAsync`, `DeleteFileAsync`, `DeleteBucketFilesAsync`; supports Range requests
- `IShortCodeGenerator` — generates unique short codes
- `IApiKeyHasher` — hash and verify API keys

### Services

- `KeyManagementService` — create/list/revoke keys; admin check compares key against config value
- `BucketService` — create/list/get/update/delete buckets; enforces ownership rules
- `FileService` — upload/delete files, resolve short URLs, generate short codes
- `BucketSummaryService` — generates LLM-friendly plain-text summaries of buckets

### DTOs

Request and response DTOs for each operation, decoupling API contracts from domain entities.

## Infrastructure Layer (`ClawdFiles.Infrastructure`)

### Data

- `ClawdFilesDbContext` — EF Core context with DbSets for ApiKey, Bucket, BucketFileHeader
- SQLite provider, connection string from config
- Migrations stored in this project

### Repositories

- `EfApiKeyRepository`, `EfBucketRepository`, `EfFileHeaderRepository` — EF Core implementations

### Storage

- `LocalFileStorage` — stores files at `{StorageRoot}/{bucketId}/{path}`, handles directory creation, stream I/O, and Range support via FileStream

### Security

- `Sha256ApiKeyHasher` — SHA-256 with salt for key storage and verification

### Background Services

- `BucketExpiryService` : `BackgroundService` — runs on configurable interval (default 5 min), deletes expired buckets and their files from DB and filesystem

### Other

- `RandomShortCodeGenerator` — 6-char alphanumeric codes with collision checking
- `DependencyInjection.cs` — extension method to register all infrastructure services

## Web Layer (`ClawdFiles.Web`)

### API Controllers

| Controller | Endpoints |
|---|---|
| `KeysController` | `POST /api/keys`, `GET /api/keys`, `DELETE /api/keys/{prefix}` |
| `BucketsController` | `POST /api/buckets`, `GET /api/buckets`, `GET /api/buckets/{id}`, `PATCH /api/buckets/{id}`, `DELETE /api/buckets/{id}`, `GET /api/buckets/{id}/summary`, `GET /api/buckets/{id}/zip` |
| `FilesController` | `POST /api/buckets/{id}/upload`, `DELETE /api/buckets/{id}/files` |
| `RawController` | `GET /raw/{bucketId}/{**filePath}` |
| `ShortUrlController` | `GET /s/{shortCode}` (307 redirect) |
| `DocsController` | `GET /llms.txt`, `GET /docs/api.md` |

### Authentication

- `ApiKeyAuthenticationHandler` — extracts Bearer token, hashes it, looks up key in DB
- Admin check: compares raw token against `AdminApiKey` config value
- Public endpoints (raw files, bucket view, short URLs, docs) require no auth

### Blazor Server Pages

| Route | Page | Auth |
|---|---|---|
| `/` | Home/landing | Public |
| `/{bucketId}` | Bucket view (files, metadata, downloads) | Public |
| `/{bucketId}/{filePath}` | File detail (preview, download) | Public |
| `/admin` | Admin dashboard | Admin |
| `/admin/buckets` | Bucket management | Admin |
| `/admin/buckets/{id}` | Bucket detail/edit | Admin |

### Configuration (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=clawdfiles.db"
  },
  "AdminApiKey": "your-admin-key-here",
  "Storage": {
    "RootPath": "./storage"
  },
  "Expiry": {
    "CleanupIntervalMinutes": 5
  }
}
```

Admin key can be overridden via environment variable `ClawdFiles__AdminApiKey` for Docker deployments.

## Solution Structure

```
ClawdFiles.sln
├── src/
│   ├── ClawdFiles.Domain/
│   │   ├── Entities/
│   │   │   ├── ApiKey.cs
│   │   │   ├── Bucket.cs
│   │   │   └── BucketFileHeader.cs
│   │   └── ValueObjects/
│   │       └── ExpiryPreset.cs
│   ├── ClawdFiles.Application/
│   │   ├── Interfaces/
│   │   │   ├── IApiKeyRepository.cs
│   │   │   ├── IBucketRepository.cs
│   │   │   ├── IFileHeaderRepository.cs
│   │   │   ├── IFileStorage.cs
│   │   │   ├── IShortCodeGenerator.cs
│   │   │   └── IApiKeyHasher.cs
│   │   ├── Services/
│   │   │   ├── KeyManagementService.cs
│   │   │   ├── BucketService.cs
│   │   │   ├── FileService.cs
│   │   │   └── BucketSummaryService.cs
│   │   └── DTOs/
│   ├── ClawdFiles.Infrastructure/
│   │   ├── Data/
│   │   │   ├── ClawdFilesDbContext.cs
│   │   │   └── Migrations/
│   │   ├── Repositories/
│   │   │   ├── EfApiKeyRepository.cs
│   │   │   ├── EfBucketRepository.cs
│   │   │   └── EfFileHeaderRepository.cs
│   │   ├── Storage/
│   │   │   └── LocalFileStorage.cs
│   │   ├── Security/
│   │   │   └── Sha256ApiKeyHasher.cs
│   │   ├── Services/
│   │   │   ├── BucketExpiryService.cs
│   │   │   └── RandomShortCodeGenerator.cs
│   │   └── DependencyInjection.cs
│   └── ClawdFiles.Web/
│       ├── Controllers/
│       │   ├── KeysController.cs
│       │   ├── BucketsController.cs
│       │   ├── FilesController.cs
│       │   ├── RawController.cs
│       │   ├── ShortUrlController.cs
│       │   └── DocsController.cs
│       ├── Authentication/
│       │   └── ApiKeyAuthenticationHandler.cs
│       ├── Components/
│       │   ├── Layout/
│       │   ├── Pages/
│       │   │   ├── Home.razor
│       │   │   ├── BucketView.razor
│       │   │   ├── FileView.razor
│       │   │   └── Admin/
│       │   │       ├── Dashboard.razor
│       │   │       ├── BucketList.razor
│       │   │       └── BucketDetail.razor
│       │   └── App.razor
│       ├── Program.cs
│       └── appsettings.json
└── tests/
    └── ClawdFiles.Tests/
```

## Error Handling

All API errors return JSON with `error` and `hint` fields, matching the spec convention. Common status codes: 400, 401, 403, 404.

## Future Considerations (not in scope for initial build)

- Docker/docker-compose setup
- S3-compatible storage backend
- Rate limiting
- File size limits configuration
