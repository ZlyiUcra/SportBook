# SportBook

SportBook is a platform for booking sports venues (courts and fields). Customers search venues by
city and sport type, view available time slots, and book or cancel bookings. Venue owners manage
their own venues and courts, confirm incoming bookings, and see who booked what. Authenticated
users leave reviews on venues they have used. The product is designed to support future
monetization on both sides of the marketplace (see `specs/001-sportbook-venue-booking/spec.md` for
the full specification).

## Current status

All three planned user stories are implemented end to end (backend + frontend):

- Book a court: search venues, view availability, book, cancel (2-hour cutoff).
- Manage venue and courts: venue owners create/edit/delete their own venues and courts, confirm
  pending bookings, view their own venue's bookings.
- Build trust through reviews: authenticated users rate and review a venue (one review per user
  per venue), see the venue's average rating.

Automated tests currently cover the booking flow (25 tests: 11 unit, 14 integration against a
real SQL Server instance). Tests for venue management and reviews are deferred to the polish
phase - see `specs/001-sportbook-venue-booking/tasks.md`.

## Components

```text
backend/
  src/
    SportBook.Api             ASP.NET Core Web API - controllers, JWT auth, DI wiring
    SportBook.Application     Services (business logic), DTOs, request/response mapping
    SportBook.Domain          Entities, enums - no framework dependencies
    SportBook.Infrastructure  EF Core DbContext, migrations, SQL Server provider registration
  tests/
    SportBook.UnitTests         xUnit + EF Core Sqlite in-memory - no real database needed
    SportBook.IntegrationTests  xUnit + WebApplicationFactory - runs against the real SQL Server
                                 container (booking-overlap concurrency needs the real engine)

frontend/
  src/                       Feature-Sliced Design layering
    app/                     Routes, layouts, providers
    pages/                   One folder per route (ui/<Route>Page.tsx)
    features/                One user action per slice (ui + model + api)
    entities/                Domain data (types, read API calls)
    shared/                  UI kit (shadcn/ui), Axios instance, i18n, theme store, utils

docker-compose.yml           SQL Server 2025 Developer edition for local development
specs/001-sportbook-venue-booking/
                             Full spec, plan, data model, API contracts, task breakdown
```

**Backend stack**: C# / .NET 10, ASP.NET Core Web API (MVC controllers), EF Core 10 +
`Microsoft.EntityFrameworkCore.SqlServer`, JWT bearer authentication, xUnit.

**Frontend stack**: React 19, Vite, TypeScript, TanStack Query, Zustand, React Hook Form + Zod,
Axios, Tailwind CSS + shadcn/ui, i18next (English, Ukrainian, Portuguese), Vitest.

## Prerequisites

- Docker (Desktop or Engine) - runs SQL Server 2025 Developer edition locally, no native SQL
  Server install needed. Developer edition is licensed for development/test only, never for
  production (see `specs/001-sportbook-venue-booking/research.md`).
- .NET 10 SDK.
- The `dotnet-ef` global tool: `dotnet tool install --global dotnet-ef` (or
  `dotnet tool update --global dotnet-ef` if an older version is already installed).
- Node.js and yarn.

## Local setup

### Quick start (scripts)

`scripts/start.ps1` runs the one-time setup below (idempotent - safe to re-run) and then opens
the backend and frontend dev servers each in their own PowerShell window:

```powershell
powershell -File scripts/start.ps1
```

It fills in sample values for the SQL login password and JWT signing key on first run (see
`scripts/setup.ps1` - the two variables at the top) so nothing needs to be typed in manually.
Those sample values are fine for solo local development; change them if this machine is ever
shared with anyone else.

Each dev server's window stays open only until it responds - once a service is confirmed up, its
window is hidden automatically (the process keeps running in the background; only the window
disappears). If a service fails to start, its window is left visible so you can read the error.

To stop both:

```powershell
powershell -File scripts/start.ps1 -Stop
```

This finds whatever is listening on the backend/frontend ports and stops it, including the
hidden windows - works even if you started them some other way.

Running just the one-time setup without starting the dev servers:

```powershell
powershell -File scripts/setup.ps1
```

Or step through the setup manually:

### 1. Start the database

```powershell
docker compose up -d
docker compose ps
```

First start takes tens of seconds while SQL Server initializes - wait until the `mssql` service
shows `healthy` before continuing.

### 2. Create the application's SQL login

The container only bootstraps the `sa` (sysadmin) login. The application is meant to connect with
a least-privilege login instead - `scripts/setup.ps1` does this step automatically; the command
below is what it runs, shown here for anyone stepping through setup manually:

```powershell
docker exec sportbook-mssql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "SportBook_Dev_Passw0rd" -C -b -Q "IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = 'sportbook_app') BEGIN CREATE LOGIN sportbook_app WITH PASSWORD = 'Sb_App_Login_Dev9!', CHECK_POLICY = ON; END; ALTER SERVER ROLE dbcreator ADD MEMBER sportbook_app;"
```

Feel free to change the password in that command - just use the same value in the connection
string in the next step.

### 3. Configure backend secrets

Nothing secret is committed to the repo. `appsettings.json` only documents the shape of the
required configuration with empty values; set the real values locally via `dotnet user-secrets`:

```powershell
cd backend/src/SportBook.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=127.0.0.1,14330;Database=SportBookDb;User Id=sportbook_app;Password=Sb_App_Login_Dev9!;TrustServerCertificate=True;Encrypt=True"
dotnet user-secrets set "Jwt:Key" "<any long random string, e.g. output of a password generator>"
```

`TrustServerCertificate=True` is required locally because the container presents a self-signed
certificate - this flag must never be used outside local development.

### 4. Create the database and run the backend

```powershell
cd backend
dotnet restore
dotnet ef database update --project src/SportBook.Infrastructure --startup-project src/SportBook.Api
dotnet run --project src/SportBook.Api
```

The API listens on `http://localhost:5217` by default (see
`backend/src/SportBook.Api/Properties/launchSettings.json`). The first `dotnet ef database
update` run creates the `SportBookDb` database - nothing else creates it.

### 5. Run the frontend

```powershell
cd frontend
yarn install
yarn dev
```

The dev server listens on `http://localhost:5173` and expects the API at
`http://localhost:5217/api` (see `frontend/.env.development`).

## Running tests

```powershell
dotnet test backend/tests/SportBook.UnitTests/SportBook.UnitTests.csproj
dotnet test backend/tests/SportBook.IntegrationTests/SportBook.IntegrationTests.csproj
```

Unit tests use an in-memory Sqlite database and need nothing else running. Integration tests
need the SQL Server container from step 1 running and reachable - they create and drop their own
`SportBookDb_Tests` database, separate from the one the app itself uses.

## Further reading

- `specs/001-sportbook-venue-booking/spec.md` - full feature specification.
- `specs/001-sportbook-venue-booking/plan.md` - technical plan and architecture decisions.
- `specs/001-sportbook-venue-booking/data-model.md` - entity definitions and validation rules.
- `specs/001-sportbook-venue-booking/contracts/api.md` - HTTP API contract.
- `specs/001-sportbook-venue-booking/tasks.md` - full task breakdown and current progress.
