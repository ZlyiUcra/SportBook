# Phase 0 Research: City Selection, Geolocation and Venue Map

All items below were decided during the consilium of 2026-07-18 (transfer artifact
`.specify/consilium/2026-07-18-city-geolocation-map.md`) - most of them measured or interrogated
rather than assumed. User-confirmed decisions are marked as such; the Technical Context carries no
remaining NEEDS CLARIFICATION markers.

## Coordinate modeling: plain decimals vs spatial types

- **Decision**: Plain `decimal(9,6)` latitude/longitude columns on both `City` and `Venue`;
  distance math is a pure C# haversine function in Application. No
  `NetTopologySuite`/`geography`, no spatial indexes.
- **Rationale**: The whole spatial workload is "which of ~3-6k cities are within 150 km" over a
  reference table - measured on the real GeoNames UA subset (3,815 rows at population >= 1000):
  worst-case neighbor set is 722 cities, full pairwise scan is millions of cheap float ops done
  once and cached. `geography` columns would add a dependency chain AND quarantine all spatial
  logic to integration-only testing, because the Sqlite in-memory unit-test provider does not
  support SqlServer geography - breaking the established unit-test path for zero measurable gain
  at this scale. A pure function is unit-testable and has two consumers (nearest-city, neighbor
  expansion).
- **Alternatives considered**: NetTopologySuite + `geography` + spatial index (rejected - cost
  accounting above; also SQL Server only uses spatial indexes in narrow query forms, an easy
  silent-full-scan trap); precomputed neighbor edge table (~1.36M rows for UA - rejected, measured
  unnecessary; revisit only if neighbor sets exceed ~2-3k IDs, unreachable with a 150 km cap on
  UA data).

## Nearby-cities computation shape

- **Decision**: Two-step: (1) Application computes the neighbor city-ID set for the selected city
  from the in-memory city list (bounding-box prefilter + exact haversine, pure C#), cached
  indefinitely per city (reference data changes only by migration); (2) the venue query filters
  `cityIds.Contains(v.CityId)` - EF translates this to an OPENJSON parameter on SqlServer and it
  stays translatable on the Sqlite test provider. The 150 km radius is a server-side constant.
- **Rationale**: Performance-interrogated: max 722 IDs = ~6KB parameter, sub-ms parse; the filter
  works on an FK column that gets an index by EF convention (the legacy string filter was an
  unindexed scan, so the hot search path gets cheaper, not slower). Keeps ORDER BY Name - the
  SC-005-proven shape - with an optional cheap "selected city first" ranking term.
- **Alternatives considered**: SQL-side haversine over all venues (rejected - must be verified
  against client evaluation on every EF upgrade, harder to unit-test); persisted neighbor table
  (rejected above); returning the whole city list to the browser to filter client-side (rejected -
  violates the server-computes-truth stance and ships ~50KB+ nobody asked for).

## City reference data source and threshold

- **Decision**: GeoNames dumps (CC BY 4.0), UA subset, feature class P (populated places),
  population >= 500. Localized names (UK, PT) from the per-country alternatenames file; region
  (admin1) display names resolved the same way for suggestion disambiguation. A one-time dev
  script converts the dump into a committed data file; the script's first step prints actual row
  counts at thresholds 500/1000/5000 to confirm the threshold choice on real numbers (the
  convocation caught a 2x divergence between estimated and measured counts - the numbers decide,
  not the estimates).
- **Rationale**: User-confirmed. GeoNames carries geonameid/name/asciiname/alternatenames/
  country/admin1/lat/lng/population - everything the feature needs including UK/PT names matching
  the app's three locales without inventing translations. Threshold 500 keeps small-settlement
  users able to find their own place in the autocomplete (the dropdown answers "where the user
  is", not "where venues are"); population-DESC ranking keeps villages below cities in
  suggestions.
- **Alternatives considered**: cities1000/cities5000 cuts (rejected - a user in a small settlement
  cannot find themselves, list reads as broken); worldwide list (rejected - inventory without a
  second market); live geocoding via Nominatim (rejected - 1 req/s policy makes per-keystroke use
  a violation waiting for a block; nothing needs runtime geocoding); IP-based geolocation
  (rejected - ships user IPs to a third party, a data-sharing decision nobody made).

## Seeding mechanism

- **Decision**: The committed city data file is an embedded resource of the Infrastructure
  project; the "create + seed Cities" EF migration reads it in `Up()` and emits deterministic
  INSERT batches. Never `HasData`, never a runtime/startup call to external services.
- **Rationale**: Keeps schema and its reference data in one ordered, versioned history (migration
  chain) instead of a second parallel seeding mechanism with its own versioning. `HasData` loses
  twice: it bloats the model snapshot with thousands of rows AND would pour them into every
  Sqlite unit-test database (`EnsureCreated()` applies model seed data; unit fixtures seed only
  what each test needs). Integration tests already run `Database.Migrate()` once per container -
  they get real city rows for free.
- **Alternatives considered**: `HasData` (rejected above); idempotent startup seeder with a
  version marker (acceptable fallback per the convocation, but second choice - a parallel
  versioning mechanism); runtime download (rejected outright - external call at runtime,
  non-deterministic builds).

## Venue.City migration strategy

- **Decision**: Match-or-fail, NOT NULL end state, three migrations inside this feature:
  (1) create + seed `Cities`; (2) add nullable `Venue.CityId` FK + `Latitude`/`Longitude`,
  backfill by exact string match against city names, then a guard - `IF EXISTS (... CityId IS
  NULL) THROW` listing the unmatched values - then ALTER to NOT NULL; (3) drop the legacy
  `Venue.City` string column (separate migration, same feature - the compromise that closed the
  pragmatist-vs-architect drop-timing disagreement).
- **Rationale**: There is no production database and no committed seed data (verified by grep:
  the only seeding lives in test fixtures) - dev rows are hand-made and few, so a loud
  transactional fail listing mismatches is minutes of manual fixing, while interim-nullable
  would be permanent hidden debt. Migrations on SqlServer are transactional: a guard failure
  rolls back cleanly; fresh databases (integration host, new dev) pass the guard trivially.
- **Alternatives considered**: Interim nullable + follow-up NOT NULL migration (rejected - only
  justified when a production database exists); resolving free-text city server-side on write
  (rejected - keeps the unmatched-city queue growing forever, see contract decision below).

## Write path moves to cityId

- **Decision**: `CreateVenueRequest`/`UpdateVenueRequest` replace `city: string` with
  `cityId: int` (validated to exist), plus an optional `latitude`/`longitude` pair (both-or-
  neither, range-validated). The owner form uses the same city combobox as search. The 002
  contract explicitly supersedes the 001 venue endpoints.
- **Rationale**: Interrogation-confirmed blocker: with search filtering on `CityId`, a write path
  still accepting free text would either create venues invisible to city-scoped search (null
  CityId) or silently re-grow the free-text problem the feature exists to kill. The combobox
  makes invalid city input unrepresentable in the UI; the server still validates.
- **Alternatives considered**: Server-side resolution of free-text city on write (rejected -
  unmatched inputs need a 400 anyway, so the client might as well send an ID); keeping 001's
  write shape untouched (rejected - guarantees falsely documented endpoints).

## Map library and loading boundary

- **Decision**: Leaflet + react-leaflet v5 behind a single typed wrapper in `shared/ui/map`
  (props: center, markers, onSelect/onPick) - the only module importing leaflet - loaded
  exclusively via `React.lazy`/dynamic `import()`; `leaflet.css` and the marker asset (through
  the Vite asset pipeline) live in that chunk. Verification: measure `yarn build` output before/
  after - initial chunk delta must be 0.
- **Rationale**: User-approved dependencies. Leaflet is ~42KB gz vs MapLibre GL ~290KB gz (~7x)
  with no WebGL need; react-leaflet v5 is the maintained React-19 binding. The repo has zero code
  splitting today, so the boundary must be the component, not a route: the map sits on the `/`
  search page, and a synchronous import would put ~50KB gz into the landing chunk (estimated
  +22-28% - the build measurement replaces the estimate). Whether the search map loads on mount
  or on toggle is a product choice; both satisfy the boundary.
- **Alternatives considered**: MapLibre GL (rejected - weight without justification); route-level
  splitting only (rejected - the map's route IS the landing route); marker clustering (rejected -
  speculative below a few hundred markers; pins are capped at the page size of 100).

## Map content safety

- **Decision**: Contract MUST: popup/tooltip content is rendered exclusively as react-leaflet JSX
  children; `bindPopup`/`bindTooltip`/`setContent` with strings and `L.divIcon({ html })` fed
  from venue fields are forbidden.
- **Rationale**: `Venue.Name`/`Description` are unvalidated user input; Leaflet renders string
  popup content as HTML by design (the disputed CVE-2025-69993 is beside the point - it is our
  stored XSS if we pipe user strings in). React escaping makes the JSX path safe by default; the
  likeliest future drift point is `divIcon({ html })` from custom-marker examples, hence the
  explicit ban now, before the first marker exists.
- **Alternatives considered**: Sanitizing HTML popups (rejected - adds a sanitizer dependency to
  allow a pattern nothing needs).

## Geolocation privacy posture

- **Decision**: Contract MUSTs: the client rounds device coordinates to 2 decimal places
  (~1.1 km) before calling the nearest-city endpoint; the server resolves the nearest city and
  neither persists nor logs the received coordinates. Detection is optional, browser-permission
  gated, and failure degrades to manual selection.
- **Rationale**: Interrogation-verified: nothing in the current deployment persists query strings
  (no proxy, no HTTP logging), so this is spec-level hygiene, not an active leak - but GET query
  coordinates would land in any future proxy/LB access log, so the cheap constraint ships with
  the contract. 2 decimals is the sweet spot by mechanism: max ~0.66 km position shift keeps
  nearest-city flips confined to genuine near-tie boundary cases, while 1 decimal (~6.6 km max
  shift) would misassign cities exactly where the feature matters most - dense agglomerations
  (neighboring-city centers 5-15 km apart).
- **Alternatives considered**: Sending precise coordinates (rejected - needless precision);
  1-decimal rounding (rejected - breaks agglomeration correctness); POST instead of GET (not
  required once rounding + no-persist hold; can be revisited with a future logging decision).

## Tile provider

- **Decision**: Development and demos use the public OSM tile server with the required visible
  attribution; the tile URL and attribution are constants in `shared/config` (single switch
  point). Choosing a keyed, Origin-restricted provider is a recorded open item with the deadline
  "before production release"; the risk that the community server may block heavy/commercial use
  without notice is accepted in writing for pre-production. User-confirmed 2026-07-18.
- **Rationale**: Zero keys and zero cost while the product is pre-production; the OSM tile usage
  policy explicitly tolerates light use with attribution; switching later costs one constant.
- **Alternatives considered**: Keyed provider now (rejected - key management without a production
  need); bundling/prefetching tiles (rejected - violates OSM policy).

## City DTO localization

- **Decision**: `CityResponse` carries all three localized names (and region display names); the
  client picks by active locale. Flat name columns on the entity (`NameEn`/`NameUk`/`NamePt`),
  filled offline by the dataset conversion script - no generic translation table.
- **Rationale**: Three fixed locales are an app-level constant (001 decision); a generic
  translation table is abstraction without a second consumer. Client-side picking matches the
  existing i18n architecture (no server-side locale negotiation anywhere).
- **Alternatives considered**: Server-side name selection via Accept-Language (rejected - would
  introduce locale negotiation the app does not otherwise have); separate CityTranslations table
  (rejected - normalization without need at 3 fixed locales).

## Country handling

- **Decision**: No `Country` table. `City.CountryCode` is the single source of truth; v1 data is
  UA-only and no country selector ships.
- **Rationale**: User-confirmed ("derived"). A country table duplicating a constant of the
  cities table is the anti-pattern the convocation named; deriving later costs ~1 hour.
- **Alternatives considered**: Country entity + selector (rejected - single-country coverage).

## City selection UI

- **Decision**: shadcn/ui combobox backed by `cmdk` (user-approved dependency), server-side
  suggestions: debounce 250-300 ms, minimum 2 characters, TOP ~10 ordered by population DESC,
  matching against all localized name columns; suggestions show region context to disambiguate
  same-named settlements. The full city list is never shipped to the browser.
- **Rationale**: User-confirmed cmdk over a hand-rolled combobox (keyboard navigation and a11y
  for ~6KB gz). Server-side filtering keeps the payload tiny and the ranking rule in one place.
- **Alternatives considered**: Hand-rolled combobox (offered, rejected by user); client-side
  filtering over a downloaded list (rejected - ~50-60KB gz shipped for no reason).

## Deliberately out of scope (recorded for tasks/spec traceability)

Zones/area drawing on the map (no semantics, no stated need), Country table, NetTopologySuite,
marker clustering, IP geolocation, live geocoding, persisted neighbor edge table, worldwide city
coverage - all rejected with rationale in the consilium artifact's "Свідомо не робимо" section.
