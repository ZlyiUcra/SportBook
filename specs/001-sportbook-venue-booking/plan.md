# Implementation Plan: SportBook Venue Booking

**Branch**: `001-sportbook-venue-booking` | **Date**: 2026-07-15 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-sportbook-venue-booking/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

A two-project web application: an ASP.NET Core Web API backend (MVC Controllers) backed by
Microsoft SQL Server via EF Core, and a React SPA frontend, delivering venue search, court booking
with overlap-safe scheduling, owner venue/court management with booking confirmation, and venue
reviews - as scoped and constrained by the consilium reviews recorded in
`.specify/consilium/2026-07-15-sportbook.md` and `.specify/consilium/2026-07-16-mssql-day1.md`.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (LTS, GA Nov 2025, supported to Nov 2028) for the backend;
TypeScript 5.9+ for the frontend.

**Primary Dependencies**:
- Backend (NuGet): ASP.NET Core Web API with MVC Controllers (`[ApiController]`); EF Core 10 +
  `Microsoft.EntityFrameworkCore.SqlServer` (SQL Server provider);
  `Microsoft.AspNetCore.Authentication.JwtBearer`; `Microsoft.AspNetCore.DataProtection` pinned
  >=10.0.7 (CVE-2026-40372); built-in `Microsoft.AspNetCore.OpenApi` for API docs (no third-party
  Swashbuckle needed on .NET 10); xUnit + `Microsoft.NET.Test.Sdk` +
  `xunit.runner.visualstudio` for tests; `Microsoft.EntityFrameworkCore.Sqlite` (in-memory mode)
  for fast Service-layer unit tests without a real SQL Server instance.
- Frontend (npm/yarn): React 19, Vite 7, `react-router-dom` (~10KB gzip) for navigation,
  `@tanstack/react-query` (~13KB gzip) for server state; auth/session state via React Context
  (no extra dependency) rather than a state library, since this app has one piece of global
  client state (the current user); Vitest + `@testing-library/react` for tests. Note: `yarn
  create vite` scaffolded current registry latest at implementation time (Vite 8.1, TypeScript
  6.0.3) rather than the exact versions above - accepted as a routine drift, no architectural
  impact.

**Storage**: Microsoft SQL Server 2025 from day 1, via EF Core +
`Microsoft.EntityFrameworkCore.SqlServer`, run locally and in any non-prod environment via Docker
(`docker-compose.yml` at repo root, service `mssql`, image `mcr.microsoft.com/mssql/server:2025-latest`
- pinned major that tracks security CUs - Developer edition via `MSSQL_PID`). Developer edition is
licensed for development/test only and MUST NOT back any internet-facing or production deployment;
the production edition/hosting decision (Express within its caps, paid Standard/Enterprise, or
Azure SQL) is explicitly deferred - see research.md. Host bind `127.0.0.1:14330` -> container
`1433`: loopback-only, non-default port to avoid a natively installed SQL Server default instance.
Compose sets `MSSQL_MEMORY_LIMIT_MB` below the container memory limit (deterministic across cgroup
v1/v2; WSL2 defaults to v1/hybrid where the engine's cgroup-v2 awareness does not apply).
Application/Domain queries are LINQ-only and must translate under both the SqlServer provider and
the Sqlite test provider (the unit-test suite is the second consumer of this rule);
`FromSqlRaw`/`FromSqlInterpolated` remain banned as an injection-surface control - provider-specific
SQL (e.g. `sp_getapplock`, lock hints) is an Infrastructure-only exception requiring explicit
review. `decimal` columns (`PricePerHour`, `TotalPrice`) get explicit `HasPrecision`; all stored
timestamps are UTC by convention (`DateTime.UtcNow`, convert at the edges) since `datetime2` does
not preserve `DateTime.Kind`. Provider registration stays isolated in a single DI extension method
(test hosts swap it). Connection string is read from configuration/environment, not hardcoded; the
local dev connection string uses `TrustServerCertificate=True` for the container's self-signed
certificate - dev-only, never in any non-local connection string - and the app connects as a
least-privilege login (SA is bootstrap-only).

**Testing**: xUnit (backend unit + integration tests, `WebApplicationFactory` for endpoint tests,
EF Core Sqlite in-memory for service tests); Vitest + React Testing Library (frontend).

**Target Platform**: Cross-platform ASP.NET Core web service (Linux/Windows) behind HTTPS; React
SPA served as static assets, targeting evergreen browsers.

**Project Type**: Web application (backend API + frontend SPA, two independently deployable
projects).

**Performance Goals**: Derived from spec SC-005 - the API must serve at least 500 concurrent
`GET /venues` requests with p95 response time under 500ms and no more than 2x the single-user
baseline p95 latency; no other specific throughput target is set for a greenfield product with
unproven demand (per pragmatist/performance consilium guidance - avoid scaling infrastructure the
product doesn't need yet).

**Constraints**: Booking overlap check MUST be enforced server-side and hold under concurrent
requests (spec FR-004); cancellation cutoff of 2 hours before start MUST be enforced server-side
(FR-005); `TotalPrice` MUST be server-computed only, never trusted from the client (consilium
artifact); all response DTOs MUST be an explicit whitelist (never entity passthrough) so
`PasswordHash`/`Email` never leak; Application/Domain data access is LINQ-only and must stay
translatable under the Sqlite test provider (see Storage above); stored timestamps are UTC by
convention; ASCII-only source files per `CLAUDE.md`.

**Scale/Scope**: Greenfield product, 24 backend endpoints across 7 resource groups (auth, users,
venues, courts, availability, bookings, reviews), single deployable API + single SPA, no
multi-tenancy or CI/CD pipeline in scope.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

`.specify/memory/constitution.md` is still the unfilled bootstrap template (no principles have
been ratified for this project yet) - there are no constitution gates to evaluate against, so this
gate trivially passes. Recommend running `/speckit-constitution` before the project reaches
production maturity so future plans have real gates to check against; not a blocker for this plan.

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── SportBook.Api/              # Controllers, Program.cs, DI wiring, JWT/authorization setup
│   ├── SportBook.Application/      # Services (business logic incl. overlap check, pricing,
│   │                                # ownership checks), DTOs (Request/Response types), mapping
│   ├── SportBook.Domain/           # Entities (User, Venue, Court, Booking, Review), enums
│   └── SportBook.Infrastructure/   # EF Core DbContext, migrations, SqlServer provider registration
│                                    # (single DI extension; test hosts swap the provider here)
└── tests/
    ├── SportBook.UnitTests/        # xUnit, EF Core Sqlite in-memory, Service-layer logic
    │                                # (overlap check, pricing, cancellation cutoff)
    └── SportBook.IntegrationTests/ # xUnit + WebApplicationFactory, endpoint + auth tests

frontend/
├── src/
│   ├── api/                        # Typed API client + Request/Response types mirroring backend
│   ├── pages/                      # VenueSearch, VenueDetail, MyBookings, OwnerDashboard, etc.
│   ├── components/
│   ├── hooks/                      # useAuth, useVenues, useBookings (TanStack Query wrappers)
│   └── context/                    # AuthContext (current user, tokens)
└── tests/                          # Vitest + React Testing Library
```

**Structure Decision**: Web application layout (backend + frontend as two independent projects).
Backend uses a 4-project layered split (Api/Application/Domain/Infrastructure) so the
Application/Domain layers can be unit-tested without a real database (per best-practices
consilium finding on Service-layer testability), and so provider-specific code stays isolated to
Infrastructure - test hosts swap the provider there, and engine-specific mechanisms stay out of
Application/Domain.

## Complexity Tracking

Not applicable - Constitution Check has no gates to violate (constitution.md is unfilled).
