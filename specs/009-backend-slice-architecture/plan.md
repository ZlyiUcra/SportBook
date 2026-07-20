# Implementation Plan: Backend rearchitecture to vertical slice architecture

**Branch**: `009-backend-slice-architecture` | **Date**: 2026-07-20 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/009-backend-slice-architecture/spec.md`

**Note**: This plan documents a rework already implemented and verified (see spec.md
Assumptions) - it records the technical approach actually taken, not a forward-looking design to
be estimated.

## Summary

Convert the SportBook backend's HTTP layer from ASP.NET Core MVC Controllers to Minimal API
endpoint-mapping files, and convert its Application layer from flat, multi-purpose service
classes to one Command/Query + Handler pair per client-reachable action, organized under
`Features/<Resource>/<UseCase>/` and dispatched through `martinothamar/Mediator` (MIT-licensed,
source-generator based). No route, request/response shape, or status code changes for any
existing client.

## Technical Context

**Language/Version**: C# / .NET 10

**Primary Dependencies**: ASP.NET Core Minimal API (framework-included, no new package);
`Mediator.Abstractions` + `Mediator.SourceGenerator` 3.0.2 (martinothamar/Mediator, MIT) - the
one new dependency this rework adds, chosen over MediatR (commercial-licensed since v13.0.0) and
over a hand-rolled dispatcher after a five-perspective review (`.specify/consilium/
2026-07-20-mediator-adoption.md`)

**Storage**: SQL Server via EF Core 10 (unchanged - no schema, migration, or query-shape change)

**Testing**: xUnit - `SportBook.UnitTests` (Sqlite in-memory) and `SportBook.IntegrationTests`
(`WebApplicationFactory` against the real SQL Server container), both pre-existing and unchanged
in what they assert

**Target Platform**: ASP.NET Core web service (unchanged deployment target)

**Project Type**: Web service backend (the SportBook web app's existing backend; the frontend is
untouched by this feature)

**Performance Goals**: None new - the rework is a routing/dispatch-layer restructuring above
unchanged query logic; consilium performance review found the chosen dispatch library
measurably faster than the rejected alternative (compile-time handler resolution vs. MediatR's
reflection-based lookup) but this was not the deciding factor

**Constraints**: Zero wire-contract change for any endpoint (spec FR-002/FR-007); the dispatch
mechanism must not depend on a revenue-gated commercial license (spec FR-005)

**Scale/Scope**: 8 resource areas (Auth, Availability, Bookings, Cities, Courts, Reviews, Users,
Venues), 26 client-reachable actions converted

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

`.specify/memory/constitution.md` is still the unfilled bootstrap template - no ratified
principles, so the gate trivially passes pre- and post-design (same status as 001-008).

## Project Structure

### Documentation (this feature)

```text
specs/009-backend-slice-architecture/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md         # Phase 1 output
├── quickstart.md         # Phase 1 output
├── contracts/            # Phase 1 output
└── tasks.md              # Phase 2 output (/speckit-tasks - not created by this command)
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── SportBook.Api/
│   │   └── Endpoints/                        # One MapXxxEndpoints file per resource (8 files) -
│   │                                          # route/verb/auth registration + a single
│   │                                          # mediator.Send hand-off, no business logic
│   ├── SportBook.Application/
│   │   ├── Features/<Resource>/<UseCase>/     # One folder per action (26 total) - a
│   │   │                                      # Command/Query record + its Handler, together
│   │   ├── Services/                          # Logic genuinely shared across actions only:
│   │   │                                      # OwnershipChecks, AuthTokenIssuer,
│   │   │                                      # VenueDetailReader, VenueLocationValidator,
│   │   │                                      # BookingHelpers, CityService (internal-only
│   │   │                                      # GetNeighborIdsAsync), CityDirectoryCache,
│   │   │                                      # CityDistance - none of these answer an
│   │   │                                      # endpoint of their own
│   │   └── Dtos/                              # Response shapes and any request DTO still
│   │                                          # needed as a body-binding target distinct from
│   │                                          # its Command (e.g. a route id combines with it)
│   ├── SportBook.Domain/                      # Unchanged
│   └── SportBook.Infrastructure/              # Unchanged
└── tests/
    ├── SportBook.UnitTests/                   # Unchanged assertions; call sites updated to
    │                                          # construct Handlers instead of Services
    └── SportBook.IntegrationTests/            # Unchanged assertions (still exercise HTTP);
                                               # one added regression test (raw string-valued
                                               # enum body) closing a gap the existing suite
                                               # could not see
```

**Structure Decision**: Two-layer split preserved from the pre-existing Clean-Architecture
project boundary (`SportBook.Api` / `SportBook.Application` / `SportBook.Domain` /
`SportBook.Infrastructure`) - routing lives in `Api/Endpoints/`, action logic lives in
`Application/Features/`. A single project holding both was considered and rejected: it would
blur the existing, already-tested boundary between "what HTTP shape does a client see" and "what
does the action do," for no gain identified during the consilium review.

## Complexity Tracking

*No Constitution Check violations - table intentionally omitted.*
