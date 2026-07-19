# Phase 0 Research: Geolocation-centered radius map of nearby venues

All items were decided during the consilium of 2026-07-19 (transfer artifact
`.specify/consilium/2026-07-19-venue-radius-map.md`) and refined by /speckit-clarify
(2026-07-19). User-confirmed decisions are marked. No NEEDS CLARIFICATION markers remain.

## Distance computation: in-memory C# haversine, not SQL trigonometry

- **Decision**: The nearby query materializes the coordinate-bearing venues via a translatable SQL
  filter (`WHERE Latitude != null`) and computes distance, the 75 km filter, distance ordering and
  the cap in C# using the existing pure `CityDistance.DistanceKm`. No trigonometry is pushed into
  SQL.
- **Rationale**: The city path already runs the harder version of this workload - haversine over
  the ~5228-row cached city list in-process (`CityDistance.cs`, "no database round-trip"). The
  coordinate-bearing subset of ~204 venues is a handful of rows by comparison. Pushing
  `SIN/COS/ATN2 ORDER BY` into SQL does not translate on the Sqlite unit-test provider, which would
  quarantine venue-distance logic to integration-only tests - exactly the NetTopologySuite tradeoff
  the codebase deliberately avoided for cities. Keeping the math in C# leaves it unit-testable on
  Sqlite with zero database.
- **Alternatives considered**: `EF.Functions`/raw-SQL trig (rejected - Sqlite-untranslatable,
  breaks the unit-test path, no perf gain at this scale); a bounding-box `Latitude/Longitude
  BETWEEN` prefilter before haversine (rejected as premature at 204 rows - recorded as future work
  with the index below); NetTopologySuite/`geography` (rejected - dependency weight and the same
  Sqlite quarantine, for a workload the pure function handles).

## Marker clustering library

- **Decision**: `react-leaflet-cluster` 4.1.3, which wraps `leaflet.markercluster` 1.5.3
  (pulled transitively) and exposes a React `<MarkerClusterGroup>` component; dev-only
  `@types/leaflet.markercluster` 1.5.6.
- **Rationale**: User-confirmed clustering in /speckit-clarify (fewer markers at default zoom,
  expanding on zoom-in). Verified on npm that 4.1.3's peer deps are react-leaflet ^5.0.0,
  react ^19.0.0, @react-leaflet/core ^3.0.0 - matching the repo's exact versions (5.0.0 / 19.2.7 /
  3.0.0), so no compat gap. `leaflet.markercluster` is the canonical, evergreen Leaflet plugin
  (~10 KB gz JS + a small CSS). All of it is imported only inside the already-lazy `MapView` chunk,
  so the initial route bundle is unaffected (spec SC-006).
- **Alternatives considered**: Integrating `leaflet.markercluster` directly via a `useMap` effect
  without the React wrapper (viable, keeps deps minimal, but re-implements what
  `react-leaflet-cluster` already provides for React 19 - more code for no gain); no clustering /
  natural marker spread (the board's default, but explicitly overridden by the user's clarify
  choice).

## Nearby endpoint shape

- **Decision**: `GET /api/venues/nearby?lat={lat}&lng={lng}&sportType={sportType?}`, authenticated,
  returns a flat `NearbyVenueResponse[]` - the venue summary fields plus a `distanceKm` - ordered by
  ascending distance and capped at the nearest 100. The reference point is resolved on the client
  (device location or selected city coordinates) and passed as `lat`/`lng`; the server does not need
  to know which source it came from.
- **Rationale**: One point-in endpoint serves both reference tiers (near-me and selected-city) since
  the client already holds the city's coordinates (`CityResponse.latitude/longitude`). `distanceKm`
  lets the client mark the nearest venue and order the list without re-deriving distance in JS
  (single source of truth for "nearest"). The cap bounds the payload; at 204 venues total a 75 km
  circle rarely holds more than a handful, and 100 is the same ceiling as the existing page size.
- **Alternatives considered**: Extending `GET /api/venues` with `lat`/`lng` (rejected - overloads a
  paged, city-filtered endpoint with different semantics: unpaged, distance-ordered, fixed radius);
  returning no distance and re-computing nearest in JS (rejected - duplicates the haversine on the
  client and risks tie-break/rounding divergence).

## Fixed 75 km radius - constant home

- **Decision**: A `const decimal VenueRadiusKm = 75` lives beside `VenueService` (the consumer),
  NOT on `CityDistance`. It is enforced server-side and is not a request parameter.
- **Rationale**: `CityDistance.NearbyRadiusKm = 150` is a city-to-city neighbor radius on a class
  about the city directory; the venue point-radius is a different concept. Two distance constants
  with the numeral 150/75 must be visually un-confusable, so they live in different, purpose-named
  homes (consilium finding). Server-side enforcement prevents a client from widening it into a
  full-table distance-scan primitive.
- **Alternatives considered**: Reusing/parameterising `CityDistance.NearbyRadiusKm` (rejected -
  concept-mixing and the naming-collision trap); a client-supplied radius (rejected - resource-burn
  and the fixed-radius product decision).

## Reference-point resolution and geolocation

- **Decision**: One `useReferencePoint` selector is the single source of truth for the center,
  resolving by precedence: (1) device location obtained through an explicit "near me" action,
  (2) the explicitly selected directory city's coordinates, (3) none -> no query, no map. A shared
  `useGeolocation` hook owns `getCurrentPosition`, the 2-decimal rounding, and the
  permission/denied/error state; it is extracted from the inline logic currently living in
  `MyCityButton`. The "near me" action supersedes the 002 "My city" button as the geolocation
  entry point for this feature.
- **Rationale**: The board flagged that `MyCityButton` discards the raw device coordinates (it emits
  only a resolved `City`), and that duplicating the permission/error state machine or computing
  "do I have a reference" separately from "does the map have a center" is the exact duplication to
  avoid. One hook + one selector keep the map and the list reading the same center. Gating
  geolocation behind an explicit action avoids an ungestured permission prompt on page load (which
  browsers throttle) and matches the shipped click-to-locate pattern.
- **Alternatives considered**: Automatic `getCurrentPosition` on map load (rejected in clarify -
  silent prompt, browser-throttled; the user chose the explicit action); keeping `MyCityButton`
  and adding a parallel geolocation path (rejected - duplicated permission handling).

## fitBounds behaviour

- **Decision**: The map fits its bounds to all in-range pins once per reference-point change (a
  `useMap` effect keyed on the reference identity and the returned venue-id set), with a sensible
  maximum zoom so a tight cluster does not over-zoom. Manual zoom/pan afterwards is preserved -
  unrelated React re-renders do not re-fit.
- **Rationale**: react-leaflet's `MapContainer` reads `center`/`zoom` only at mount, so "frame all
  pins then keep the user's manual navigation" needs an imperative effect, not a prop. Keying the
  effect on the reference/venue-set (not every render) is what prevents the fit-vs-manual-zoom
  snap-back trap the board named.
- **Alternatives considered**: Re-fitting on every render (rejected - fights manual zoom);
  remounting the map via a changing `key` (rejected - destroys manual state and clustering on every
  update).

## Marker emphasis for the nearest venue

- **Decision**: The nearest venue uses a second, larger `L.icon` (distinct from the default marker
  icon), applied by a per-marker variant flag on `MapView`. Popups remain react-leaflet JSX
  children.
- **Rationale**: A size/label badge is tempting to build with `L.divIcon({ html })` fed from venue
  fields - that renders unvalidated `Venue.Name`/`Description` as HTML (stored XSS). A second
  `L.icon` (like the existing `defaultIcon`) achieves emphasis with no raw-HTML path.
- **Alternatives considered**: `divIcon({ html })` with venue text (rejected - XSS); a CSS class on
  a DOM marker fed from user text (same risk).

## What this supersedes on the search page

- **Decision**: The reference-point radius view supersedes three 002 pieces on the venue search
  page: the page-based `VenueSearchMap` (pins of the current results page) is replaced by the
  radius map; the `includeNearby` 150 km city-neighbor toggle is replaced by the 75 km point-radius
  model; and the "My city" button is replaced by the "near me" action. The `sportType` filter is
  kept and narrows the in-range set (passed to the nearby endpoint). When no reference point is
  active, the map is absent and the list shows a prompt to pick a city or use "near me".
- **Rationale**: The user's clarify answer unifies the map and the list around the reference point
  (both show the same in-range set), which is incompatible with a separate page-based map and a
  parallel city-neighbor toggle. Collapsing to one reference-driven model removes the divergent-set
  confusion the feature exists to fix.
- **Alternatives considered**: Keeping `includeNearby` and `VenueSearchMap` alongside the new map
  (rejected - two maps / two radii / divergent sets, the exact confusion); keeping "My city"
  (rejected - redundant with "near me" for this feature, and it discards the coordinates the radius
  view needs).

## No schema change

- **Decision**: No new table, column, or migration. The feature reuses the existing nullable
  `Venue.Latitude`/`Longitude` from 002.
- **Rationale**: Everything the radius view needs (venue coordinates, city coordinates) already
  exists. Distance is computed, not stored.
- **Alternatives considered**: A precomputed venue-distance or spatial column (rejected - distance
  is relative to a runtime reference point, not storable; and NTS/geography was already rejected).

## Deliberately out of scope (recorded for tasks/spec traceability)

Drawing/zones/polygons; a client-controllable radius; persisting or logging device coordinates;
automatic geolocation prompt on load; a `Latitude`/`Longitude` index or bounding-box prefilter (a
recorded future-work item for when coordinate-bearing venues reach the low tens of thousands, with
app-wide rate limiting revisited then); any map/list fallback when there is no reference point.
