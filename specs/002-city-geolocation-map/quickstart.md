# Quickstart: City Selection, Geolocation and Venue Map

Validation guide for proving the feature works end-to-end once implemented. Not implementation
code - see `contracts/api.md` for exact request/response shapes and `data-model.md` for entities
and the migration chain. Prerequisites, setup, and run commands are unchanged from
`specs/001-sportbook-venue-booking/quickstart.md`; this feature only adds two frontend steps:
`yarn install` after the new dependencies land, and the build measurement below.

## Migration and seed validation

```powershell
# From backend/ - applies CreateAndSeedCities, AddVenueCityIdAndCoordinates, DropVenueLegacyCity
dotnet ef database update --project src/SportBook.Infrastructure --startup-project src/SportBook.Api
```

Expected outcomes:

1. `Cities` exists and holds the seeded UA subset (~3-6k rows; the exact count is printed by the
   dataset conversion script and must match what lands in the table).
2. On a dev database with venues whose legacy `City` text matches no directory city, the
   match-or-fail migration THROWS listing the unmatched values, and rolls back cleanly - fix the
   listed venue rows manually and re-run. On a fresh database the guard passes silently.
3. After the chain: `Venues.CityId` is NOT NULL for every row, `Venues.City` no longer exists.

## API validation scenarios

Run with an authenticated token (all endpoints require auth - verify an unauthenticated request
gets 401 on every new endpoint).

### 1. City autocomplete (US1)

- `GET /api/cities?query=ky` - returns at most 10 cities including Kyiv, larger populations
  first; same city findable via `query=Киї` (UK) and the PT name.
- `GET /api/cities?query=k` - 400 (below 2-character minimum).
- Same-named settlements each carry distinct region names in the response.

### 2. Nearest city (US3)

- `GET /api/cities/nearest?lat=50.45466&lng=30.5238` (Kyiv's own GeoNames coordinates) - returns
  Kyiv. Note: a nearby-but-not-exact point can legitimately resolve to a finer-grained
  neighborhood/sub-locality instead (e.g. "Stare Misto") - the directory is not limited to
  top-level city labels.
- `lat=91` or `lng=181` - 400.
- Confirm no server log line contains the received coordinates.

### 3. Search by city and nearby (US1, US4)

- Venues in a city: `GET /api/venues?cityId=<kyiv>` returns only that city's venues, each with a
  nested `city` object.
- `includeNearby=true` adds venues from cities within 150 km (e.g. an Irpin venue appears for
  Kyiv) and never from beyond 150 km (e.g. a Lviv venue does not).
- `includeNearby=true` without `cityId` changes nothing.

### 4. Venue write path (US2)

- `POST /api/venues` with a valid `cityId` succeeds; with an unknown `cityId` - 400; the legacy
  `city` string field is rejected/ignored per contract.
- `latitude` without `longitude` - 400; both present but out of range - 400; both valid - the
  venue detail returns them; update omitting both clears the pin.

## Frontend validation scenarios (manual, via `yarn dev`)

1. **Combobox (US1)**: typing 2+ characters in any app locale suggests cities with region
   context; free text cannot be submitted as a city.
2. **My city (US3)**: with location permission granted, the nearest city pre-fills and can be
   overridden; with permission denied, manual selection continues with no blocking error. In
   devtools Network, the `nearest` request's `lat`/`lng` have exactly 2 decimal places.
3. **Search map (US5)**: open the map on the search page - pins appear only for current-page
   venues that have coordinates; clicking a pin leads to the venue; a venue whose name contains
   `<b>test</b>` shows the literal text in its popup, not bold markup.
4. **Owner pin (US2)**: in the venue form, place, move, and remove the pin; verify the venue
   page shows the single-marker map only while a pin is set - no map block otherwise.
5. **Lazy chunk (SC-006)**: in devtools Network, the leaflet chunk loads only when a map first
   renders - not on login, booking, or dashboard pages.

## Build and performance verification

```powershell
# From frontend/ - run BEFORE the map lands and again AFTER; compare gzip sizes
yarn build
```

- Initial JS chunk delta must be 0: leaflet/react-leaflet/leaflet.css live in a separate lazy
  chunk (research.md loading-boundary decision).
- Re-run the 001 SC-005 load scenario (500 concurrent venue searches) against the reshaped
  query - the p95 target must still hold (the `CityId` FK filter is indexed where the legacy
  string filter was a scan, so no regression is expected; the run is the proof).

## Automated tests

```powershell
# Backend (from backend/): unit (Sqlite in-memory) + integration (real SQL Server container)
dotnet test

# Frontend (from frontend/)
yarn test
```

Must include: haversine + neighbor-set unit tests, `ToQueryString()` translation guard for the
`CityId IN` filter, cities endpoint integration tests, venue search/write integration tests for
the reshaped DTOs, and combobox/form tests with the map component mocked (no leaflet in jsdom).
