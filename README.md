# PastebinApp

**PastebinApp** is a Pastebin-style HTTP API built with ASP.NET Core. It lets you create, read, and delete short-lived text pastes. Each paste gets a unique short URL (Base62), optional title and language, configurable expiration (1+ hours), and a view counter. Metadata is stored in PostgreSQL, paste content in MinIO (S3-compatible), and Redis is used for caching and for buffering view-count updates before periodic flush to the database.

---

## Features

- **Create pastes** — `POST /api/pastes` with content, expiration (hours), optional title/language (max 512 KB).
- **Read pastes** — `GET /api/pastes/{hash}` returns content, metadata, and increments view count.
- **Delete pastes** — `DELETE /api/pastes/{hash}` removes paste from DB, cache, and blob storage.
- **Short URLs** — Pre-generated Base62 hashes (8 chars) from a Redis-backed pool, refilled in the background.
- **View counts** — Atomic Redis `INCR` per request; a background job flushes deltas to PostgreSQL in batches and invalidates paste cache so responses stay consistent.
- **Expiration** — Pastes expire after the chosen TTL; a cleanup job removes expired records from DB, cache, and MinIO.
- **Validation** — FluentValidation for `CreatePaste`; global exception middleware maps domain errors to 404/410/400/500.

---

## Tech stack

| Layer | Technology |
|-------|------------|
| API | ASP.NET Core 8, Swagger/OpenAPI, FluentValidation |
| Application | CQRS-style services, DTOs, interfaces |
| Domain | Entities (`Paste`, `PasteHash`), domain exceptions |
| Infrastructure | EF Core (PostgreSQL), Redis (cache + hash pool + view buffer), MinIO (S3-compatible blob storage) |
| DevOps | Docker Compose (API, Postgres, Redis, MinIO, pgAdmin, Redis Commander) |

---

## Prerequisites

- **.NET 8 SDK**
- **Docker & Docker Compose** (for running dependencies or full stack)

---

## Quick start

### Option 1: Docker Compose (full stack)

From the repository root:

```bash
docker compose up -d
```

- **API**: http://localhost:5000  
- **Swagger**: http://localhost:5000/swagger  
- **Health**: http://localhost:5000/health  
- **PostgreSQL**: `localhost:5432` (user `pastebin_user`, DB `pastebindb`)  
- **Redis**: `localhost:6379`  
- **MinIO**: API `localhost:9000`, Console http://localhost:9001  
- **pgAdmin**: http://localhost:5050 (admin@example.com / admin)  
- **Redis Commander**: http://localhost:8081  

Ensure MinIO is up before creating pastes (it stores paste content). The API container uses `postgres` and `redis` hostnames; MinIO is `minio:9000` inside the Docker network.

### Option 2: Local API + Docker dependencies

1. Start only Postgres, Redis, and MinIO:

   ```bash
   docker compose up -d postgres redis minio
   ```

2. Set `appsettings.Development.json` (or env) with `DefaultConnection`, `Redis`, and `MinIO` (e.g. `localhost:9000`).

3. Run migrations:

   ```bash
   dotnet ef database update --project src/PastebinApp.Infrastructure --startup-project src/PastebinApp.Api
   ```

4. Run the API:

   ```bash
   dotnet run --project src/PastebinApp.Api
   ```

---

## API overview

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/` | API info and links to `/health`, `/swagger`, `/api/pastes`. |
| `GET` | `/health` | Health check (status, timestamp, environment). |
| `POST` | `/api/pastes` | Create paste. Body: `content`, `expirationHours` (≥1), optional `title`, `language`. Returns `hash`, `url`, `createdAt`, `expiresAt`, `expiresInSeconds`. |
| `GET` | `/api/pastes/{hash}` | Get paste by hash. Returns content, metadata, `viewCount`, `expiresInSeconds`. 404 if not found, 410 if expired. |
| `DELETE` | `/api/pastes/{hash}` | Delete paste. 204 on success, 404 if not found. |

Validation errors return `400` with a `ValidationErrorResponse` (field + message). Domain and unexpected errors are handled by middleware and return JSON `ErrorResponse` with `error` and optional `details`.

---

## Configuration

Key sections in `appsettings.json` / environment:

| Section | Keys | Description |
|---------|------|-------------|
| `ConnectionStrings` | `DefaultConnection`, `Redis` | PostgreSQL and Redis. |
| `MinIO` | `Endpoint`, `AccessKey`, `SecretKey`, `UseSSL`, `BucketName` | MinIO / S3-compatible storage. |
| `HashPool` | `Key`, `MinPoolSize`, `RefillBatchSize`, `CheckIntervalSeconds` | Hash pool in Redis and refill job. |
| `Cache` | `KeyPrefix`, `ViewCountPrefix`, `ViewCountDirtySetKey`, `ViewCountPendingPrefix`, `ViewCountSlidingExpirationHours` | Cache key prefixes and view-count buffer. |
| `ViewCountFlush` | `IntervalSeconds`, `BatchSize`, `PendingTtlMinutes` | Flush job interval, batch size, and pending TTL. |
| `Cleanup` | `IntervalMinutes`, `BatchSize` | Expired-paste cleanup interval and batch size. |

---

## Project structure

```
PastebinApp/
├── src/
│   ├── PastebinApp.Api/           # HTTP API, controllers, middleware, mapping
│   ├── PastebinApp.Application/   # Services, DTOs, validators, interfaces
│   ├── PastebinApp.Domain/        # Entities, domain exceptions
│   └── PastebinApp.Infrastructure/# EF Core, Redis, MinIO, repos, background jobs
├── tests/
│   └── PastebinApp.Tests/
├── docker-compose.yaml
└── README.md
```

---

## Running tests

```bash
dotnet test
```

---

## License

MIT (or your preferred license).
