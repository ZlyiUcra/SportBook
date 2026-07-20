# Tasks: Preserve map viewport across venue-detail navigation and visible-venue count

**Input**: Design documents from `/specs/008-preserve-search-viewport/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api.md, quickstart.md

**Tests**: Included alongside each story (same stance as 002-007), frontend-only - Vitest + React
Testing Library. The contract MUSTs (in-memory-only viewport, no Geolocation on restore, clear on
reference change / survive sport change, count == visible set, plural-aware label, no Leaflet leak)
are each backed by a named task below.

**Organization**: Tasks are grouped by user story (from spec.md) to enable independent implementation
and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1-US2)
- File paths are relative to repo root, matching plan.md Project Structure

## Path Conventions

- Frontend only: `frontend/src/`, `frontend/tests/`
- The venues search page tests live in `frontend/tests/pages/VenueSearchReturn.test.tsx` (004) and are
  EXTENDED here, not replaced.

---

## Phase 1: Setup (Shared Infrastructure)

No setup tasks - no dependencies, no configuration, no schema, no migration, no new package. Existing
tooling covers everything.

---

## Phase 2: Foundational (Blocking Prerequisites)

No foundational tasks. US1 (viewport preservation) and US2 (count) are logically independent: the
count reads the already-computed visible set (004 FR-007) and needs no viewport-preservation to work.
They share one file (`VenueSearchPage.tsx`), so they land sequentially in priority order (US1 then
US2) to avoid clobbering each other's edit, but neither blocks the other's correctness.

---

## Phase 3: User Story 1 - Return to the exact viewport I left (Priority: P1) MVP

**Goal**: Preserve the customer's map viewport (zoom and pan) across a venue-detail detour by storing
the map's center+zoom in the in-memory `useSearchStore` and restoring it on the search page's remount
- reversing 004 FR-004. The saved viewport clears on a reference-point change and survives a
sport-filter change (spec FR-002).

**Independent Test**: Run a radius search, zoom and pan, open a venue, return - the map is at the same
zoom/pan (not the default full-radius framing); change the reference point - the map reframes; change
only the sport filter - the viewport stays.

### Tests for User Story 1

- [ ] T001 [P] [US1] Store unit test: `setViewport({lat,lng,zoom})` stores the viewport, `setViewport(null)`
      clears it, the initial viewport is `null`, and the existing `city`/`sportType`/`deviceCoords`
      fields and their setters are unaffected, in `frontend/tests/pages/searchStore.test.ts` (new file)
- [ ] T002 [P] [US1] Page test (extend `frontend/tests/pages/VenueSearchReturn.test.tsx`): on remount
      with a saved viewport in the store, the mocked `MapView` is rendered with the saved center/zoom
      and `fitBoundsKey` is withheld (restore path, not auto-reframe); after a viewport report the
      store viewport is updated; changing the reference point clears the store viewport; changing only
      the sport filter keeps it; restoring never calls the Geolocation API

### Implementation for User Story 1

- [ ] T003 [P] [US1] In `frontend/src/pages/venues/model/searchStore.ts`, add a `viewport:
      { lat: number; lng: number; zoom: number } | null` field (initial `null`) and a
      `setViewport(viewport)` setter; keep the store `create`-only with NO `persist` middleware (004
      contract MUST, spec FR-005). Document that the viewport is the restorable camera (008 spec
      FR-001) and that clear-on-reference is page-driven (the store has no knowledge of the reference)
- [ ] T004 [P] [US1] In `frontend/src/shared/ui/map/MapView.tsx`, enrich the `onViewportChange`
      payload from `MapBounds` to `{ bounds: MapBounds; center: LatLng; zoom: number }` (update the
      prop type and the `ViewportReporter` to report `map.getCenter()`/`map.getZoom()` alongside
      `map.getBounds()` on `moveend`/`zoomend` and on mount, per research.md). No Leaflet type leaves
      the module - only the plain shape (003 single-Leaflet-consumer rule, 004 contract MUST)
- [ ] T005 [US1] In `frontend/src/pages/venues/ui/VenueSearchPage.tsx`: read `viewport`/`setViewport`
      from the store; in the `onViewportChange` handler set both the local `viewportBounds`
      (`report.bounds`, list/count filter, unchanged behavior) AND `setViewport({lat,lng,zoom})` from
      the report; extend the existing `referenceKey` effect to also `setViewport(null)` (clear on a new
      search, spec FR-002/SC-003); change `fitBoundsKey` to reference-only (drop the venue-id portion)
      so a sport-filter change no longer reframes; RESTORE - derive `restoring = viewport != null` and
      pass `center={restoring ? viewport : referencePoint}`, `zoom={viewport?.zoom ?? 13}`,
      `fitBoundsKey={restoring ? undefined : referenceKey}` so `MapContainer` mounts at the saved view
      and `FitBounds` is suppressed while restoring (depends on T003, T004)

**Checkpoint**: US1 functional and independently testable - returning from a venue page restores the
saved zoom/pan; a new reference point reframes; a sport-filter change keeps the viewport.

---

## Phase 4: User Story 2 - See how many venues I am looking at right now (Priority: P2)

**Goal**: Show the count of venues currently visible in the map viewport above the results list, equal
to the already-computed visible set, with a locale-aware plural label.

**Independent Test**: From a framed search the count above the list equals the visible venue cards;
zoom in - the count drops; pan to an empty area - the count reads zero with the "no venues in view"
state; switch language - the plural form is correct.

### Tests for User Story 2

- [ ] T006 [US2] Page test (extend `frontend/tests/pages/VenueSearchReturn.test.tsx`): the count above
      the list equals `visibleVenues.length`; it updates when the visible set changes; it reads zero
      when the viewport is empty; the label uses the correct plural form for the active locale (en
      `_one`/`_other`) (depends on T007 for the key and T008 for the render)

### Implementation for User Story 2

- [ ] T007 [P] [US2] In `frontend/src/shared/i18n/locales/{en,uk,pt,es}.json`, add a
      `venues.visibleCount` plural key - `_one`/`_other` for en/pt/es, `_one`/`_few`/`_many`/`_other`
      for uk - interpolated as `{{count}}` (e.g. en `_one`: "{{count}} venue visible", `_other`:
      "{{count}} venues visible"). i18next resolves the form from the count per locale (spec FR-009)
- [ ] T008 [US2] In `frontend/src/pages/venues/ui/VenueSearchPage.tsx`, render `visibleVenues.length`
      via `t('venues.visibleCount', { count: visibleVenues.length })` above the results list, under the
      same conditions the list/map render (a reference point exists and the in-range set is
      non-empty); when the visible set is empty the count reads zero and 004's "no venues in view"
      state applies (spec FR-006/FR-008). No new server request - the visible set is already computed
      (depends on T007)

**Checkpoint**: US2 functional - the visible-venue count is always shown above the list and matches the
viewport exactly.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: README and end-to-end validation

- [ ] T009 [P] Update root `README.md`: note that the venue search preserves the map viewport across
      venue visits and shows a count of venues visible in the viewport; add the 008 spec to "Further
      reading" (if that section exists, as in 007)
- [ ] T010 Run `quickstart.md` scenarios 1-4 end-to-end against a locally running stack, plus
      non-regression: `yarn test` (all green), `yarn build` (clean, no new dependency, initial route
      chunk unchanged - the map stack stays lazy-loaded, 003 SC-006). NOTE: a live browser click-through
      needs headless-browser tooling (Playwright) that is not installed; installing it would add a new
      dependency requiring sign-off per repo rules, so it is flagged rather than assumed (same note as
      006/007) - the manual scenarios are run in a real browser by the user, the automated checks by
      the agent

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: empty
- **Foundational (Phase 2)**: empty - both stories are logically independent
- **User Story 1 (Phase 3)**: T001/T002 (tests) first; T003 (store) and T004 (MapView) in parallel;
  T005 (page wiring) after T003 + T004
- **User Story 2 (Phase 4)**: T006 (test) first; T007 (i18n); T008 (page render) after T007.
  Independent of US1 in logic, but shares `VenueSearchPage.tsx` - sequence after US1
- **Polish (Phase 5)**: depends on both stories being complete

### Within Each User Story

- US1: tests (T001, T002) alongside implementation (T003 store, T004 MapView, T005 page); T005 after
  T003 + T004
- US2: test (T006) alongside implementation (T007 i18n, T008 page); T008 after T007

### Parallel Opportunities

- US1: T001 and T002 (different test files); T003 and T004 (different src files - store vs MapView)
- Polish: T009 can run alongside the story work (README only)

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 3: User Story 1 (viewport preservation)
2. **STOP and VALIDATE**: quickstart.md scenarios 1 (viewport preserved across a detour), 3 (reference
   change reframes, sport change keeps it), 4 (no persistence beyond the session)
3. Demo: returning from a venue page no longer discards the customer's zoom/pan

### Incremental Delivery

1. User Story 1 -> validate -> demo (viewport preservation, the core reversal of 004 FR-004)
2. User Story 2 -> validate -> demo (the visible-venue count)
3. Polish -> README, full quickstart + non-regression

---

## Notes

- Frontend-only: no backend, no schema, no migration, no new dependency (per plan.md)
- The viewport is stored as `{lat,lng,zoom}` (center+zoom), exactly spec FR-001's "zoom level and pan
  position"; bounds for the list/count filter stay ephemeral page state (as in 004), repopulated on
  mount by the existing `ViewportReporter` mount-time report
- Restore works because navigation remounts `VenueSearchPage`/`MapView`, and `MapContainer` reads
  `center`/`zoom` at mount; `fitBoundsKey` is withheld while restoring so `FitBounds` (rendered only
  when `fitBoundsKey !== undefined`) does not auto-reframe - this is the precise reversal of 004 FR-004
- `fitBoundsKey` drops the venue-id portion and becomes reference-only, so a sport-filter change (same
  reference, new venue set) no longer reframes - implementing spec FR-002 (viewport survives a sport
  change)
- The viewport is in-memory only (no `persist`); a full page reload clears it, consistent with 004's
  stricter-than-required privacy posture (spec FR-005/SC-004)
- The count is a pure render of `visibleVenues.length` (004 FR-007) - no new data, no query
- Commit after each verified functional slice (build + run + check), per the user-stated atomic-commit
  preference - not mechanically per task or per phase
