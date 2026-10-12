# Project Mercury 🚀

A multi-tenant speed-to-lead quoting engine and dispatch dashboard for home service operators. 

When a prospect submits a request (via Thumbtack, web form, etc.), Mercury ingests the payload, evaluates the business's deterministic pricing formula, and dispatches a personalized SMS quote and booking link within seconds.

## 🏗️ Architecture (Monorepo)

This repository contains both the client and server applications:

*   **/src/frontend**: React + TypeScript client. Provides the dashboard for operators to view leads, adjust pricing formulas, and manage RBAC (Role-Based Access Control) user settings.
*   **/src/backend**: C# / .NET 10 Web API. Handles webhook ingestion, Entity Framework Core multi-tenant data isolation, pricing logic, and Twilio API integrations.
*   **Database**: PostgreSQL (containerized for local development), accessed through the `Npgsql.EntityFrameworkCore.PostgreSQL` provider.

## 📂 Project Structure

```text
mercury/
├── src/
│   ├── backend/          # .NET 10 solution and API projects
│   └── frontend/         # React application (Vite)
├── docker-compose.yml    # Spins up the local PostgreSQL database and dependencies
├── mercury_ai_spec.md    # Engineering specification: stack, entities, security rules
└── README.md
```

## Local Database

PostgreSQL runs in Docker. Requires Docker Desktop (or any Docker Engine with Compose v2).

```sh
cp .env.example .env     # Windows: Copy-Item .env.example .env
docker compose up -d
```

`.env` holds local-only credentials and is git-ignored. `.env.example` is the committed template — never commit a real `.env`.

The first start creates the `mercury` database and the `mercury_app` role, then persists data in the `mercury-postgres-data` volume. The container reports healthy once `pg_isready` succeeds, so dependent services can wait on it.

Connection string for the API, matching the defaults in `.env.example`:

```
Host=localhost;Port=5432;Database=mercury;Username=mercury_app;Password=local_dev_only_password
```

Supply it through user secrets rather than committing it to `appsettings.json`:

```sh
dotnet user-secrets --project src/backend/Mercury.Api set "Database:ConnectionString" "Host=localhost;Port=5432;Database=mercury;Username=mercury_app;Password=local_dev_only_password"
```

To browse the database, start the optional Adminer container and open http://localhost:8080 (system `PostgreSQL`, server `postgres`):

```sh
docker compose --profile tools up -d
```

Useful commands:

```sh
docker compose ps                         # container status and health
docker compose logs -f postgres           # follow database logs
docker compose down                       # stop, keeping data
docker compose down -v                    # stop and delete all local data
```

Both ports bind to `127.0.0.1`, so neither the database nor Adminer is reachable from your network.

## Backend

Install the .NET SDK version pinned in `global.json` (10.0.401).
Shared compiler settings are defined in `Directory.Build.props`, including warnings as errors.

* **Mercury.Api**: ASP.NET Core composition root, endpoints, and middleware; references Core and Infrastructure.
* **Mercury.Core**: Entities, enums, interfaces, and configuration options; no infrastructure dependencies.
* **Mercury.Infrastructure**: Home for the future EF Core `AppDbContext`, Twilio client, and Stripe client; references Core.
* **Mercury.Tests**: xUnit test project; references Api, Core, and Infrastructure.

From the repository root:

```sh
dotnet build src/backend/Mercury.sln
dotnet test src/backend/Mercury.sln
dotnet run --project src/backend/Mercury.Api
```

Alternatively, run `dotnet build` and `dotnet test` from `src/backend`.
The scaffold does not yet expose application endpoints.

## Configuration

Every setting is bound to a strongly typed options class and validated **at startup**. A missing or
malformed value stops the application immediately with a message naming the setting and the command
to fix it, rather than failing later on a request.

| Section | Setting | Required | Secret | Default |
| --- | --- | --- | --- | --- |
| `Database` | `ConnectionString` | yes | yes | — |
| `Database` | `MaxRetryCount` | no | no | `3` |
| `Database` | `CommandTimeoutSeconds` | no | no | `30` |
| `Clerk` | `Authority` | yes | no | — |
| `Clerk` | `SecretKey` | yes | yes | — |
| `Clerk` | `Audience` | no | no | none |
| `Twilio` | `Enabled` | no | no | `false` |
| `Twilio` | `AccountSid` | when enabled | yes | — |
| `Twilio` | `AuthToken` | when enabled | yes | — |
| `Stripe` | `Enabled` | no | no | `false` |
| `Stripe` | `SecretKey` | when enabled | yes | — |
| `Stripe` | `WebhookSecret` | when enabled | yes | — |

Twilio and Stripe are disabled by default so the API runs locally without accounts for either.
Enable one only when you are working on it, and supply its credentials at the same time.

### Where values come from

* **Local development** — `dotnet user-secrets`. Secrets live outside the repository, so they cannot be committed.
* **Containers and CI** — environment variables, using `__` for the section separator, for example `Database__ConnectionString`.
* **`appsettings.json`** — non-secret defaults only.

`src/backend/Mercury.Api/appsettings.Development.json.example` lists every key with placeholder values
as a reference. Do not copy real secrets into `appsettings.Development.json`; that file is committed.

### Minimum setup to start the API

```sh
dotnet user-secrets --project src/backend/Mercury.Api set "Database:ConnectionString" "Host=localhost;Port=5432;Database=mercury;Username=mercury_app;Password=local_dev_only_password"
dotnet user-secrets --project src/backend/Mercury.Api set "Clerk:Authority" "https://your-app.clerk.accounts.dev"
dotnet user-secrets --project src/backend/Mercury.Api set "Clerk:SecretKey" "sk_test_your_clerk_secret_key"
```

Clerk values come from the Clerk dashboard under **API Keys**. Without them the API will not start,
because every request is expected to carry a Clerk bearer token.

To enable an integration later:

```sh
dotnet user-secrets --project src/backend/Mercury.Api set "Twilio:Enabled" "true"
dotnet user-secrets --project src/backend/Mercury.Api set "Twilio:AccountSid" "AC..."
dotnet user-secrets --project src/backend/Mercury.Api set "Twilio:AuthToken" "..."
```
