# Quickstart: Backend rearchitecture to vertical slice architecture

End-to-end validation that the rework changed nothing observable. Backend-only; no frontend
changes to validate.

## Prerequisites

- SQL Server container running (`docker-compose up -d`, see repository root `README.md`).
- Backend buildable: `dotnet build SportBook.sln` from the repository root.

## Automated checks

```powershell
dotnet build SportBook.sln
dotnet test backend/tests/SportBook.UnitTests/SportBook.UnitTests.csproj
dotnet test backend/tests/SportBook.IntegrationTests/SportBook.IntegrationTests.csproj
```

EXPECT: clean build (only the pre-existing `NU1510`/`NU1903` warnings, unrelated to this
feature); 51 unit tests green; 72 integration tests green (71 pre-existing + 1 new regression
test for the raw string-valued enum body, see contracts/api.md).

## Manual verification (live server)

```powershell
dotnet run --project backend/src/SportBook.Api
```

Then, against `http://localhost:5217`:

1. `POST /api/auth/logout` with no `Authorization` header and any JSON body → EXPECT `401`.
2. `POST /api/auth/register`, then `/login`, then `/refresh`, each with no prior token →
   EXPECT `201`, `200`, `200` respectively.
3. `GET /api/venues?mine=true&pageSize=100000` (authenticated) → EXPECT the response's
   `pageSize` field to read `100`, not `100000`.
4. `GET /api/venues/{anyGuid}/courts`, `/reviews`, and `GET /api/courts/{anyGuid}/availability`,
   each with no `Authorization` header → EXPECT `401` for all three (proves the fallback auth
   policy still reaches routes nested under a different resource's URL prefix).
5. `GET /api/users/me` (authenticated) → EXPECT the caller's own profile only.
6. `POST /api/venues/{venueId}/courts` with a raw JSON body (`{"sportType":"Tennis",...}`, not a
   typed client SDK object) → EXPECT `201` (proves the enum wire format from contracts/api.md
   holds against a real string-valued payload, not just the test suite's own serialization).

Delete any account/venue/court created during steps 2 and 6 afterward - this quickstart creates
no data meant to persist.

## Structural check

- `find backend/src/SportBook.Api/Controllers` → EXPECT: directory does not exist (SC-001).
- `find backend/src/SportBook.Application/Services -maxdepth 1 -name "*.cs"` → EXPECT: only the
  shared-collaborator files listed in data-model.md's Shared component table, none of the prior
  flat per-resource service classes (SC-002).
