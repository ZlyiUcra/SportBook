# Implementation Plan: City Selection, Geolocation and Venue Map

**Branch**: `002-city-geolocation-map` | **Date**: 2026-07-18 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/002-city-geolocation-map/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Replace free-text venue city with a structured City reference table (Ukrainian settlements seeded
from GeoNames), move both search and the venue write path to `cityId`, add nearest-city detection
from browser geolocation, optional expansion of search to cities within 150 km, and a lazily
loaded map: venue pins on the search page, an owner-placed location pin in the venue form, and a
single-marker map on the venue page when a pin is set. Scope, privacy posture, and all contested
technical choices were decided and user-confirmed by the consilium recorded in
`.specify/consilium/2026-07-18-city-geolocation-map.md`; this plan turns those decisions into
design artifacts.

## Technical Context

**Language/Version**: C# 14 / .NET 10 backend; TypeScript 6.0 frontend - unchanged from 001.

**Primary Dependencies**:
- Backend (NuGet): no new packages. Coordinates are plain `decimal` columns and distance math is a
  pure C# function - `NetTopologySuite`/`geography` was consciously rejected (it would quarantine
  spatial logic away from the Sqlite unit-test path; see research.md). EF Core 10 + SqlServer
  provider as in 001.
- Frontend (npm/yarn), all four user-approved 2026-07-18: `leaflet` (~42KB gz, raster map engine),
  `react-leaflet` v5 (~4-10KB gz, React 19 binding - repo runs React 19.2.7, compatible),
  `@types/leaflet` (dev-only), `cmdk` (~6KB gz, command palette primitive required by the
  shadcn/ui combobox used for city selection). No other additions; MapLibre GL (~290KB gz) was
  rejected for weight without a WebGL justification.

**Storage**: Same SQL Server 2025 via EF Core as 001. New `Cities` reference table (~3-6k rows,
UA subset of GeoNames, population >= 500 - final threshold confirmed against actual dataset counts
by the seed tooling), localized name columns (EN/UK/PT) plus region display names for
disambiguation, `decimal(9,6)` latitude/longitude. `Venues` gains `CityId` (FK, NOT NULL at the
end of the migration chain via a match-or-fail transactional migration) and a nullable
`Latitude`/`Longitude` pair; the legacy free-text `City` column is dropped in a separate follow-up
migration inside this feature. Seeding is a committed data file compiled into the Infrastructure
migration (never `HasData` - it bloats the model snapshot and would leak thousands of rows into
every Sqlite unit-test database; never a runtime call to external services). Nearby expansion is
two-step: neighbor city IDs computed in Application from the in-memory city list (pure haversine,
cacheable indefinitely - reference data), then a translatable `Contains` filter on `Venue.CityId`
(OPENJSON parameter on SqlServer; measured worst case 722 neighbor IDs on real GeoNames data,
comfortably below any flip-to-edge-table threshold). LINQ-only/no-raw-SQL rules from 001 stand;
raw SQL appears only inside the Infrastructure migration (backfill + guard), which is the
established Infrastructure-only exception.

**Testing**: xUnit as in 001 - haversine/neighbor selection get pure unit tests (Sqlite-friendly
path); `ToQueryString()` assertion proves the `Contains` filter translates instead of
client-evaluating; integration tests own SqlServer-specific behavior and re-verify search after
the query-shape change. Frontend: Vitest + RTL; the map component is mocked in form/page tests
(no leaflet/WebGL in jsdom - at most a thin smoke test), city combobox and geolocation flows
tested with mocked APIs.

**Target Platform**: Unchanged - cross-platform ASP.NET Core service + React SPA for evergreen
browsers. Browser Geolocation API requires a secure context (HTTPS; localhost qualifies), which
the deployment posture already satisfies.

**Project Type**: Web application (backend API + frontend SPA), unchanged.

**Performance Goals**: Spec SC-005 - the existing 500-concurrent search target must still hold
after the query-shape change (re-run the 001 load scenario as verification; the `CityId` FK filter
gains an index by EF convention where the old string filter was an unindexed scan, so the path is
expected to get cheaper, not slower). Spec SC-006/FR - zero growth of the initial JS chunk: the
map stack loads only via dynamic `import()` (the repo currently has zero code splitting, so a
synchronous import anywhere in the search page would drag ~50KB gz into the landing chunk);
verified by measuring `yarn build` output before/after. Spec SC-007 - city suggestions within 1s:
debounced 250-300ms, min 2 chars, TOP ~10 ordered by population server-side.

**Constraints**: Contract-level MUSTs carried from the consilium verdict: map popup/tooltip
content is rendered exclusively as react-leaflet JSX children - `bindPopup`/`bindTooltip`/
`setContent` with strings and `L.divIcon({ html })` fed from venue fields are forbidden
(Venue.Name/Description are unvalidated user input; any raw-HTML render is stored XSS). The
client rounds device coordinates to 2 decimal places (~1.1 km) before calling the nearest-city
endpoint and the server neither persists nor logs received coordinates. All new endpoints stay
behind the global fallback authorization policy - no `[AllowAnonymous]`. Latitude/longitude
inputs validated to legal ranges; the 150 km nearby radius is a server-side constant not
influenced by clients. Attribution for GeoNames (CC BY 4.0) and OSM tiles lives on the About
page. ASCII-only source files per `CLAUDE.md`.

**Scale/Scope**: One new reference table (~3-6k rows), 2 new endpoints (city autocomplete +
nearest city), a breaking reshape of the 5 venue endpoints' DTOs (documented as superseding 001's
contract), 3 migrations, one new FSD entity (`city`) + one feature slice (`city-select`) + one
lazily loaded map component, i18n additions in 3 locales.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

`.specify/memory/constitution.md` is still the unfilled bootstrap template (no principles have
been ratified for this project yet) - there are no constitution gates to evaluate against, so this
gate trivially passes, pre- and post-design. The standing recommendation from 001 to run
`/speckit-constitution` remains open; the accumulated consilium briefings are ready source
material. Not a blocker for this plan.

## Project Structure

### Documentation (this feature)

```text
specs/002-city-geolocation-map/
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
│   ├── SportBook.Api/              # + CitiesController (autocomplete, nearest)
│   ├── SportBook.Application/      # + CityService (suggest, nearest, neighbor-set), haversine as a
│   │                                # pure function; VenueService reshaped to cityId + includeNearby;
│   │                                # City DTOs; Venue DTOs gain city object + nullable coordinates
│   ├── SportBook.Domain/           # + City entity; Venue gains CityId FK + nullable Latitude/Longitude
│   └── SportBook.Infrastructure/   # + 3 migrations (create+seed Cities; add Venue.CityId match-or-fail
│                                    # + coordinates; drop legacy Venue.City), committed city data file
│                                    # as an embedded resource of the seed migration
└── tests/
    ├── SportBook.UnitTests/        # haversine, neighbor selection, suggestion ranking, DTO mapping,
    │                                # ToQueryString translation guard
    └── SportBook.IntegrationTests/ # cities endpoints, venue search by cityId/includeNearby, venue
                                     # create/update with cityId + coordinate validation

frontend/
├── src/
│   ├── pages/
│   │   └── venues/                  # search page: city combobox + nearby toggle + lazy map section;
│   │                                # venue detail: lazy single-marker map when pin set
│   ├── features/
│   │   ├── city-select/             # combobox (shadcn+cmdk), "my city" geolocation button, nearby
│   │   │                            # toggle; model owns rounding-before-send
│   │   └── venue-management/        # VenueForm: city combobox reuse + lazy pin-picker on the map
│   ├── entities/
│   │   └── city/                    # City types, api calls (suggest, nearest), locale name pick
│   └── shared/
│       ├── ui/map/                  # MapView wrapper (typed props: center, markers, onSelect/onPick),
│       │                            # the ONLY module importing leaflet/react-leaflet; loaded solely
│       │                            # via React.lazy/dynamic import; leaflet.css lives in this chunk
│       ├── config/                  # tile URL + attribution constants (single switch point for a
│       │                            # future keyed provider)
│       └── i18n/                    # + city/map/geolocation keys in en/uk/pt
└── tests/                           # combobox + form tests with mocked map component
```

**Structure Decision**: Same two-project web layout and layer rules as 001. Backend geo logic
lands in Application as pure functions + LINQ (unit-testable on Sqlite), engine-specific SQL stays
inside Infrastructure migrations. Frontend follows the existing FSD slices: a new `entities/city`
and `features/city-select` mirror the established auth/venue slices; the map wrapper sits in
`shared/ui/map` because it has two real consumers (search page map + venue form pin-picker + venue
detail marker) and pages must depend on our typed wrapper, not on leaflet types.

## Complexity Tracking

Not applicable - Constitution Check has no gates to violate (constitution.md is unfilled).
