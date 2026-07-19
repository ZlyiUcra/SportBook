# Phase 0 Research: Return-to-search navigation and viewport-synced venue list

All product-level choices were made by the user in the 2026-07-19 discussion (spec Assumptions).
This file pins the mechanisms. No NEEDS CLARIFICATION markers remain.

## Where the search state lives: in-memory Zustand store, no persistence

- **Decision**: A session-scoped Zustand store (the repo already uses Zustand for the session and
  theme stores) holding `city`, `sportType`, and the granted device coordinates; the reference
  point is derived from it with 003's precedence (device -> city -> none). Plain in-memory store -
  explicitly WITHOUT the `persist` middleware.
- **Rationale**: The 003 pain is that this state lives in `VenueSearchPage` component state and
  dies on unmount. Lifting it to a module-level store survives navigation while dying with the
  session - exactly spec FR-002/FR-006. The store lands in `pages/venues/model/` because the
  search page is its only consumer.
- **Alternatives considered**: `sessionStorage` (rejected - it IS storage; writing device
  coordinates there violates FR-006 even though it is session-scoped); URL query parameters
  (rejected - coordinates would leak into browser history and shareable URLs, and a city object
  does not round-trip through an id without a fetch-by-id endpoint that does not exist); React
  context above the router (rejected - more wiring than a store for the same lifetime, and the
  repo convention for cross-page state is already Zustand). Note: a full page reload clears the
  in-memory store; the spec only requires state NOT to outlive the session, so clearing earlier is
  compliant and privacy-safer.

## Back control: a link to the search route, not history navigation

- **Decision**: The venue page's "back to search" action is a plain link to the search route
  (`/`). Browser back works independently because the state lives in the store, not in history.
- **Rationale**: `navigate(-1)` depends on how the customer arrived - for a direct landing (shared
  link) it would leave the app or do nothing. A route link gives one predictable destination and
  the store decides what appears there: restored search, or the default no-reference prompt (spec
  FR-005). This also satisfies acceptance scenario 4 (browser back) with zero extra code.
- **Alternatives considered**: `navigate(-1)` (rejected - history-dependent, breaks for direct
  landings); scroll/viewport restoration (out of scope - the user chose default full-radius
  framing on return, which the existing `fitBoundsKey` mount behavior already produces).

## Viewport reporting: gesture-end events crossing the map boundary as a plain type

- **Decision**: `MapView` gains an optional `onViewportChange(bounds)` callback backed by a
  `useMapEvents({ moveend, zoomend })` helper (same pattern as the existing `ClickHandler`),
  reporting `map.getBounds()` converted to a plain `MapBounds = { south, west, north, east }`.
  It also fires once after the initial automatic framing so the page starts with real bounds.
- **Rationale**: `moveend`/`zoomend` fire once per completed gesture - exactly spec FR-008's
  "update when the gesture ends" with no debouncing code. Leaflet also fires `moveend` after
  `fitBounds`, which gives the initial full-set report (FR-009) for free. The plain type keeps
  `shared/ui/map` the only Leaflet consumer (003 structure rule): no `L.LatLngBounds` leaks into
  pages, and the mocked MapView in tests can emit bounds trivially.
- **Alternatives considered**: continuous `move` events + debounce (rejected - reimplements what
  gesture-end events already are); lifting the Leaflet map instance to the page via ref (rejected -
  breaks the single-Leaflet-consumer boundary and drags the map stack into page tests).

## Visibility test: numeric point-in-bounds on the page

- **Decision**: A venue is "in view" when `south <= lat <= north` and `west <= lng <= east`,
  computed in the page over the already-loaded in-range set.
- **Rationale**: The set is <= 100 rows (003 cap) already in memory - an O(n) filter per completed
  gesture is negligible. Antimeridian wrap-around is deliberately ignored: the venue directory is
  Ukraine-only (003 assumption), nowhere near longitude 180.
- **Alternatives considered**: asking Leaflet (`bounds.contains`) per venue (rejected - requires
  the Leaflet bounds object outside the map boundary); re-querying the server by bounding box
  (rejected - the data is already on the client; a new endpoint would be pure waste and would
  contradict the spec's "no server change" scope).

## Pagination: client-side, one constant, reset on visible-set change

- **Decision**: The list renders pages of `searchPageSize = 10` (a single named constant beside
  its consumer) over the viewport-filtered, distance-ordered set, with the same Prev/Next controls
  002 used. Any change of the visible set - viewport change, sport-filter change, reference change
  - resets to page 1. Controls are hidden when there is only one page.
- **Rationale**: The full set is capped at 100 client-side rows; server pagination would add a
  round-trip per page for data the client already holds. Reset-on-change prevents being stranded
  on a page that no longer exists (spec edge case). One constant satisfies the user's "raisable
  later" requirement without config machinery.
- **Alternatives considered**: server-side pagination (rejected - data already delivered; also the
  viewport filter is client-side, so server pages would not match the visible set); infinite
  scroll (rejected - not asked for, and Prev/Next already exists as a pattern in this codebase).

## Emphasis and framing: unchanged from 003, restated for traceability

- **Decision**: The emphasized marker remains the nearest venue of the WHOLE in-range set (user-
  confirmed), regardless of viewport or list page. Return to the search re-frames to the default
  full-radius view via the existing mount-time `fitBoundsKey` behavior - the previous manual zoom
  is deliberately not stored (user-confirmed).
- **Rationale**: Emphasis is a property of the search ("closest to me"), not of the camera.
  Not storing the viewport keeps the store free of map internals and makes FR-004 the zero-code
  path: a fresh mount always frames the full set.
- **Alternatives considered**: emphasis follows the visible subset (rejected by the user);
  restoring the exact zoomed viewport on return (rejected by the user - default framing chosen).
