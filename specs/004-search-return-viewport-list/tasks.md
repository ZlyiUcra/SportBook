# Tasks: Return-to-search navigation and viewport-synced venue list

**Input**: Design documents from `/specs/004-search-return-viewport-list/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api.md, quickstart.md

**Tests**: Included alongside each story (same stance as 002/003). The contract MUSTs (no-storage
search state, no geolocation call on restore, plain-type map boundary, gesture-end-only updates)
are each backed by a named task below.

**Organization**: Tasks are grouped by user story (from spec.md) to enable independent
implementation and testing of each story. Frontend-only feature - no backend tasks exist.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1-US3)
- File paths are relative to repo root, matching plan.md Project Structure

## Path Conventions

- Frontend: `frontend/src/`, `frontend/tests/` (backend untouched by this feature)

---

## Phase 1: Setup (Shared Infrastructure)

No setup tasks - the feature adds no dependencies, no configuration, and no schema. Existing
tooling (yarn, Vitest, Zustand, react-leaflet) covers everything.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The session store and the viewport-reporting map boundary - every user story depends
on at least one of these. No user story work can begin until this phase is complete.

- [ ] T001 [P] Create the search-state store in `frontend/src/pages/venues/model/searchStore.ts`:
      an in-memory Zustand store (explicitly NO `persist` middleware - contract MUST, spec FR-006)
      holding `city: City | null`, `sportType: SportType | ''`, `deviceCoords: {lat,lng} | null`,
      with setters and a derived reference-point selector implementing 003's precedence
      (deviceCoords -> city coords -> none) per data-model.md "Search state"
- [ ] T002 [P] Extend `frontend/src/shared/ui/map/MapView.tsx`: export a plain
      `MapBounds = { south, west, north, east }` type and add an optional
      `onViewportChange(bounds)` prop backed by a `useMapEvents({ moveend, zoomend })` helper
      (same pattern as the existing `ClickHandler`), converting `map.getBounds()` to `MapBounds`;
      fires once after the initial framing (leaflet emits `moveend` after `fitBounds` - research.md)
      and never during a gesture. No Leaflet type crosses the module boundary (contract MUST)
- [ ] T003 Rework `frontend/src/shared/lib/useReferencePoint.ts` to be store-backed: it reads
      `city`/`deviceCoords` from the search store, keeps `useGeolocation` as the permission/error
      machine, and writes granted rounded coords into the store - so a remount restores the
      reference without any Geolocation API call (spec FR-003; depends on T001)

**Checkpoint**: Store and viewport-reporting MapView exist; user story wiring can begin.

---

## Phase 3: User Story 1 - Return to my search from a venue page (Priority: P1) 🎯 MVP

**Goal**: One action returns from a venue page to the search with reference point, sport filter,
and results restored - no geolocation prompt, default full-radius framing.

**Independent Test**: Search (near-me or city), open a venue, return via the in-page action and via
browser back; verify the restored state, the absence of a permission prompt, and the default
framing. Land directly on a venue URL and verify the return action leads to the default prompt
state.

### Implementation for User Story 1

- [ ] T004 [US1] Wire `frontend/src/pages/venues/ui/VenueSearchPage.tsx` to the search store:
      `city` and `sportType` read/write the store instead of `React.useState`, the reference point
      comes from the store-backed `useReferencePoint`; a fresh mount with stored state shows the
      same results and the default full-radius framing via the existing `fitBoundsKey` mount
      behavior (spec FR-002, FR-004; depends on T001, T003)
- [ ] T005 [P] [US1] Add the always-visible "back to search" action to
      `frontend/src/pages/venue-detail/ui/VenueDetailPage.tsx` as a route link to `/` (research.md:
      not history-dependent navigation), with new i18n keys in
      `frontend/src/shared/i18n/locales/{en,uk,pt}.json` (spec FR-001, FR-005)
- [ ] T006 [US1] Frontend test in `frontend/tests/pages/VenueSearchReturn.test.tsx`: state
      restores across unmount/remount of the search page with NO Geolocation API call; an empty
      store yields the default prompt state (store reset between tests) (depends on T004, T005)

**Checkpoint**: US1 fully functional - leave the search, come back, everything is still there.

---

## Phase 4: User Story 2 - See in the list exactly what I see on the map (Priority: P2)

**Goal**: The list mirrors the venues visible in the current viewport, nearest-first, updating on
gesture end, with a dedicated "no venues in view" state; emphasis stays the overall nearest.

**Independent Test**: Zoom in and verify the list shrinks to the visible pins; zoom out and verify
it grows back; pan away from all pins and verify the "no venues in view" state (distinct from "no
venues within 75 km"); verify the emphasized marker never changes with the viewport.

### Implementation for User Story 2

- [ ] T007 [US2] Viewport filtering in `frontend/src/pages/venues/ui/VenueSearchPage.tsx`: hold
      the latest `MapBounds` from `onViewportChange` in page state (NOT in the search store -
      spec FR-004), filter the list to venues whose point lies within bounds (numeric comparison,
      research.md), full set before the first report (spec FR-009); add the "no venues in view"
      empty state with i18n keys in `frontend/src/shared/i18n/locales/{en,uk,pt}.json` (spec
      FR-010); the map keeps rendering the full in-range set and the emphasized marker stays the
      first element of the FULL set (spec FR-011, FR-014; depends on T002, T004)
- [ ] T008 [US2] Frontend test in `frontend/tests/pages/VenueRadiusView.test.tsx`: the MapView
      mock emits `MapBounds`; verify the list filters to in-bounds venues nearest-first, the
      "no venues in view" state appears for an empty viewport while "no venues within 75 km"
      remains for an empty in-range set, and emphasis stays on the overall-nearest venue
      (depends on T007)

**Checkpoint**: US2 functional - the map is the filter, the list is the detail.

---

## Phase 5: User Story 3 - Browse a long list page by page (Priority: P3)

**Goal**: The list pages at 10 venues with Prev/Next, resetting to page 1 on any visible-set
change; the map is never affected by paging.

**Independent Test**: With 11+ visible venues verify 10-per-page paging and controls; pan/zoom or
change the sport filter while on page 2 and verify the reset to page 1; verify all pins stay on
the map regardless of the list page.

### Implementation for User Story 3

- [ ] T009 [US3] Pagination in `frontend/src/pages/venues/ui/VenueSearchPage.tsx`: a
      `searchPageSize = 10` named constant (raisable later - spec FR-012), Prev/Next controls
      (same pattern 002 used), page state resets to 1 whenever the visible set changes (viewport,
      sport filter, or reference change - spec FR-013), controls hidden when only one page;
      pagination slices ONLY the list - map markers are untouched (spec FR-014; depends on T007)
- [ ] T010 [US3] Frontend test in `frontend/tests/pages/VenueRadiusView.test.tsx`: 11+ visible
      venues page at 10 nearest-first, Prev/Next work, bounds change and sport-filter change reset
      to page 1, no controls at <= 10 visible (depends on T009)

**Checkpoint**: All three user stories independently functional - full feature deliverable.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Contract audits and end-to-end validation across the stories

- [ ] T011 [P] No-storage audit (contract MUST, spec FR-006/SC-005): confirm the search state
      never reaches `persist`/localStorage/sessionStorage/cookies/URL - code search over
      `frontend/src/` plus a devtools Application check while exercising near-me, per
      quickstart.md scenario 4
- [ ] T012 [P] Update root `README.md` (search description in "Using the application" and the
      spec listing in "Further reading") for the return action, viewport-synced list, and
      pagination
- [ ] T013 Run all quickstart.md validation scenarios end-to-end against a locally running stack,
      plus non-regression: full `yarn test`, backend `dotnet test`, and a `yarn build`
      initial-chunk comparison (must be unchanged - no new dependencies)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: empty - nothing to do
- **Foundational (Phase 2)**: No dependencies - BLOCKS all user stories (T003 depends on T001)
- **User Story 1 (Phase 3)**: Depends on T001/T003; T005 is independent of them (different file)
- **User Story 2 (Phase 4)**: Depends on T002 and US1's page wiring (T004)
- **User Story 3 (Phase 5)**: Depends on US2's viewport filtering (T007)
- **Polish (Phase 6)**: Depends on all desired user stories being complete

### Within Each User Story

- Implementation before its test task; tests exercise the story's independent-test criteria
- Story complete before moving to the next priority (solo/sequential execution)

### Parallel Opportunities

- T001 and T002 (different files) can run in parallel at the start
- T005 (venue page + i18n) can run in parallel with T004 (search page)
- T011 and T012 can run in parallel in Polish

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 2: Foundational (store, map boundary, store-backed reference)
2. Complete Phase 3: User Story 1 (return with restored state)
3. **STOP and VALIDATE**: quickstart.md scenarios 1-4
4. Demo: search -> venue -> back, nothing lost

### Incremental Delivery

1. Foundational -> store + viewport boundary ready
2. User Story 1 -> validate -> demo (MVP: return-to-search)
3. User Story 2 -> validate -> demo (viewport-synced list)
4. User Story 3 -> validate -> demo (pagination)
5. Polish -> storage audit, README, full quickstart + non-regression

---

## Notes

- Frontend-only: no backend, schema, dependency, or chunk-size changes; 003's lazy-map rule stands
- This feature supersedes 003 spec FR-013 (list = viewport-visible subset; see contracts/api.md)
- The viewport (`MapBounds`) deliberately never enters the search store - returning to the search
  re-frames to the default full-radius view (spec FR-004, user-confirmed 2026-07-19)
- Commit after each verified functional slice (build + run + check), per user-stated atomic-commit
  preference - not mechanically per task or per phase
