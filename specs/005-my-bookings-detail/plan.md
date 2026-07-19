# Implementation Plan: My bookings - venue detail, status filter, and pagination

**Branch**: `005-my-bookings-detail` | **Date**: 2026-07-19 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/005-my-bookings-detail/spec.md`

## Summary

Make a booking legible and navigable. Three slices: (1) enrich the booking response so every
booking carries its venue name, city, court sport, and court name - the booking already links to a
court (booking -> court -> venue -> city; sport is on the court), so this is an Include chain plus
a wider DTO, surfaced on both the customer "My bookings" list and the owner "Venue bookings" list;
(2) a server-side status filter on the customer's bookings - All / Upcoming / Completed / Cancelled
- where "Completed" is the existing read-time derivation (Confirmed + past end time), mapped to a
translatable predicate so it composes correctly with paging; (3) surface the paging the list
endpoint already returns with Previous/Next controls that reset to page one on filter change. The
booking-overlap, pricing, and cancellation rules from 001 are untouched. All product choices were
made by the user in the 2026-07-19 discussion and are recorded in the spec's Assumptions.

## Technical Context

**Language/Version**: C# 14 / .NET 10 backend; TypeScript 6.0 frontend - unchanged from 001-004.

**Primary Dependencies**: No new packages, backend or frontend. Reuses EF Core Include chains,
the existing `PagedResponse`/`PageRequest`, `CityResponse`, and the shadcn UI kit already present.

**Storage**: Same SQL Server / EF Core. NO schema change and NO migration - every field the
enriched response needs already exists on Court/Venue/City; the status filter is a query predicate,
not stored data.

**Testing**: xUnit as in 001-003. A pure unit test asserts the status-filter predicate maps each
choice to the right stored-status/time combination on the Sqlite path and a `ToQueryString()` guard
proves the filter and the Include chain translate to SQL (no client evaluation). Integration tests
cover the enriched fields on the response and the filter's cross-page correctness. Frontend: Vitest
+ RTL for the filter tabs, the pagination controls and their reset-on-filter, and the rendered
venue/city/sport/court detail.

**Target Platform**: Unchanged - ASP.NET Core service + React SPA.

**Project Type**: Web application (backend API + frontend SPA), unchanged.

**Performance Goals**: The enriched list adds a bounded Include join (court + venue + city) to an
already-paged query (page size unchanged), so per-request cost stays flat in the page size, not the
history size. The status filter is a `WHERE` predicate evaluated server-side before paging, so
filtering never materializes the whole history.

**Constraints**: The status-filter predicate MUST be SQL-translatable and applied before Skip/Take
so it composes with paging across the whole set (spec FR-006) - no client-side filtering of a
materialized page. The enriched `BookingResponse` MUST NOT expose new internal-only fields (no
owner id, no extra user data beyond what 001 already returns); it adds only human-readable venue/
city/sport/court labels (spec FR-011). "Completed" stays a derived view, never a stored status
(001 invariant). ASCII-only source files per repo rules.

**Scale/Scope**: 1 widened response DTO (`BookingResponse` + venue/city/sport/court), an Include
chain on the three booking-list/read paths, 1 status-filter parameter on the customer bookings
endpoint (+ its service predicate), and frontend changes to the customer "My bookings" page (filter
tabs, pagination controls, richer rows) plus the shared booking row detail on the owner "Venue
bookings" page. i18n additions in en/uk/pt. No schema, no migration, no new dependency.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

`.specify/memory/constitution.md` is still the unfilled bootstrap template - no ratified
principles, so the gate trivially passes pre- and post-design (same status as 001-004). The
standing recommendation to run `/speckit-constitution` remains open; not a blocker.

## Project Structure

### Documentation (this feature)

```text
specs/005-my-bookings-detail/
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
│   ├── SportBook.Api/              # BookingsController: + `status` query param on GET /api/bookings
│   │                                # (All|Upcoming|Completed|Cancelled), default All
│   ├── SportBook.Application/      # BookingResponse: + VenueName, City (CityResponse), Sport,
│   │                                # CourtName. BookingService: Include court->venue->city on the
│   │                                # list/read paths; a BookingStatusFilter -> translatable
│   │                                # predicate (Upcoming/Completed/Cancelled/All) applied before
│   │                                # Skip/Take. Mapping.ToResponse reads the loaded chain.
│   ├── SportBook.Domain/           # unchanged (reuses Booking/Court/Venue/City navigations)
│   └── SportBook.Infrastructure/   # unchanged (no migration)
└── tests/
    ├── SportBook.UnitTests/        # status-filter predicate over materialized rows (Sqlite);
    │                                # ToQueryString guard (filter + Include translate, no client eval)
    └── SportBook.IntegrationTests/ # enriched fields present; filter correctness across pages

frontend/
├── src/
│   ├── pages/
│   │   ├── my-bookings/            # filter tabs (All/Upcoming/Completed/Cancelled), Prev/Next
│   │   │                            # pagination (reset to page 1 on filter change), richer rows
│   │   │                            # (venue, city, sport, court)
│   │   └── owner-bookings/         # same enriched booking row detail (shared shape)
│   ├── entities/
│   │   └── booking/                # Booking type gains venueName/city/sport/courtName; the
│   │                                # bookings API call gains an optional status arg
│   └── shared/
│       └── i18n/                   # + filter-label / no-bookings-in-filter keys in en/uk/pt
└── tests/                          # filter tabs, pagination + reset, enriched-row rendering
```

**Structure Decision**: Same two-project layout as 001-004. The enriched fields ride the existing
shared `BookingResponse`, so both booking lists gain the detail from one DTO change; the status
filter and paging UI live on the customer `my-bookings` page (the owner page keeps its current
behavior aside from the shared richer row). The filter is a server-side predicate on the existing
`ListMineAsync`, keeping filtering and paging consistent on one query.

## Complexity Tracking

Not applicable - Constitution Check has no gates to violate (constitution.md is unfilled).
