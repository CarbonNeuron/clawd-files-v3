# Clawd Files

A file-sharing platform with bucket-based organization, API key authentication, and LLM-friendly endpoints. Built with ASP.NET Core 10 and SQLite.

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    ClawdFiles.Web                       │
│  Controllers · Authentication · Blazor UI · Program.cs  │
├─────────────────────────────────────────────────────────┤
│                 ClawdFiles.Application                  │
│       Services · DTOs · Interfaces (contracts)          │
├─────────────────────────────────────────────────────────┤
│                ClawdFiles.Infrastructure                │
│   EF Core Repos · SQLite · Local Storage · Expiry Job   │
├─────────────────────────────────────────────────────────┤
│                   ClawdFiles.Domain                     │
│              Entities · Value Objects                    │
└─────────────────────────────────────────────────────────┘
```

Four-layer clean architecture: **Domain** (entities, value objects) → **Application** (services, DTOs, interface contracts) → **Infrastructure** (EF Core repos, SQLite, local file storage, background expiry service) → **Web** (controllers, auth handler, Blazor UI).

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Framework | ASP.NET Core 10 (.NET 10) |
| Database | SQLite via Entity Framework Core 10 |
| File Storage | Local filesystem |
| Auth | Custom Bearer token scheme (SHA-256 hashed API keys) |
| UI | Blazor Server |
| Tests | xUnit + Moq + WebApplicationFactory |

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Build and Run

```bash
dotnet restore
dotnet build
dotnet run --project src/ClawdFiles.Web
```

The app starts on `http://localhost:5000` by default. On first run it creates `clawdfiles.db` and a `storage/` directory in the working directory.

### Run Tests

```bash
dotnet test
```

## Configuration

All settings can be overridden via environment variables using the `__` separator (e.g., `ConnectionStrings__DefaultConnection`).

| Setting | Default | Description |
|---------|---------|-------------|
| `ConnectionStrings:DefaultConnection` | `Data Source=clawdfiles.db` | SQLite connection string |
| `AdminApiKey` | `change-me-in-production` | Master admin API key |
| `Storage:RootPath` | `./storage` | Root directory for uploaded files |
| `Expiry:CleanupIntervalMinutes` | `5` | How often the background service checks for expired buckets |

## API Endpoints

All write operations require `Authorization: Bearer <api_key>`. Public read endpoints need no authentication.

### Keys (Admin only)

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/keys` | Create a new API key |
| `GET` | `/api/keys` | List all API keys |
| `DELETE` | `/api/keys/{prefix}` | Revoke an API key by prefix |

### Buckets

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `POST` | `/api/buckets` | Required | Create a bucket |
| `GET` | `/api/buckets` | Required | List your buckets (admin sees all) |
| `GET` | `/api/buckets/{id}` | Public | Get bucket details with file list |
| `PATCH` | `/api/buckets/{id}` | Required | Update bucket name/description/purpose |
| `DELETE` | `/api/buckets/{id}` | Required | Delete bucket and all its files |
| `GET` | `/api/buckets/{id}/summary` | Public | LLM-friendly plain-text summary |
| `GET` | `/api/buckets/{id}/zip` | Public | Download all files as ZIP |

### Files

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `POST` | `/api/buckets/{id}/upload` | Required | Upload files (multipart, max 100 MB) |
| `DELETE` | `/api/buckets/{id}/files` | Required | Delete a file by path |

### Raw & Short URLs

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/raw/{bucketId}/{filePath}` | Download raw file (supports Range requests) |
| `GET` | `/s/{shortCode}` | Redirect to file via 6-char short code |

### Documentation

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/llms.txt` | API overview for LLMs (plain text) |
| `GET` | `/docs/api.md` | API overview (markdown) |

## Docker

### Build

```bash
docker build -t clawd-files .
```

### Run

The container stores its SQLite database and uploaded files in `/app/data/`. Mount a volume to persist data:

```bash
docker run -d \
  -p 8080:8080 \
  -v clawd-files-data:/app/data \
  -e ClawdFiles__AdminApiKey=your-secret-key \
  clawd-files
```

### Docker Compose

```yaml
services:
  clawd-files:
    image: ghcr.io/carbonneuron/clawd-files-v3:latest
    ports:
      - "8080:8080"
    volumes:
      - clawd-files-data:/app/data
    environment:
      - ClawdFiles__AdminApiKey=your-secret-key

volumes:
  clawd-files-data:
```
