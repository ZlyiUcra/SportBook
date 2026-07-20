# Contracts: Preserve map viewport across venue-detail navigation and visible-venue count

Delta contract on top of `specs/004-search-return-viewport-list/contracts/api.md`. Frontend-only;
no backend change.

## HTTP surface: NO changes

No endpoint is added, changed, or removed. `GET /api/venues/nearby` (003) remains the single data
source, unchanged. Viewport preservation and the count are purely client-side over the
already-delivered set - a client MUST NOT gain any new server capability.

## Superseded 004 behavior

- 004 spec FR-004 ("on return the map shows the default full-radius framing, not the previously
  zoomed viewport") is superseded by 008 spec FR-001: on return the map restores the saved zoom/pan.
  004 US1 Acceptance Scenario 3 is superseded likewise.
- 004 research.md's "the previous manual zoom is deliberately not stored" is reversed: the viewport
  (center+zoom) IS now stored in the search store.
- All other 004 requirements stand (listed in 008 spec Assumptions).

## Frontend contract MUSTs

Binding on the implementation, same style as 004's frontend MUSTs:

- The viewport field is in-memory only: no `persist` middleware, no localStorage, no sessionStorage,
  no cookies, no URL query parameters. It dies with the session and on a full page reload (spec
  FR-005, 004 contract MUST).
- Restoring the viewport MUST NOT invoke the browser Geolocation API (spec FR-003); the API remains
  reachable only from the explicit "near me" action.
- The viewport MUST be cleared when the reference point changes (a new city or a fresh "near me"),
  and MUST survive a sport-filter change (spec FR-002/SC-003).
- The viewport and the viewport report cross the `shared/ui/map` boundary only as plain shapes
  (`{ lat, lng, zoom }` and `{ bounds, center, zoom }`) - no Leaflet type or map instance leaks into
  pages (003 single-Leaflet-consumer rule, 004 contract MUST).
- The count MUST equal the visible set the list shows (004 FR-007) and update on completed gestures
  only (004 FR-008), never continuously during a drag/pinch.
- The count label MUST follow each UI locale's plural rules (spec FR-009) via i18next plural keys.
- The emphasized marker remains the nearest venue of the FULL in-range set; viewport restoration and
  the count never affect which markers the map shows (004 FR-011/FR-014).
