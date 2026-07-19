# Tasks: Geolocation-centered radius map of nearby venues

**Input**: Design documents from `/specs/003-venue-radius-map/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api.md, quickstart.md

**Tests**: Included alongside each story (same stance as 002). The contract-level MUSTs from the
consilium (fixed server-side radius, coordinate range validation, no-trig SQL translation, XSS-safe
markers, lazy chunk) are each backed by a named task below.

**Organization**: Tasks are grouped by user story (from spec.md) to enable independent
implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1-US3)
- File paths are relative to repo root, matching plan.md Project Structure

## Path Conventions

- Backend: `backend/src/SportBook.{Api,Application}/`, `backend/tests/SportBook.{UnitTests,IntegrationTests}/`
- Frontend: `frontend/src/`, `frontend/tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Dependencies needed before any map-clustering code

- [ ] T001 [P] Add frontend dependencies `react-leaflet-cluster` (~4.1.3) and dev
      `@types/leaflet.markercluster` (~1.5.6) to `frontend/package.json` per plan.md Primary
      Dependencies (user-approved via the clustering choice in /speckit-clarify); confirm
      `leaflet.markercluster` is pulled transitively

**Checkpoint**: Clustering dependency installed and importable inside the map wrapper only.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The nearby endpoint, the shared reference-point/geolocation plumbing, and the MapView
extensions - every user story depends on these. No user story work can begin until this phase is
complete.

- [ ] T002 [P] Add `NearbyVenueResponse` DTO (venue summary fields + `decimal DistanceKm`) in
      `backend/src/SportBook.Application/Dtos/VenueDtos.cs` per data-model.md
- [ ] T003 Implement `VenueService.SearchNearbyAsync(decimal lat, decimal lng, SportType? sportType,
      CancellationToken)` in `backend/src/SportBook.Application/Services/VenueService.cs`: a
      `VenueRadiusKm = 75` server-side constant (declared beside `VenueService`, NOT on
      `CityDistance`); query `Venues` where `Latitude != null` (+ the existing active-court sport
      predicate when `sportType` is set), `Include(v => v.City)`; then in C# compute
      `CityDistance.DistanceKm` per row, filter `<= 75`, order by distance ascending, `Take(100)`,
      project to `NearbyVenueResponse` (depends on T002)
- [ ] T004 Implement `VenuesController` `GET /api/venues/nearby` (`lat`, `lng`, optional
      `sportType`) per contracts/api.md - range-validate `lat`/`lng`, never persist or log received
      coordinates - in `backend/src/SportBook.Api/Controllers/VenuesController.cs` (depends on T003)
- [ ] T005 [P] Extract a `useGeolocation` hook (browser Geolocation API, 2-decimal rounding,
      permission/denied/error state) from the inline logic in `MyCityButton` into
      `frontend/src/shared/lib/` (depends on nothing)
- [ ] T006 [P] Implement a `useReferencePoint` resolver (precedence: granted device location via
      `useGeolocation` -> explicitly selected city coordinates -> none) as the single source of
      truth for the map center and the nearby query, in `frontend/src/shared/lib/` (depends on T005)
- [ ] T007 [P] Frontend: `NearbyVenue` type (+`distanceKm`) and `getNearbyVenues(lat, lng,
      sportType?)` API call in `frontend/src/entities/venue/` (depends on T004)
- [ ] T008 Extend `frontend/src/shared/ui/map/MapView.tsx`: marker clustering via
      `react-leaflet-cluster` (the only new leaflet consumer, still lazy), a `fitBounds` mode (a
      `useMap` effect keyed on the reference/venue-id set so it fits once per reference change and a
      max-zoom cap prevents over-zoom, never re-fitting on unrelated renders), and a per-marker
      emphasis icon (a second `L.icon` for the nearest venue - never `L.divIcon({ html })` with
      venue fields) (depends on T001)

**Checkpoint**: `GET /api/venues/nearby` works and is range-/radius-enforced; the reference-point
hook and the clustered/fit/emphasis MapView exist. User story wiring can now begin.

---

## Phase 3: User Story 1 - See venues near me on a map (Priority: P1) 🎯 MVP

**Goal**: A customer activates "near me", grants location, and sees the in-75 km venues on a
clustered, auto-framed map (nearest emphasized) plus a distance-ordered list.

**Independent Test**: Activate "near me" with location granted; verify the map centers on the
device position, shows the in-range venues (clustered, nearest emphasized, all framed), and the
list shows the same venues nearest-first, with none beyond 75 km.

### Tests for User Story 1

- [ ] T009 [P] [US1] Integration test: `GET /api/venues/nearby` returns venues within 75 km ordered
      nearest-first with `distanceKm`, excludes venues beyond 75 km, rejects out-of-range `lat`/`lng`
      (400), honors `sportType`, and requires auth (spec Acceptance Scenarios) in
      `backend/tests/SportBook.IntegrationTests/VenueNearbyPointTests.cs`
- [ ] T010 [P] [US1] Unit test: the nearby distance/order/cap over materialized rows (Sqlite path)
      in `backend/tests/SportBook.UnitTests/VenueNearbyDistanceTests.cs`
- [ ] T011 [P] [US1] Unit test: `ToQueryString()` proves the `Latitude != null` (+ optional sport)
      candidate query translates to SQL and pushes NO trigonometry server-side in
      `backend/tests/SportBook.UnitTests/VenueNearbyQueryTranslationTests.cs`

### Implementation for User Story 1

- [ ] T012 [US1] Frontend: "near me" action in `frontend/src/features/city-select/` using
      `useGeolocation` to set the device-location reference point (rounds coords to 2 decimals before
      any request) (depends on T005, T006)
- [ ] T013 [US1] Frontend: reshape `frontend/src/pages/venues/ui/VenueSearchPage.tsx` to the
      reference-point radius view - when a reference is active, call `getNearbyVenues` and render the
      clustered `MapView` (nearest emphasized, auto-framed) plus a distance-ordered results list from
      the same in-range set; remove the `VenueSearchMap` usage and the `includeNearby` toggle
      (depends on T006, T007, T008, T012)
- [ ] T014 [US1] Frontend smoke test (map + geolocation mocked, per research.md testing stance): the
      near-me flow shows the in-range venues on the map and in the list nearest-first in
      `frontend/tests/pages/VenueRadiusView.test.tsx`

**Checkpoint**: US1 fully functional - "near me" shows the clustered, framed radius map and the
distance-ordered list.

---

## Phase 4: User Story 2 - See venues near a chosen city (Priority: P2)

**Goal**: A customer who does not share location picks a city and gets the same radius view centered
on that city.

**Independent Test**: Without granting location, select a directory city and verify the map centers
on it and the same 75 km behavior and list appear.

### Implementation for User Story 2

- [ ] T015 [US2] Frontend: wire city selection into `useReferencePoint` (a selected city's
      coordinates become the reference when no device location is active) so the combobox drives the
      same radius map + list in `frontend/src/pages/venues/ui/VenueSearchPage.tsx` (depends on T013)
- [ ] T016 [US2] Frontend test: selecting a city with no geolocation drives the radius map and the
      distance-ordered list; device location takes precedence over a selected city when both exist,
      in `frontend/tests/pages/VenueRadiusView.test.tsx`

**Checkpoint**: US2 functional - city selection centers the same radius view without location
sharing.

---

## Phase 5: User Story 3 - No misleading map when there is nothing to center on (Priority: P3)

**Goal**: With neither location nor a selected city, no map block renders at all and the list guides
the customer.

**Independent Test**: Deny location and select no city; verify no map area/frame/placeholder renders
and the list shows a prompt to pick a city or use "near me".

### Implementation for User Story 3

- [ ] T017 [US3] Frontend: the no-reference state in
      `frontend/src/pages/venues/ui/VenueSearchPage.tsx` - render no map block at all (not an empty
      map), and show a results-list prompt to pick a city or use "near me"; removing the reference
      (revoke/deselect) removes the map block rather than showing it empty (depends on T013)
- [ ] T018 [US3] Frontend test: no geolocation and no selected city renders no map block and shows
      the prompt in `frontend/tests/pages/VenueRadiusView.test.tsx`

**Checkpoint**: All three user stories independently functional - full feature deliverable.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T019 [P] Add near-me / no-reference-prompt / cluster-related i18n keys in
      `frontend/src/shared/i18n/locales/{en,uk,pt}.json` (all three locales)
- [ ] T020 Remove the superseded "My city" button (its geolocation role is replaced by "near me")
      and any now-dead `includeNearby` wiring left on the search page; confirm no dead code or unused
      imports in `frontend/src/features/city-select/` and `frontend/src/pages/venues/`
- [ ] T021 Measure `yarn build` output before/after the clustering libs land from `frontend/` -
      confirm the initial JS chunk delta is 0 (leaflet/react-leaflet/react-leaflet-cluster/
      leaflet.markercluster all in the lazy `MapView` chunk; spec SC-006, quickstart.md)
- [ ] T022 [P] Response-DTO whitelist audit for `NearbyVenueResponse` - confirm `Population` never
      leaks and no new `[AllowAnonymous]` was introduced (contract MUST, spec FR-011)
- [ ] T023 [P] Update `backend/README.md`/`frontend/README.md`/root `README.md` with the nearby
      endpoint, the clustering dependency, and the "near me" action
- [ ] T024 Verify the EF Core query plan for `GET /api/venues/nearby` - confirm the only server-side
      work is the `Latitude != null` (+ sport) filter and that no trigonometric full-table scan is
      generated (performance consilium finding)
- [ ] T025 [P] Run all quickstart.md validation scenarios end-to-end against a locally running stack

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational - no dependency on other stories (MVP)
- **User Story 2 (Phase 4)**: Depends on Foundational and reuses US1's reshaped search page (T013)
  - implement after US1 for a working page
- **User Story 3 (Phase 5)**: Depends on Foundational and the reshaped page (T013) - the no-reference
  guard sits on the same page as US1/US2
- **Polish (Phase 6)**: Depends on all desired user stories being complete

### Within Each User Story

- Tests before implementation within each story
- Application service before Controller; Controller before the frontend that consumes it
- Story complete before moving to next priority (for solo/sequential execution)

### Parallel Opportunities

- T002, T005, T007 (different files) can run in parallel early in Foundational; T003 depends on
  T002, T004 on T003, T006 on T005, T008 on T001
- All US1 test tasks (T009, T010, T011) can run in parallel
- Most Polish tasks marked [P] can run in parallel

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (endpoint + hooks + MapView)
3. Complete Phase 3: User Story 1 (near-me radius view)
4. **STOP and VALIDATE**: run quickstart.md near-me and nearby-endpoint scenarios
5. Demo the near-me radius map + distance-ordered list

### Incremental Delivery

1. Setup + Foundational -> foundation ready (nearby endpoint, reference hook, clustered/fit MapView)
2. User Story 1 -> validate -> demo (MVP: near-me radius map + list)
3. User Story 2 -> validate -> demo (city-centered radius without location)
4. User Story 3 -> validate -> demo (no map when no reference)
5. Polish -> supersession cleanup, build/chunk check, DTO audit, docs, query plan, quickstart

---

## Notes

- [P] tasks touch different files with no unmet dependency
- [Story] label maps each task to its user story for traceability
- The search-page reshape (T013) removes 002's `VenueSearchMap` and the `includeNearby` toggle and
  is where US1/US2/US3 all converge - do not commit it as a checkpoint until at least US1 keeps the
  page working end to end (per the user's atomic-commit preference: verified working functional
  slice, not per-task/per-phase mechanically)
- No database migration in this feature - it reuses `Venue.Latitude`/`Longitude` from 002
- The 75 km radius is a server-side constant distinct from 002's `CityDistance.NearbyRadiusKm = 150`;
  keep it in its own named home (VenueService), never bolted onto `CityDistance`
- Map content safety (T008: no `divIcon({ html })` emphasis) and coordinate rounding (T012) and the
  no-trig SQL translation (T011) are contract MUSTs from the consilium, not optional hardening
- A `Latitude`/`Longitude` index or bounding-box prefilter, and app-wide rate limiting, are recorded
  future work for when coordinate-bearing venues reach the low tens of thousands - intentionally NOT
  done now (performance/security consilium findings)
- Commit after each verified functional slice (build + run + check), per user-stated atomic-commit
  preference - not mechanically per task or per phase
