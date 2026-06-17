# EvalCore Class Service

ASP.NET Core Class Service for the Automated Grading System. It manages classes, student membership, lab metadata, presigned MinIO/S3 lab asset URLs, and lab domain events.

## Architecture

The service follows a pragmatic layered flow:

`Controller -> Application Service -> Repository Interface -> Repository Implementation -> EF Core DbContext -> PostgreSQL`

Projects:

- `src/Class.Api`: controllers, auth, Swagger, CORS, error envelope, health endpoint.
- `src/Class.Application`: DTOs, requests, service interfaces/implementations, pagination, result models, current user and storage abstractions.
- `src/Class.Domain`: entities and role/status constants only.
- `src/Class.Infrastructure`: EF Core DbContext, repositories, migrations, S3 presign service, RabbitMQ outbox publisher, development seeder.
- `tests/Class.Tests`: application service tests with fake repositories, fake S3, and fake outbox.

## Requirements

- .NET 8 SDK
- PostgreSQL from `prn232-ops`
- MinIO from `prn232-ops`
- RabbitMQ from `prn232-ops`
- Docker for image builds

This service does not call Identity Service directly. It trusts validated JWT claims:

- `sub`: account id
- `email`: email
- `role`: `student`, `lecturer`, or `admin`
- `fullName`: optional full name

## Configuration

Copy `.env.example` to a local `.env` if needed. `.env` is ignored by git.

Key variables:

```env
DATABASE_URL=Host=localhost;Port=5432;Database=ags;Username=ags;Password=ags_password
JWT_SECRET=change-me-same-secret-as-identity
JWT_ISSUER=ags
JWT_AUDIENCE=ags-api
CORS_ALLOWED_ORIGINS=http://localhost:3000,http://localhost:5173
S3_INTERNAL_ENDPOINT=http://localhost:9000
S3_PUBLIC_ENDPOINT=http://localhost:9000
S3_ACCESS_KEY=ags
S3_SECRET_KEY=ags_password
S3_USE_SSL=false
LAB_ASSETS_BUCKET=lab-assets
PRESIGNED_URL_EXPIRES_MINUTES=15
RABBITMQ_HOST=localhost
RABBITMQ_PORT=5672
RABBITMQ_USERNAME=ags
RABBITMQ_PASSWORD=ags_password
RABBITMQ_EXCHANGE=ags.domain.events
```

`JWT_SECRET` must match Identity Service and ops configuration. Do not commit real secrets.

## Local Run

Start PostgreSQL, MinIO, and RabbitMQ from the separate `prn232-ops` repository. This repository intentionally does not include infra compose files.

Expected local infra:

- PostgreSQL: `localhost:5432`, database `ags`, user `ags`, password `ags_password`
- MinIO: `http://localhost:9000`
- RabbitMQ: `localhost:5672`, `ags` / `ags_password`

Commands:

```bash
dotnet restore
dotnet tool restore
dotnet build
dotnet test
dotnet ef database update --project src/Class.Infrastructure --startup-project src/Class.Api
ASPNETCORE_URLS=http://localhost:8082 dotnet run --project src/Class.Api
curl http://localhost:8082/health
```

Development seeding is idempotent and creates one demo class:

- Name: `PRN232 - ASP.NET Core Web API`
- Lecturer id: `DEMO_LECTURER_ID` or `00000000-0000-0000-0000-000000000102`

Seeded IDs are for local visual testing only. Real E2E should create classes through a logged-in lecturer.

## Presigned Lab Asset Flow

1. Lecturer/admin calls `POST /api/classes/{classId}/labs` with lab metadata plus PDF and Postman collection filenames.
2. The service creates a `pending_assets` lab and returns presigned PUT URLs:
   - `requirements/{labId}/{safeFileName}`
   - `postman-collections/{labId}/{safeFileName}`
3. Frontend uploads files directly to MinIO using those URLs.
4. Frontend calls `POST /api/labs/{labId}/assets/complete`.
5. The service verifies object existence, marks the lab `active`, and writes a `LabCreated` outbox event.

The backend never receives multipart file bytes and does not use local file storage.

## Troubleshooting

### Presigned URL points to AWS S3

Problem: the create-lab response returns a presigned URL under `https://s3.amazonaws.com/...`, and direct upload fails with `InvalidAccessKeyId`.

Cause: S3 endpoint config is missing or the SDK is falling back to AWS S3 instead of local MinIO.

Fix: set both endpoints to the MinIO address the caller and service can reach, then restart Class Service:

```env
S3_PUBLIC_ENDPOINT=http://localhost:9000
S3_INTERNAL_ENDPOINT=http://localhost:9000
LAB_ASSETS_BUCKET=lab-assets
```

## Events

Outbox table: `classroom.outbox_events`

RabbitMQ exchange: `ags.domain.events` topic exchange

Events:

- `LabCreated` with routing key `class.lab-created.v1`
- `LabUpdated` with routing key `class.lab-updated.v1`

Business changes and outbox rows are saved together. The background publisher retries unpublished events and logs RabbitMQ failures without breaking completed API transactions.

## API Summary

All normal responses use:

```json
{ "success": true, "data": {} }
```

Errors use:

```json
{ "success": false, "error": { "code": "ERROR_CODE", "message": "Message", "details": {} } }
```

Endpoints:

- `POST /api/classes` lecturer
- `GET /api/classes?page=1&pageSize=20` lecturer/admin
- `GET /api/classes/search?name=PRN232&page=1&pageSize=20` student/lecturer/admin
- `GET /api/classes/{classId}` student joined class, owning lecturer, or admin
- `PUT /api/classes/{classId}` owning lecturer/admin
- `DELETE /api/classes/{classId}` owning lecturer/admin
- `POST /api/classes/{classId}/join` student
- `GET /api/classes/my?page=1&pageSize=20` student
- `GET /api/classes/{classId}/members?page=1&pageSize=20` owning lecturer/admin
- `POST /api/classes/{classId}/labs` owning lecturer/admin
- `GET /api/classes/{classId}/labs?page=1&pageSize=20` student joined class, owning lecturer, or admin
- `GET /api/labs/{labId}` student joined active lab, owning lecturer, or admin
- `PUT /api/labs/{labId}` owning lecturer/admin
- `DELETE /api/labs/{labId}` owning lecturer/admin
- `POST /api/labs/{labId}/assets/complete` owning lecturer/admin
- `GET /api/labs/{labId}/assets/requirement` student joined active lab, owning lecturer, or admin
- `GET /api/labs/{labId}/assets/collection` owning lecturer/admin only
- `GET /health` anonymous

Swagger is enabled in Development and supports Bearer token authentication.

## Runtime Migrations

Class Service can automatically apply EF Core migrations on startup without requiring `dotnet ef database update` to be run manually.

### How it works

On startup, `Class.Api` calls `Database.MigrateAsync()` using the runtime EF Core APIs. No `dotnet-ef` CLI tool is required inside the container.

### Control

The behavior is controlled by the `AUTO_APPLY_MIGRATIONS` environment variable:

```env
AUTO_APPLY_MIGRATIONS=true
```

| Scenario | Behavior |
|---|---|
| `ASPNETCORE_ENVIRONMENT=Development` + variable absent | Migrations applied (default `true`) |
| `ASPNETCORE_ENVIRONMENT=Production` + variable absent | Migrations skipped (default `false`) |
| `AUTO_APPLY_MIGRATIONS=true` | Migrations always applied |
| `AUTO_APPLY_MIGRATIONS=false` | Migrations always skipped |

### Retry

If Postgres is not yet ready at startup (common in Docker Compose), migration will retry up to 5 times with a 2-second delay between attempts. Transient connection errors are retried; permanent migration errors abort startup immediately so container logs clearly reveal the problem.

### Docker Compose

The full `prn232-ops` Docker stack relies on this for the local tester/dev flow:

```bash
docker compose --profile app down -v --remove-orphans
docker compose --profile app up -d
# class-service starts, detects Development env, applies classroom migrations automatically
make smoke-app
```

No manual SQL scripts or extra migration containers are needed.

## Docker

Build:

```bash
docker build -t evalcore-class-service:local .
```

Run example:

```bash
docker run --rm -p 8082:8080 --env-file .env evalcore-class-service:local
```

Published images:

- `dorrissdang/evalcore-class-service:main`
- `ghcr.io/automated-grading-system/evalcore-class-service:main`

## CI/CD

GitHub Actions:

- `Class PR Check`: restore, format check, build, test, Docker build on PRs.
- `Class Main CI`: restore, format check, build, test, Docker build on `main`.
- `Class Backend CD`: publishes multi-arch images to DockerHub and GHCR.
- `Class AI Code Review`: optional Vietnamese AI review when `OPENCODE_API_KEY` is configured.
- `Class AI PR Docs`: optional Vietnamese PR description update when `OPENCODE_API_KEY` is configured.

Required CD secrets:

- `DOCKERHUB_USERNAME`
- `DOCKERHUB_TOKEN`

GHCR uses `GITHUB_TOKEN`.
