# Contracts: Return-to-search navigation and viewport-synced venue list

Delta contract on top of `specs/003-venue-radius-map/contracts/api.md`.

## HTTP surface: NO changes

No endpoint is added, changed, or removed. `GET /api/venues/nearby` (003) remains the single data
source: fixed 75 km radius, nearest-first, capped at 100, authenticated, coordinates never
persisted or logged. Viewport filtering and pagination happen entirely on the client over that
already-delivered set - a client MUST NOT gain a way to widen the radius or page on the server.

## Superseded 003 behavior

- 003 spec FR-013 ("the list reflects the same in-range set as the map") is superseded by 004 spec
  FR-007: the list now reflects the viewport-visible SUBSET of the in-range set, nearest-first.
  The map still shows the full in-range set; initial framing makes both coincide.

## Frontend contract MUSTs

Not HTTP, but binding on the implementation (same style as 003's frontend MUSTs):

- The search state store is in-memory only: no `persist` middleware, no localStorage, no
  sessionStorage, no cookies, no URL query parameters. Device coordinates exist solely in JS
  memory for the session (spec FR-006, SC-005).
- Restoring the search from the store MUST NOT invoke the browser Geolocation API - the API is
  called only from the explicit "near me" action, exactly as in 003 (spec FR-003).
- Viewport bounds cross the `shared/ui/map` boundary only as the plain `MapBounds` shape - no
  Leaflet type or map instance leaks into pages (003 single-Leaflet-consumer rule).
- The list updates on completed gestures only (`moveend`/`zoomend`), never continuously during a
  drag or pinch (spec FR-008).
- The emphasized marker is always the nearest venue of the FULL in-range set; pagination and
  viewport filtering never affect which markers the map shows (spec FR-011, FR-014).
- The page size is one named constant (10); raising it is a one-line change (spec FR-012).
