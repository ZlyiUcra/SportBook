# Phase 0 Research: Preserve map viewport across venue-detail navigation and visible-venue count

All product-level choices were made by the user in the 2026-07-20 discussion (spec Assumptions),
including the reversal of 004 FR-004. This file pins the mechanisms. No NEEDS CLARIFICATION markers
remain.

## What the viewport stores: center + zoom, not bounds

- **Decision**: Store `{ lat, lng, zoom }` (the map's center and zoom level) in the in-memory
  `useSearchStore`, alongside the existing `city`/`sportType`/`deviceCoords`.
- **Rationale**: Spec FR-001 says "the same zoom level and pan position" - center+zoom IS that,
  literally. Storing bounds (the alternative) would require an imperative `fitBounds(savedBounds)`
  after mount to restore, which flashes the map at the reference point before jumping to the saved
  view; center+zoom is read by `MapContainer` at mount, so the map mounts directly at the saved view
  with no flash. The list/count visibility filter continues to use bounds, but those stay ephemeral
  page state (as in 004), repopulated on mount by the existing mount-time viewport report - so only
  the restorable view needs to survive navigation, and bounds do not.
- **Alternatives considered**: storing `MapBounds` (rejected - restore-via-fitBounds flashes; bounds
  are also the wrong shape for "zoom level and pan position"); storing both bounds and center+zoom
  (rejected - redundant; bounds are derivable on mount from the restored view).

## How the saved viewport is restored on return: mount-time center/zoom, fitBounds suppressed

- **Decision**: On `VenueSearchPage` remount (the return from a venue page), pass the saved
  center+zoom as `MapView`'s `center`/`zoom` props and pass `fitBoundsKey={undefined}` while a saved
  viewport exists. `MapContainer` reads `center`/`zoom` only at mount, so it mounts at the saved
  view; and `MapView` already renders the `FitBounds` helper only when `fitBoundsKey !== undefined`
  (a line-level fact of `MapView.tsx`), so withholding it suppresses the auto-reframe that 004 used
  to reset to the full-radius view. When no viewport is saved (fresh search, or a reference-point
  change just cleared it), pass the reference point + default zoom and a reference-keyed
  `fitBoundsKey` so the marker framing runs as today.
- **Rationale**: Restoring via the props `MapContainer` already reads at mount is the zero-flash path
  and reuses an existing mechanism; the conditional `fitBoundsKey` reuses the existing
  "render `FitBounds` only when set" guard, so no new prop or branch inside `MapView` is required for
  the suppress behavior. This is exactly the reversal of 004's research.md decision ("the previous
  manual zoom is deliberately not stored... a fresh mount always frames the full set").
- **Alternatives considered**: a new `restoreBounds`/`restoreViewport` prop on `MapView` with a
  dedicated restore effect (rejected - duplicate of what mount-time center/zoom already achieves, and
  adds a prop for one consumer); `navigate(-1)` with browser scroll/viewport restoration (rejected -
  history-dependent, breaks for direct venue-page landings, and 004 already chose a route link).

## When the viewport resets: reference-point change only (sport filter keeps it)

- **Decision**: The store's viewport is cleared when the reference point changes (a different city,
  or a fresh "near me" grant) and ONLY then. A sport-filter change does NOT clear it - the new result
  set is shown within the preserved viewport.
- **Rationale**: Spec FR-002. This also requires `fitBoundsKey` to stop depending on the returned
  venue-id set: 004's `fitBoundsKey = reference | venueIds` would re-frame (resetting the viewport)
  whenever a sport change produces a different venue set. Changing `fitBoundsKey` to the reference
  point alone makes re-framing happen only on a reference change, which is exactly the reset trigger
  the spec wants. On a reference change: viewport cleared -> `fitBoundsKey` (reference-keyed)
  changes -> `FitBounds` mounts/updates -> frames the new marker set; on a sport change: reference
  unchanged -> `fitBoundsKey` unchanged -> no reframe -> markers update within the kept viewport.
- **Alternatives considered**: keep `fitBoundsKey` including venue ids and special-case sport changes
  (rejected - couples framing to the wrong signal and needs an exception); clear viewport on sport
  change too (rejected by the user, spec FR-002).

## Viewport report payload: bounds plus center+zoom

- **Decision**: Enrich `MapView`'s `onViewportChange` payload from `MapBounds` to
  `{ bounds: MapBounds, center: LatLng, zoom: number }`, reported on the same `moveend`/`zoomend`
  cadence (004 FR-008) and once on mount. The page uses `.bounds` for the list/count visibility
  filter (unchanged) and `.center`/`.zoom` to keep the store's viewport in sync.
- **Rationale**: One report feeds both consumers (filter and restore) without a second callback or a
  ref lifted out of `shared/ui/map` (which would break the single-Leaflet-consumer boundary). The
  values are already available on the Leaflet map instance in the existing handler (`getBounds`,
  `getCenter`, `getZoom`).
- **Alternatives considered**: keep `onViewportChange(bounds)` and add a second callback or a ref
  (rejected - two callbacks for one event, or a boundary-breaking ref); derive center+zoom from
  bounds off the map (rejected - needs the map size; not derivable off the map instance).

## Count: length of the already-computed visible set, plural-aware label

- **Decision**: Render `visibleVenues.length` above the results list (where the list renders - after
  the map), with an i18next plural key `venues.visibleCount` that has per-locale plural forms
  (`_one`/`_other` for en/pt/es; `_one`/`_few`/`_many`/`_other` for uk). The count appears under the
  same conditions the list/map do (a reference point exists and the in-range set is non-empty); when
  the visible set is empty, the count reads zero and 004's "no venues in view" state applies.
- **Rationale**: The visible set is already computed per render (004 FR-007); the count is its
  `.length`, so it is a pure render with no new data or query. i18next resolves the correct plural
  form from the count per locale, satisfying spec FR-009 without manual branching.
- **Alternatives considered**: show "X of Y" (visible of total in-range) (rejected - the user asked
  for the count of what is visible right now, not a ratio; the total is not requested); compute the
  count server-side (rejected - the visible set is a client-side viewport filter over already-
  delivered data; a server count would contradict the no-backend-change scope).

## Emphasis and nearest-marker: unchanged from 003/004, restated for traceability

- **Decision**: The emphasized marker stays the nearest venue of the WHOLE in-range set (004 FR-011),
  regardless of the restored viewport. Preserving the viewport moves only the camera, not which
  marker is emphasized.
- **Rationale**: Emphasis is a property of the search ("closest to me"), not of the camera - the same
  rule 004 restated; restoring the viewport does not change it.
