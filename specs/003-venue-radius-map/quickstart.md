# Quickstart: Geolocation-centered radius map of nearby venues

Validation guide for proving the feature works end-to-end once implemented. Not implementation
code - see `contracts/api.md` for exact request/response shapes and `data-model.md` for the DTO and
the computation. Prerequisites, setup, and run commands are unchanged from
`specs/002-city-geolocation-map/quickstart.md`; this feature adds one frontend dependency install
(`yarn install` after `react-leaflet-cluster` + `@types/leaflet.markercluster` land) and no
database change (no migration).

## API validation scenarios

Run with an authenticated token (all endpoints require auth - verify an unauthenticated request
gets 401 on the new endpoint).

### 1. Nearby venues by point (US1/US2)

- Seed at least one venue with coordinates near Kyiv (e.g. `latitude=50.45`, `longitude=30.52`) and
  one far away (e.g. near Lviv).
- `GET /api/venues/nearby?lat=50.45&lng=30.52` - returns the Kyiv-area venue with a `distanceKm`
  near 0, ordered nearest first, and does NOT return the Lviv venue (beyond 75 km).
- Confirm the response items carry `distanceKm` and are sorted ascending.
- `GET /api/venues/nearby?lat=91&lng=30.52` or `lng=181` - 400.
- `GET /api/venues/nearby?lat=50.45&lng=30.52&sportType=Tennis` - returns only in-range venues with
  an active Tennis court.
- Confirm no server log line contains the received coordinates.

### 2. Fixed radius cannot be widened

- There is no radius parameter; confirm that adding an arbitrary query value does not change the
  75 km cut-off (a venue at ~80 km never appears).

## Frontend validation scenarios (manual, via `yarn dev`)

1. **Near me (US1)**: activate the "near me" action; with location permission granted, the map
   centers on the device position, shows the in-range venues clustered (a count bubble that expands
   into individual markers on zoom-in), emphasizes the nearest venue with a larger marker, and frames
   all pins on screen; the list below shows the same venues nearest-first. In devtools Network, the
   `nearby` request's `lat`/`lng` have exactly 2 decimal places.
2. **Selected city (US2)**: without granting location, pick a city; the map centers on that city with
   the same 75 km behavior and the same list.
3. **No reference (US3)**: deny location and select no city - no map block renders at all (not an
   empty map), and the list shows a prompt to pick a city or use "near me".
4. **Manual zoom preserved**: after the map frames the pins, zoom/pan; verify it is not snapped back
   by unrelated updates. Change the reference (pick a different city) and verify it re-frames.
5. **Clustering (US1)**: with several close venues, verify they render as a grouped count marker at
   default framing and separate into individual markers as you zoom in.
6. **XSS-safe popup**: a venue whose name contains `<b>test</b>` shows the literal text in its popup,
   not bold markup.
7. **Lazy chunk (SC-006)**: in devtools Network, the leaflet + clustering chunk loads only when a map
   first renders - not on login, booking, or dashboard pages.

## Build and performance verification

```powershell
# From frontend/ - run BEFORE the clustering libs land and again AFTER; compare gzip sizes
yarn build
```

- Initial JS chunk delta must be 0: leaflet, react-leaflet, react-leaflet-cluster,
  leaflet.markercluster and their CSS all live in the separate lazy `MapView` chunk (research.md).
- Single-request cost of `GET /api/venues/nearby` is sub-millisecond at the current venue count
  (in-memory haversine over the coordinate-bearing subset); confirm no full-table trig scan appears
  in the query plan (the only SQL work is the `Latitude != null` filter).

## Automated tests

```powershell
# Backend (from backend/): unit (Sqlite in-memory) + integration (real SQL Server container)
dotnet test

# Frontend (from frontend/)
yarn test
```

Must include: a unit test for the haversine distance/order/cap over materialized rows; a
`ToQueryString()` guard proving the `Latitude != null` (+ sport) filter translates and that no
trigonometry is pushed to SQL; integration tests for the nearby endpoint (range validation,
fixed-radius enforcement, distance ordering, auth); and reference-point/near-me frontend tests with
the map component and geolocation mocked (no leaflet/clustering in jsdom).
