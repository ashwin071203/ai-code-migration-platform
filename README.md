# ForgeShift Migration Platform

Phase 1 foundation for a local-first AI code migration and application modernization platform.

## Current scope

- ASP.NET Core API targeting .NET 10 with SQL Server and EF Core configuration
- Separate Data, Repositories, Services, AI, MigrationEngine, and Worker projects
- JWT authentication with BCrypt password hashing
- Frontend registration and sign-in with validation and terms acceptance
- Local forgot-password and one-time reset-token workflow
- Initial EF Core migration for the complete Phase 1/2 data model
- Authenticated SignalR migration hub with reconnecting dashboard client
- API-side status broadcaster relaying persisted worker updates to SignalR groups
- Admin-only audit log read endpoint at `GET /api/v1/admin/audit`
- Admin-only user listing, role changes, and enable/disable operations under `/api/v1/admin/users`
- Admin migration rule CRUD under `/api/v1/rules`, persisted in SQL Server and ready for provider consumption
- Configurable AI layer with Ollama and OpenAI-compatible providers
- Java-to-C# provider with deterministic syntax mappings and manual-review warnings
- React Web-to-React Native provider with JSX primitive mappings and manual-review warnings
- Spring Boot-to-ASP.NET Core provider with configuration conversion and manual-review warnings
- Deterministic persisted validation stage with `GET /api/v1/migrations/{id}/validation`
- Authenticated project creation and listing
- Secure ZIP upload with local workspace isolation and path/size limits
- Evidence-based source profile analysis from project manifests
- Project file inventory persisted in SQL Server
- Backend-driven migration options with persisted plan review before queueing
- Provider-based worker execution with an isolated output workspace
- Persisted migration job queue with API and worker lifecycle status
- SignalR registration foundation
- Swagger and `/health` endpoint
- React + TypeScript + Vite dashboard with login, project creation, and responsive layout

Migration analysis, upload processing, providers, plans, validation, and reports are intentionally reserved for later phases.

Phase 2 currently includes ZIP upload and source profiling. The analyzer reports `Unknown` when the archive does not contain recognizable build evidence; it does not infer compatibility from file extensions alone.

## Prerequisites

- .NET SDK 10 preview or later compatible with the generated projects
- Node.js 20+ and npm
- Local Microsoft SQL Server instance

## SQL Server configuration

Edit `backend/MigrationPlatform.Api/appsettings.Development.json` and set `ConnectionStrings:DefaultConnection` for your instance. For example:

```text
Server=ASHWIN\MSSQLSERVER04;Database=MigrationPlatformDb;Trusted_Connection=True;TrustServerCertificate=True;
```

Use SQL authentication by replacing the connection string with `User Id=...;Password=...;`. Do not commit production credentials.

When SQL Server is available, create the initial EF Core database:

```powershell
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate --project backend/MigrationPlatform.Data --startup-project backend/MigrationPlatform.Api
dotnet ef database update --project backend/MigrationPlatform.Data --startup-project backend/MigrationPlatform.Api
```

The generated migration is stored in `backend/MigrationPlatform.Data/Migrations`. The API does not silently create a database at startup; apply the migration explicitly against the SQL Server instance configured for your machine.

## Run locally

Start the API:

```powershell
dotnet run --project backend/MigrationPlatform.Api --urls http://localhost:5100
```

Swagger is at `http://localhost:5100/swagger` and health is at `http://localhost:5100/health`.
Database connectivity is checked separately at `http://localhost:5100/health/database` and returns `503` without exposing connection details when SQL Server is unavailable.

Start the frontend in a second terminal:

```powershell
npm install --prefix frontend
npm run dev --prefix frontend
```

Open `http://localhost:5173`.

After creating a project, use **Upload ZIP** on its project card. The API accepts `POST /api/v1/projects/{id}/upload` and exposes the persisted profile at `GET /api/v1/projects/{id}/analysis`.

Use **Configure migration** on an uploaded project to select a backend-supported target. The workflow is:

```text
Create migration -> Generate plan -> Review steps and risks -> Start migration -> Worker queue
```

The plan endpoints are `GET /api/v1/migrations/options`, `POST /api/v1/migrations/{id}/plan`, `GET /api/v1/migrations/{id}/plan`, and `POST /api/v1/migrations/{id}/start`. Queued or active jobs can be stopped with `POST /api/v1/migrations/{id}/cancel`.

Migration status updates use the authenticated SignalR hub at `http://localhost:5100/hubs/migrations`. The frontend reconnects automatically and also reloads persisted migration state after reconnects.

The API status broadcaster polls persisted migration state and forwards changes to the matching SignalR group. This keeps the worker independent from the web process while preserving live updates and reconnect recovery.

The migration engine registers `.NET Framework -> Modern .NET`, `Java -> C#`, `Spring Boot -> ASP.NET Core Web API`, and `React Web -> React Native` providers. The Java provider maps package/import declarations, common primitive types, `System.out.println`, and the Java entry point. The Spring provider converts `application.properties` into a JSON configuration artifact and applies bounded Java syntax mappings. The React Native provider maps common JSX primitives such as `div` to `View`, `p` to `Text`, `button` to `Pressable`, and `img` to `Image`. Providers write `FORGESHIFT-MANUAL-REVIEW.md` and report `ManualReview`, not completed parity.

Start the worker in a third terminal when SQL Server is available. Its SQL Server and storage settings are configurable in `backend/MigrationPlatform.Worker/appsettings.json`:

```powershell
dotnet run --project backend/MigrationPlatform.Worker
```

The Phase 1 worker claims queued jobs and records an explicit manual-review failure because the migration engine is intentionally reserved for a later phase. It never reports generated code as completed before an engine exists.

Registration is available from the frontend sign-in screen. For API-level setup, register a user with Swagger or PowerShell:

```powershell
Invoke-RestMethod http://localhost:5100/api/v1/auth/register -Method Post -ContentType 'application/json' -Body '{"fullName":"Developer","email":"developer@local.test","password":"ChangeMe123!"}'
```

Forgot-password is also available from the sign-in screen. In Development, the API returns a one-time reset token in the response so the workflow works without an email server. Reset requests are hashed, expire after 30 minutes, and can only be used once.

## AI configuration

Ollama is the default provider and is optional until an AI-backed provider uses it:

```json
"AI": {
  "Provider": "Ollama",
  "Endpoint": "http://localhost:11434",
  "Model": "llama3.2"
}
```

For an OpenAI-compatible endpoint, set `Provider` to `OpenAI`, configure `Endpoint` and `Model`, and supply `AI:ApiKey` through user secrets or an environment variable. API keys are never returned in API responses.

## Project structure

```text
backend/
  MigrationPlatform.Api/
  MigrationPlatform.Data/
  MigrationPlatform.Repositories/
  MigrationPlatform.Services/
  MigrationPlatform.AI/
  MigrationPlatform.MigrationEngine/
  MigrationPlatform.Worker/
frontend/
  src/
MigrationPlatform.slnx
```

## Validation performed

```text
dotnet build MigrationPlatform.slnx --no-restore  PASS
npm run build --prefix frontend                 PASS
dotnet ef database update                        PASS
API registration/project smoke test             PASS
Standalone worker startup/database poll         PASS
Validation schema migration                      PASS
```

## Troubleshooting

- `401` on project requests: sign in again and confirm the frontend has a current token in local storage.
- SQL connection errors on register/login/project actions: check the configured server name, database permissions, and that the EF migration has been applied.
- CORS errors: keep Vite on `http://localhost:5173` or update the API CORS policy in `Program.cs`.
