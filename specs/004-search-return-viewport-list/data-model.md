# Phase 1 Data Model: Return-to-search navigation and viewport-synced venue list

This feature adds NO persistent data anywhere - no table, no column, no browser storage. It defines
two transient client-side shapes and two derived values. Server entities and DTOs from 002/003 are
reused untouched.

## Search state (transient, session-scoped store)

The customer's search inputs, lifted out of page-component state so they survive navigation within
the session. In-memory only (research.md: no persist middleware, no sessionStorage, no URL) - a new
browser session, and also a full page reload, starts empty.

| Field | Type | Notes |
|---|---|---|
| city | City \| null | The explicitly selected directory city (002 shape), if any |
| sportType | SportType \| '' | The sport filter; '' = all sports |
| deviceCoords | { lat, lng } \| null | Rounded (2-decimal) device location captured by an explicit "near me" grant; never persisted anywhere |

**Derived: reference point** - same precedence as 003 (data-model.md "Reference point"):
`deviceCoords` -> `city` coordinates -> none. The store is now the single source of truth the 003
`useReferencePoint` resolver reads; resolution rules do not change.

**Lifecycle**: written by user actions on the search page ("near me" grant, city pick, sport
change); read on every mount of the search page (including returns from a venue page); cleared only
by the session ending. Restoring from the store never triggers the Geolocation API (spec FR-003).

## MapBounds (transient, per-gesture value)

The visible map area, reported by the map wrapper after each completed zoom/pan gesture and once
after the initial automatic framing. A plain serializable shape so no Leaflet type crosses the
map boundary (research.md).

| Field | Type | Notes |
|---|---|---|
| south, west, north, east | number | Degrees; venue is visible when lat in [south, north] and lng in [west, east] |

Not stored anywhere - held in page state for the current render only; deliberately NOT part of the
search state store (return to the search re-frames to the default view, spec FR-004).

## Derived: visible set and list page (computed per render)

- **Visible set** = in-range venues (the 003 `NearbyVenue[]` response, already distance-ordered)
  filtered by the current MapBounds; before the first bounds report it equals the full set (spec
  FR-009). Drives the results list only - the map always renders the full in-range set (clustered),
  and the emphasized marker stays the first element of the FULL set (spec FR-011, FR-014).
- **List page** = 1-based index into the visible set at `searchPageSize = 10` items per page (a
  named constant, raisable without redesign). Resets to 1 whenever the visible set changes:
  viewport change, sport-filter change, or reference change (spec FR-013).

## Empty states (list)

Three distinct, non-interchangeable states (spec FR-010):

1. No reference point -> prompt to pick a city or use "near me" (003 behavior, unchanged).
2. Reference set, in-range set empty -> "no venues within 75 km" (003 behavior, unchanged).
3. In-range set non-empty, visible set empty -> NEW "no venues in view" state.
