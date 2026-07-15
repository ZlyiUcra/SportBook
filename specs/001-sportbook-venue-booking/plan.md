# Implementation Plan: SportBook Venue Booking

**Branch**: `001-sportbook-venue-booking` | **Date**: 2026-07-15 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-sportbook-venue-booking/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

A two-project web application: an ASP.NET Core Web API backend (MVC Controllers) backed by
PostgreSQL via EF Core, and a React SPA frontend, delivering venue search, court booking with
overlap-safe scheduling, owner venue/court management with booking confirmation, and venue
reviews - as scoped and constrained by the consilium review recorded in
`.specify/consilium/2026-07-15-sportbook.md`.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (LTS, GA Nov 2025, supported to Nov 2028) for the backend;
TypeScript 5.9+ for the frontend.

**Primary Dependencies**:
- Backend (NuGet): ASP.NET Core Web API with MVC Controllers (`[ApiController]`); EF Core 10 +
  `Npgsql.EntityFrameworkCore.PostgreSQL` (PostgreSQL provider);
  `Microsoft.AspNetCore.Authentication.JwtBearer`; `Microsoft.AspNetCore.DataProtection` pinned
  >=10.0.7 (CVE-2026-40372); built-in `Microsoft.AspNetCore.OpenApi` for API docs (no third-party
  Swashbuckle needed on .NET 10); xUnit + `Microsoft.NET.Test.Sdk` +
  `xunit.runner.visualstudio` for tests; `Microsoft.EntityFrameworkCore.Sqlite` (in-memory mode)
  for fast Service-layer unit tests without a real Postgres instance.
- Frontend (npm/yarn): React 19, Vite 7, `react-router-dom` (~10KB gzip) for navigation,
  `@tanstack/react-query` (~13KB gzip) for server state; auth/session state via React Context
  (no extra dependency) rather than a state library, since this app has one piece of global
  client state (the current user); Vitest + `@testing-library/react` for tests.

**Storage**: PostgreSQL now, via EF Core/Npgsql, run locally and in any non-prod environment via
Docker (`docker-compose.yml` at repo root, service `postgres`, host port `5434` -> container
`5432` to avoid clashing with a locally-installed Postgres on the dev machine). Per explicit user
direction, the product moves to SQL Server later - this iteration MUST keep storage access
portable: all queries expressed in LINQ (no `FromSqlRaw`/Postgres-only SQL), no Postgres-only
column types (no `jsonb`, arrays, or `ILIKE`) in entity mappings, and provider registration
isolated to a single DI extension method so swapping to `Microsoft.EntityFrameworkCore.SqlServer`
later is a provider-and-connection-string change, not an app rewrite. Connection string is read
from configuration/environment, not hardcoded, so the same appsettings shape works whether Postgres
is containerized (dev) or a managed instance (later environments).

**Testing**: xUnit (backend unit + integration tests, `WebApplicationFactory` for endpoint tests,
EF Core Sqlite in-memory for service tests); Vitest + React Testing Library (frontend).

**Target Platform**: Cross-platform ASP.NET Core web service (Linux/Windows) behind HTTPS; React
SPA served as static assets, targeting evergreen browsers.

**Project Type**: Web application (backend API + frontend SPA, two independently deployable
projects).

**Performance Goals**: Derived from spec SC-005 - the API must serve at least 500 concurrent venue
search requests without added latency versus baseline; no other specific throughput target is set
for a greenfield product with unproven demand (per pragmatist/performance consilium guidance -
avoid scaling infrastructure the product doesn't need yet).

**Constraints**: Booking overlap check MUST be enforced server-side and hold under concurrent
requests (spec FR-004); cancellation cutoff of 2 hours before start MUST be enforced server-side
(FR-005); `TotalPrice` MUST be server-computed only, never trusted from the client (consilium
artifact); all response DTOs MUST be an explicit whitelist (never entity passthrough) so
`PasswordHash`/`Email` never leak; storage must stay portable to SQL Server (see Storage above);
ASCII-only source files per `CLAUDE.md`.

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
│   └── SportBook.Infrastructure/   # EF Core DbContext, migrations, Npgsql provider registration
│                                    # (single point to swap for SQL Server later)
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
consilium finding on Service-layer testability), and so Postgres-specific code stays isolated to
Infrastructure for the planned future SQL Server migration.

## Complexity Tracking

Not applicable - Constitution Check has no gates to violate (constitution.md is unfilled).
