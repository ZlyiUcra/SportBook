# Implementation Plan: Geolocation-centered radius map of nearby venues

**Branch**: `003-venue-radius-map` | **Date**: 2026-07-19 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/003-venue-radius-map/spec.md`

## Summary

Add a reference-point radius view to the venue search experience. A customer's device location
(obtained through an explicit "near me" action) or their explicitly selected directory city
becomes the centre of a fixed 75 km search. The map shows the in-range venues, grouped into
clusters that expand on zoom, with the nearest venue emphasized and the whole set auto-framed to
fit the screen; the results list below shows the same venues, nearest first. When there is neither
a granted location nor a selected city, no map is shown at all. The backend adds one endpoint that
computes distance from the reference point to venues-with-coordinates entirely in C# (reusing the
existing haversine, no trigonometry pushed into SQL, unit-testable on Sqlite). The frontend adds a
shared geolocation hook, a "near me" action, marker clustering, and fit-bounds/emphasis on the
existing map wrapper. All contested choices were decided by the consilium recorded in
`.specify/consilium/2026-07-19-venue-radius-map.md`; this plan turns them into design artifacts.

## Technical Context

**Language/Version**: C# 14 / .NET 10 backend; TypeScript 6.0 frontend - unchanged from 001/002.

**Primary Dependencies**:
- Backend (NuGet): no new packages. Distance is the existing pure `CityDistance.DistanceKm`
  haversine reused over materialized venues; the consilium rejected pushing trigonometry into SQL
  (it does not translate on the Sqlite unit-test provider and would quarantine the logic to
  integration-only tests, the NetTopologySuite tradeoff already avoided for cities).
- Frontend (yarn), user-approved via the clustering choice in /speckit-clarify:
  `react-leaflet-cluster` 4.1.3 (thin React-19/react-leaflet-5 binding - its peer deps are
  react-leaflet ^5.0.0, react ^19.0.0, @react-leaflet/core ^3.0.0, matching the repo's 5.0.0 /
  19.2.7 / 3.0.0), which pulls `leaflet.markercluster` 1.5.3 transitively (~10 KB gz JS + a small
  CSS), plus dev-only `@types/leaflet.markercluster` 1.5.6. All of it is imported only inside the
  already-lazy `shared/ui/map/MapView` chunk, so it does not touch any initial route bundle.

**Storage**: Same SQL Server / EF Core as 002. NO schema change and NO migration - the feature
reuses the existing nullable `Venue.Latitude`/`Longitude`. The nearby query materializes the
coordinate-bearing venue rows (SQL `WHERE Latitude != null`, trivially translatable) and does the
haversine distance, 75 km filter, distance ordering and cap in C#; the LINQ-to-SQL rule from 001
stands (the only server work is a null-coordinate filter, not trig).

**Testing**: xUnit as in 001/002 - the distance/order/cap over materialized rows gets a pure unit
test on the Sqlite path; a `ToQueryString()` assertion proves the coordinate-null filter
translates and that no trigonometry is pushed to SQL (client evaluation of the distance math is
intended and correct, since it runs over the small materialized candidate set, not the table);
integration tests own the endpoint's range validation and radius enforcement. Frontend: Vitest +
RTL with the map component mocked (no leaflet/clustering/WebGL in jsdom); the reference-point hook
and "near me" flow tested with mocked geolocation.

**Target Platform**: Unchanged - ASP.NET Core service + React SPA for evergreen browsers. The
browser Geolocation API requires a secure context (HTTPS; localhost qualifies), already satisfied.

**Project Type**: Web application (backend API + frontend SPA), unchanged.

**Performance Goals**: Spec SC-005 - the existing 500-concurrent search target is on a different
route (`GET /api/venues`); the new `GET /api/venues/nearby` is a separate path whose single-request
cost is the dominant concern. At ~204 venues (a subset with coordinates) an in-memory haversine
scan is sub-millisecond - the same in-process workload the city path already runs over ~5228 cached
rows - so no index or bounding-box prefilter is added now (a `Latitude`/`Longitude` index / bbox
prefilter is recorded as future work for when coordinate-bearing venues reach the low tens of
thousands). Spec SC-006 - zero growth of the initial JS chunk: the clustering libraries live only
in the lazy `MapView` chunk; verified by measuring `yarn build` output before/after.

**Constraints**: Contract-level MUSTs carried from the consilium verdict: the 75 km radius is a
server-side constant, not a client parameter; `lat`/`lng` are range-validated; the endpoint neither
persists nor logs received coordinates, and the client rounds device coordinates to 2 decimals
(~1.1 km) before sending. Map marker/popup content is rendered exclusively as react-leaflet JSX
children - the "nearest bigger" emphasis is a second `L.icon`, never `L.divIcon({ html })` fed from
venue fields (stored-XSS avoidance). The map obtains device location only via an explicit "near me"
action (no silent prompt on load). Attribution for OSM tiles and GeoNames stays on the About page.
ASCII-only source files per `CLAUDE.md`.

**Scale/Scope**: 1 new endpoint (`GET /api/venues/nearby`), 1 new response DTO (venue summary plus
`distanceKm`), no schema change, 1 new frontend dependency (clustering) confined to the lazy map
chunk, one new shared geolocation hook + reference-point resolver, a reshape of the venue search
page to the reference-point radius view (the map and the results list both driven by the in-range
set), and i18n additions in en/uk/pt. The 002 page-based results map (`VenueSearchMap`), the 002
`includeNearby` city-neighbor toggle, and the 002 "My city" button are superseded by this view
(see research.md).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

`.specify/memory/constitution.md` is still the unfilled bootstrap template - no principles have
been ratified, so there are no constitution gates to evaluate against and this gate trivially
passes, pre- and post-design. The standing recommendation from 001/002 to run
`/speckit-constitution` (the accumulated consilium briefings are ready source material) remains
open; not a blocker for this plan.

## Project Structure

### Documentation (this feature)

```text
specs/003-venue-radius-map/
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
│   ├── SportBook.Api/              # VenuesController: + GET /api/venues/nearby (lat, lng, sportType?)
│   ├── SportBook.Application/      # VenueService: + SearchNearbyAsync (in-memory haversine over
│   │                                # venues-with-coords, 75km fixed constant, distance-ordered,
│   │                                # capped); + NearbyVenueResponse DTO (summary + distanceKm)
│   ├── SportBook.Domain/           # unchanged (reuses Venue.Latitude/Longitude)
│   └── SportBook.Infrastructure/   # unchanged (no migration)
└── tests/
    ├── SportBook.UnitTests/        # haversine distance/order/cap over materialized rows;
    │                                # ToQueryString guard (null-coords filter translates, no trig)
    └── SportBook.IntegrationTests/ # nearby endpoint: range validation, fixed-radius enforcement,
                                     # distance ordering, auth

frontend/
├── src/
│   ├── pages/
│   │   └── venues/                  # search page reshaped to the reference-point radius view:
│   │                                # reference (near-me | selected city) -> nearby query ->
│   │                                # clustered map + distance-ordered list; no map when no ref
│   ├── features/
│   │   └── city-select/             # + "near me" action (device-location reference); the 002
│   │                                # "My city" button is superseded here
│   ├── entities/
│   │   └── venue/                   # + nearby API call + NearbyVenue type (summary + distanceKm)
│   └── shared/
│       ├── lib/                     # + useGeolocation hook (rounded coords + permission/error
│       │                            # state, extracted from MyCityButton's inline logic) and a
│       │                            # useReferencePoint resolver (geolocation -> city -> none)
│       ├── ui/map/                  # MapView gains: marker clustering (react-leaflet-cluster),
│       │                            # a fitBounds mode (useMap effect, fit-once-per-reference),
│       │                            # and per-marker emphasis (second L.icon for nearest)
│       └── i18n/                    # + near-me / clustering-empty-state keys in en/uk/pt
└── tests/                           # reference-point + near-me flow with mocked geolocation/map
```

**Structure Decision**: Same two-project web layout and layer rules as 001/002. Backend geo logic
stays a pure Application function + a translatable null-filter query (unit-testable on Sqlite);
engine-specific SQL is untouched (no migration). Frontend keeps the single `shared/ui/map/MapView`
wrapper as the only leaflet/clustering consumer (extended, not forked), and factors the two
reference behaviours (device location, selected city) into one `useReferencePoint` selector so the
map and the results list read the same source of truth.

## Complexity Tracking

Not applicable - Constitution Check has no gates to violate (constitution.md is unfilled).
