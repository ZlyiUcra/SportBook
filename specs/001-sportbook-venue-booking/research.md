# Phase 0 Research: SportBook Venue Booking

All items below were either open questions from the consilium transfer artifact
(`.specify/consilium/2026-07-15-sportbook.md`) or NEEDS CLARIFICATION markers from the Technical
Context. User-confirmed decisions are marked as such rather than "researched"; the rest are
resolved with rationale here.

## Database engine

- **Decision**: Microsoft SQL Server 2025 from day 1, via `Microsoft.EntityFrameworkCore.SqlServer`.
  Development and all non-prod environments run Developer edition (free, licensed for
  development/test/demo only - it MUST NOT back any internet-facing or production deployment).
  The production edition/hosting/licensing decision (Express within its 10GB-per-DB/memory/CPU
  caps, paid Standard/Enterprise, or Azure SQL) is explicitly deferred until a production
  deployment is actually planned.
- **Rationale**: User-confirmed 2026-07-16, superseding the 2026-07-15 user-confirmed "PostgreSQL
  now, SQL Server later" decision (consilium artifact
  `.specify/consilium/2026-07-16-mssql-day1.md`; the 2026-07-15 artifact stays unedited as
  historical record). Driver: a Microsoft-licensed developer on the team works exclusively with
  SQL Server. The previous plan already committed to SQL Server "later", so switching pre-code
  deletes a mid-project engine migration instead of adding one. Minor technical gain: SQL Server's
  default case-insensitive collation makes EF `Contains` searches match the city/sport search UX
  with no extra code. Acknowledged tradeoff: PostgreSQL is free in production, SQL Server is not -
  a zero-cost production path is deliberately traded away.
- **Alternatives considered**: PostgreSQL now, SQL Server later (rejected - superseded by the team
  constraint above; its "portable swap later" promise was also already broken at the
  highest-priority item, the overlap constraint, which named a Postgres-only mechanism); SQLite
  for production (rejected - inadequate for concurrent writers, only used for fast unit tests).

## Local database hosting

- **Decision**: SQL Server 2025 runs in Docker for local development (and any non-prod environment
  until a real hosting decision is made), via a `docker-compose.yml` at the repo root: image
  `mcr.microsoft.com/mssql/server:2025-latest` (pinned major, tracks cumulative security updates),
  `MSSQL_PID=Developer`, `ACCEPT_EULA=Y`, an SA password meeting SQL Server complexity policy,
  `MSSQL_MEMORY_LIMIT_MB` set below the container memory limit, a healthcheck so dependent flows
  wait for readiness (cold start takes tens of seconds), and a volume on `/var/opt/mssql`. Host
  bind `127.0.0.1:14330` maps to the container's `1433` - loopback-only (no LAN exposure of a
  sysadmin-credentialed dev database) and non-default so it doesn't collide with a natively
  installed SQL Server default instance on `1433` (the same collision class the previous
  5434-vs-5432 choice guarded against).
- **Rationale**: User-confirmed. Keeps local setup to `docker compose up -d`; the app only needs a
  connection string. The image creates no application database - the first
  `dotnet ef database update` creates it, so the database name lives only in the connection
  string. The dev connection string needs `TrustServerCertificate=True` (self-signed container
  certificate); dev-only, never in a non-local connection string. The app connects as a
  least-privilege login; SA is bootstrap-only. The old `sportbook-postgres-data` Docker volume, if
  it exists locally, is left for the user to clean up - nothing deletes it.
- **Alternatives considered**: Native SQL Server install (rejected - more setup friction, and ties
  dev environment setup to the host OS rather than to a portable compose file); default `1433`
  host mapping (rejected - collision with a local default instance is plausible for the SQL
  Server-oriented teammate); `:latest` tag (rejected - unpinned major-version drift; `2025-latest`
  tracks security CUs within the pinned major).

## API style: MVC Controllers vs Minimal APIs

- **Decision**: MVC Controllers (`[ApiController]`, one controller per resource group).
- **Rationale**: User-confirmed.
- **Alternatives considered**: Minimal APIs (rejected by user preference; would have offered a
  minor throughput edge per Microsoft .NET 9+ benchmarks, not decisive at this endpoint count).

## Test framework

- **Decision**: xUnit for all backend tests.
- **Rationale**: User-confirmed.
- **Alternatives considered**: NUnit (rejected by user preference).

## DTO naming and mapping strategy

- **Decision**: Suffix convention `{Entity}Response` for reads, `Create{Entity}Request` /
  `Update{Entity}Request` for writes (e.g. `VenueResponse`, `CreateVenueRequest`). Mapping is
  hand-written extension methods (`ToResponse()`, `ApplyTo(entity)`) living next to each DTO, not
  a mapping library.
- **Rationale**: Avoids introducing a new dependency (Mapster/AutoMapper) for ~5 entities x CRUD,
  which `CLAUDE.md` requires explicit user sign-off for; hand-written mapping is direct enough at
  this scale and keeps entity-to-DTO field selection (whitelist) visible in code review, which
  directly closes the security consilium finding on accidental `PasswordHash`/`Email` leakage.
- **Alternatives considered**: Mapster (rejected - unnecessary dependency at this scale, and
  reflection/convention-based mapping is exactly the failure mode the security review flagged for
  the password-hash leak risk).

## Namespace / project layout

- **Decision**: `SportBook.Api`, `SportBook.Application`, `SportBook.Domain`,
  `SportBook.Infrastructure` (see plan.md Project Structure). File-scoped namespaces, nullable
  reference types enabled project-wide, per `CLAUDE.md` C#/.NET conventions.
- **Rationale**: Closes the nitpicker/best-practices findings on undecided project structure and
  Service-layer testability; four projects is the smallest split that keeps EF Core/SQL Server
  specifics out of Application/Domain (Sqlite-test translatability requirement) while keeping
  Application unit testable without a live database.
- **Alternatives considered**: Single project with folders (rejected - would leak
  provider-specific EF Core configuration into code that must stay translatable under the Sqlite
  test provider); Repository pattern on top of EF Core (rejected per consilium best-practices
  finding - no second storage provider exists yet to justify the extra layer, DbContext is
  accessed directly from Application services).

## Availability response contract and minimum booking increment

- **Decision**: Bookings are made in whole-hour increments (matches `PricePerHour` pricing, per
  spec Assumptions). `GET /courts/{id}/availability?date=` returns the list of free hourly slots
  for that date as `{ start, end }` pairs within the court's operating hours, excluding any
  slot that overlaps a Pending or Confirmed booking.
- **Rationale**: Closes the nitpicker finding on unspecified availability contract; whole-hour
  granularity is the simplest contract consistent with hourly pricing and avoids a fractional
  pricing edge case entirely.
- **Alternatives considered**: Configurable increment (e.g. 30 min) per court - rejected for this
  iteration as unnecessary complexity without a stated need (pragmatist guidance); can be added
  later by changing the slot-generation step without a contract-breaking change if increments stay
  a divisor of one hour.

## Pagination contract

- **Decision**: All list endpoints (`GET /venues`, `GET /venues/{id}/courts`,
  `GET /venues/{id}/reviews`, `GET /bookings` mine and by-venue) use offset pagination with
  `page` (1-based) and `pageSize` (default 20, max 100) query parameters, returning
  `{ items: [...], page, pageSize, totalCount }`.
- **Rationale**: Closes the nitpicker/performance finding that pagination was unspecified for
  Reviews/Courts lists; offset pagination is simple to reason about and sufficient at this scale
  (no infinite-scroll or very large collections in scope).
- **Alternatives considered**: Cursor-based pagination (rejected - added complexity not justified
  by current scale; offset pagination is adequate until a specific large-collection problem is
  measured).

## Refresh token storage

- **Decision**: A `RefreshToken` entity (Id, UserId, TokenHash, ExpiresAt, RevokedAt) persisted in
  the database; `POST /auth/refresh` validates and rotates it, `POST /auth/logout` sets
  `RevokedAt`.
- **Rationale**: Closes the nitpicker finding that the domain model had no place to store a
  refresh token despite `/refresh` and `/logout` existing in the endpoint list; a persisted,
  revocable token is the minimum needed to make `/logout` do something real (rather than being a
  client-only no-op) and to support token rotation on refresh.
- **Alternatives considered**: Access-token-only auth (no refresh, no logout) - rejected because
  `/refresh` and `/logout` are already agreed endpoints in the consilium artifact and product
  description; in-memory-only revocation list - rejected as it would not survive an app restart,
  no different tradeoff would be gained over a small persisted table.

## Authorization checklist (per consilium security finding)

- **Decision**: Every mutating/single-resource-read endpoint enforces ownership via the resource's
  existing FK chain, checked in the Application service before any mutation:
  - Venue update/delete: `venue.OwnerId == currentUserId`
  - Court create/update/delete: `court.Venue.OwnerId == currentUserId`
  - Booking cancel: `booking.UserId == currentUserId` (owners cannot cancel per spec FR-011)
  - Booking confirm: `booking.Court.Venue.OwnerId == currentUserId`
  - Booking get/list-mine: `booking.UserId == currentUserId`
  - Venue bookings list (owner view): `venue.OwnerId == currentUserId`
  - `OwnerId`/`UserId`/`Role` on any entity are always taken from the authenticated JWT claims,
    never from request body fields, even if a client sends them.
  - `Register` request DTO has no `Role` field; every new account is created as `Customer`.
- **Rationale**: This is exactly the "must-document-before-code" resolution the security
  archetype converged on after interrogation - no domain model change was needed, only an
  explicit, written rule per endpoint. Documented here so `/speckit-tasks` generates one task per
  check rather than leaving it implicit.

## `GET /venues/{id}` cartesian-explosion avoidance

- **Decision**: Loading a venue's Courts and Reviews together uses either `AsSplitQuery()` (two
  SQL statements) or two separate queries (venue+courts, then a `GroupBy` aggregate query for
  review count/average rating) - never a single query with two sibling collection `Include`s.
- **Rationale**: Closes the performance consilium finding with a concrete, cheap fix at the query
  level; no schema or endpoint contract change required.
- **Alternatives considered**: Single query with both `Include`s (rejected - confirmed cartesian
  row-count blowup, e.g. 10 courts x 50 reviews = 500 rows fetched for 60 logical rows).
