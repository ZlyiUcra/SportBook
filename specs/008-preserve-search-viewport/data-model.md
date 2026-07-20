# Phase 1 Data Model: Preserve map viewport across venue-detail navigation and visible-venue count

This feature adds NO persistent data anywhere - no table, no column, no browser storage, no backend
change. It adds one transient client-side field to the existing search store and reuses the 004
shapes. Server entities and DTOs from 001-003 are reused untouched.

## Search state (transient, session-scoped store) - AMENDS 004

The customer's search inputs (004 data-model.md "Search state") gain one field. In-memory only - no
`persist` middleware, no sessionStorage, no URL (004 contract MUST, spec FR-005); a new browser
session and a full page reload start empty.

| Field | Type | Notes |
|---|---|---|
| city | City \| null | 004 - unchanged |
| sportType | SportType \| '' | 004 - unchanged |
| deviceCoords | { lat, lng } \| null | 004 - unchanged; rounded 2-decimal device location, never persisted |
| viewport | { lat, lng, zoom } \| null | NEW - the map's center and zoom to restore on return (spec FR-001). Written on each viewport report; cleared on reference-point change (FR-002); NOT cleared on sport-filter change |

**Derived: reference point** - 003 precedence (deviceCoords -> city -> none), unchanged. The viewport
does NOT participate in reference resolution - it is a camera position, not a search input.

**Lifecycle**: `viewport` is written by `MapView`'s viewport report (on completed gestures and once
on mount); read on every mount of the search page so a return from a venue page restores the camera
(spec US1); cleared when the reference point changes (a new search reframes to the full-radius view,
spec FR-002/SC-003); cleared by the session ending. Restoring it never triggers the Geolocation API
(spec FR-003).

## MapViewport report (transient, per-gesture value) - AMENDS 004 MapBounds

The viewport report `MapView` emits after each completed gesture and once on mount. Enriched from
004's `MapBounds` so both the list/count filter and the store can read one report (research.md).

| Field | Type | Notes |
|---|---|---|
| bounds | MapBounds | 004 shape `{ south, west, north, east }`; venue visible when lat in [south, north] and lng in [west, east]. Drives the results list and the count (004 FR-007, spec FR-006/FR-008) |
| center | { lat, lng } | NEW - the map's center, used to keep the store's `viewport.center` in sync |
| zoom | number | NEW - the map's zoom level, used to keep the store's `viewport.zoom` in sync |

Not stored as a whole - the page consumes `bounds` immediately (ephemeral page state, as in 004) and
forwards `center`+`zoom` into the store's `viewport`.

## Derived: visible set, count, and list page (computed per render)

- **Visible set** = in-range venues (003 `NearbyVenue[]`, distance-ordered) filtered by the current
  `bounds`; before the first report it equals the full set (004 FR-009). Drives the list and the count
  only; the map renders the full in-range set (clustered) and the emphasized marker stays the first
  element of the FULL set (004 FR-011, FR-014).
- **Visible count** = `visibleVenues.length` (spec FR-006/FR-008/SC-002). Rendered above the list
  with a locale-aware plural label (spec FR-009).
- **List page** = 004 unchanged: 1-based index at `searchPageSize = 10`, resets to 1 on visible-set
  change (004 FR-012/FR-013).

## Empty states (list) - unchanged from 004

Three distinct states (004 FR-010): no reference point; reference set but in-range empty ("no venues
within 75 km"); in-range non-empty but visible set empty ("no venues in view"). When the visible set
is empty the count reads zero (spec FR-008).
