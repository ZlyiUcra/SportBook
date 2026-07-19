# Quickstart: Return-to-search navigation and viewport-synced venue list

Validation guide for proving the feature works end-to-end once implemented. Prerequisites, setup,
and run commands are unchanged from `specs/002-city-geolocation-map/quickstart.md`; no dependency
install and no database change. Seed several coordinate-bearing venues near one city (the 003
quickstart's Kyiv set works; for pagination add 11+ within one view).

## Manual scenarios (via `yarn dev`)

1. **Return with state (US1)**: run a search ("near me" or a city, plus a sport filter), open a
   venue from the list, then use the "back to search" action. The search reappears with the same
   reference, filter, and venues; NO location permission prompt; the map shows the default framing
   with all in-range venues. Repeat using the browser's back button - same result.
2. **Default framing on return (US1)**: before opening the venue, zoom deep into the map. After
   returning, the map is at the default full-radius framing, not the zoomed view.
3. **Direct landing (US1)**: open a venue URL in a fresh tab (logged in), use "back to search" -
   the search shows its default prompt state (no stale data, no errors).
4. **Session-only state (US1)**: after a search, reload the page (F5) - the search starts empty.
   In devtools > Application, verify localStorage/sessionStorage/cookies contain no coordinates,
   city, or filter values at any point.
5. **Viewport sync (US2)**: with venues spread out, zoom in so only some pins remain visible - the
   list shrinks to exactly those, still nearest-first; zoom out - the list grows back. The list
   changes only when the gesture ends, not during the drag.
6. **Empty in view (US2)**: pan far away from all pins - the list shows the "no venues in view"
   message, NOT the "no venues within 75 km" one; pan back and the list returns.
7. **Global emphasis (US2)**: zoom so the overall-nearest venue is off screen - the larger
   emphasized marker does not jump to another pin.
8. **Pagination (US3)**: with 11+ venues visible, the list shows 10 and Prev/Next controls; page 2
   shows the remainder. Pan or zoom while on page 2 - the list resets to page 1. Change the sport
   filter - also resets to page 1. With <= 10 visible, no controls appear.
9. **Map unaffected by paging (US3)**: on any list page, the map still shows all visible pins
   (clustered as needed).

## Automated tests

```powershell
# Frontend (from frontend/)
yarn test
```

Must include: restoring the search state across unmount/remount without any Geolocation API call;
viewport filtering (mocked MapView emitting `MapBounds`); the "no venues in view" vs "no venues
within 75 km" distinction; pagination at 10 with reset on bounds/filter change. Backend suites run
unchanged (`dotnet test` from the repo root) - this feature must not affect them.

## Non-regression

- 003 quickstart scenarios still pass (near-me, city, no-reference, clustering, lazy chunk).
- `yarn build` initial-chunk size is unchanged (no new dependencies; the map chunk gains only the
  bounds-reporting helper).
