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
dotnet user-secrets --project src/backend/Mercury.Api set "ConnectionStrings:MercuryDb" "Host=localhost;Port=5432;Database=mercury;Username=mercury_app;Password=local_dev_only_password"
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
* **Mercury.Core**: Entities, enums, and interfaces; no infrastructure dependencies.
* **Mercury.Infrastructure**: Home for the future EF Core `AppDbContext`, Twilio client, and Stripe client; references Core.
* **Mercury.Tests**: xUnit test project; references Api, Core, and Infrastructure. No tests are defined yet.

From the repository root:

```sh
dotnet build src/backend/Mercury.sln
dotnet test src/backend/Mercury.sln
dotnet run --project src/backend/Mercury.Api
```

Alternatively, run `dotnet build` and `dotnet test` from `src/backend`.
The scaffold does not yet expose application endpoints.
