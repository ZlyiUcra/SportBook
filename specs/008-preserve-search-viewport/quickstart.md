# Quickstart: Preserve map viewport across venue-detail navigation and visible-venue count

End-to-end validation of spec 008. Frontend-only; the backend is unchanged, so no backend setup
beyond the normal running service is needed. Assumes the 003/004 radius search already works (a
reference point resolves and `GET /api/venues/nearby` returns venues).

## Prerequisites

- Backend running (`ASPNETCORE_ENVIRONMENT=Development dotnet run`, default
  `http://localhost:5217`) with at least one city + a few venues with precise locations seeded.
- Frontend running (`yarn dev`, `http://localhost:5173`).
- A customer account (any registered user; owner role not required).

## Scenario 1 - Viewport preserved across a venue detour (spec US1, FR-001)

1. Sign in, go to the venue search, pick a city (or use "near me") so the map frames venues.
2. Zoom in twice and pan sideways so the framing is clearly no longer the default full-radius view.
   Note the rough area and zoom.
3. Above the list, read the visible-venue count (Scenario 2) - note its value.
4. Open any venue from the results, then use the venue page's "back to search" action (and, in a
   second run, the browser's back button).
5. EXPECT: the map reappears at the SAME zoom and pan position left in step 2 (NOT the default
   full-radius framing), with the same filters and result set, and no location permission prompt.

## Scenario 2 - Visible-venue count (spec US2, FR-006/FR-008)

1. From a framed search, read the count above the list; EXPECT it equals the number of venue cards
   currently shown across all pages (the visible set).
2. Zoom in so fewer venues remain on screen; once the gesture ends, EXPECT the count to drop to the
   new visible count and the list to match.
3. Zoom back out; EXPECT the count to rise again with the visible set.
4. Pan to an area with no venues; EXPECT the count to read zero and the "no venues in view" state to
   appear (distinct from "no venues within 75 km").
5. Switch the UI language (en/uk/pt/es) and revisit; EXPECT the count label to use the correct
   plural form for that locale (e.g., 1 vs many).

## Scenario 3 - New search resets the viewport, sport filter keeps it (spec FR-002, SC-003)

1. From a saved zoomed-in viewport (Scenario 1), change the reference point: pick a DIFFERENT city
   (or grant a fresh "near me").
2. EXPECT: the map reframes to the default full-radius view for the new reference (the saved
   viewport is gone).
3. Now, from a fresh framing, change ONLY the sport filter (same reference).
4. EXPECT: the viewport is NOT reset - the map keeps its zoom/pan and shows the new sport's venues
   within it; the count updates to the new visible set.

## Scenario 4 - No persistence beyond the session (spec FR-005, SC-004)

1. With a saved viewport present, reload the page (full reload).
2. EXPECT: the search starts fresh (no reference point, no viewport) - the in-memory store does not
   survive a reload, consistent with 004's stricter-than-required privacy posture.

## Automated checks

- `yarn test` - Vitest covers: `useSearchStore` viewport set-from-report and clear-on-reference (not
  on sport) semantics; `VenueSearchPage` restores saved center/zoom on remount and renders the count
  equal to the visible-set length with the correct plural form; `MapView` reports
  `{ bounds, center, zoom }` on `moveend`/`zoomend` and on mount. See data-model.md for the shapes and
  contracts/api.md for the MUSTs.
- `yarn build` - clean typecheck/build.
