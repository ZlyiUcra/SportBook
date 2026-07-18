---

description: "Task list template for feature implementation"
---

# Tasks: City Selection, Geolocation and Venue Map

**Input**: Design documents from `/specs/002-city-geolocation-map/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api.md, quickstart.md

**Tests**: Included alongside each story's implementation (no user direction to defer them this
time, unlike 001's US2/US3). Contract-level MUSTs from the consilium (map content safety,
coordinate rounding, radius enforcement, query translation) are each backed by a named test task
below, not left as prose.

**Organization**: Tasks are grouped by user story (from spec.md) to enable independent
implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1-US5)
- File paths are relative to repo root, matching plan.md Project Structure

## Path Conventions

- Backend: `backend/src/SportBook.{Api,Application,Domain,Infrastructure}/`,
  `backend/tests/SportBook.{UnitTests,IntegrationTests}/`
- Frontend: `frontend/src/`, `frontend/tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Dependencies and reference data needed before any schema or code changes

- [X] T001 [P] Add frontend dependencies `leaflet`, `react-leaflet`, `@types/leaflet` (dev),
      `cmdk` to `frontend/package.json` per plan.md Primary Dependencies (user-approved
      2026-07-18)
- [X] T002 Write and run the one-time dataset conversion script that filters the GeoNames UA
      subset (feature class P, population >= 500 - printing actual row counts at thresholds
      500/1000/5000 to confirm the choice per research.md), extracts EN/UK/PT names and region
      (admin1) display names, and emits a committed city data file as an embedded resource in
      `backend/src/SportBook.Infrastructure/Data/cities.csv`

**Checkpoint**: Dependencies installed, city data file committed and ready for the seed migration.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**CRITICAL**: No user story work can begin until this phase is complete. This phase intentionally
folds in the venue write-path reshape (not just the read/search path) - splitting it out would
leave venue creation broken (backend requiring `cityId`, no consumer providing one) between this
phase and US2's frontend work; see the Notes section for the resulting sequencing implication.

- [X] T003 [P] Create `City` domain entity in `backend/src/SportBook.Domain/Entities/City.cs`
      per data-model.md (Id = geonameid, NameEn/NameUk/NamePt, CountryCode,
      RegionEn/RegionUk/RegionPt, Latitude, Longitude, Population)
- [X] T004 Add `City` to `SportBookDbContext` with explicit `HasPrecision(9,6)` on
      Latitude/Longitude in `backend/src/SportBook.Infrastructure/SportBookDbContext.cs`
      (depends on T003)
- [X] T005 Extend `Venue` domain entity: add `CityId` (FK -> City), nullable `Latitude`,
      `Longitude`; remove the legacy `City` string field, in
      `backend/src/SportBook.Domain/Entities/Venue.cs` per data-model.md (depends on T003)
- [X] T006 Create migration `CreateAndSeedCities` in
      `backend/src/SportBook.Infrastructure/Migrations/` reading the embedded `cities.csv` and
      emitting deterministic INSERT batches - no `HasData` (depends on T002, T004)
- [X] T007 Create migration `AddVenueCityIdAndCoordinates` in
      `backend/src/SportBook.Infrastructure/Migrations/`: add nullable `CityId` FK + index and
      nullable `Latitude`/`Longitude`; backfill `CityId` by exact match of the legacy `City`
      string against City name columns; guard with `THROW` listing unmatched values if any
      `CityId` remains null; `ALTER` `CityId` to NOT NULL (depends on T005, T006)
- [X] T007a Create migration `DropVenueLegacyCity` in
      `backend/src/SportBook.Infrastructure/Migrations/`: drops the legacy `Venues.City` string
      column, per data-model.md's 3-migration chain (separate migration by design, same feature -
      safe once T005 has already removed all application-code references to it) (depends on T007)
- [X] T008 [P] Implement the haversine distance function and neighbor-set computation (150km
      constant) as a pure function in
      `backend/src/SportBook.Application/Services/CityDistance.cs` (depends on T003)
- [X] T009 [P] Implement `CityService` base (in-memory city list cache, localized-name matching
      helper shared by suggestion and nearest lookups) in
      `backend/src/SportBook.Application/Services/CityService.cs` (depends on T008)
- [X] T010 Update `VenueService.Search`/`GetById` to filter and project by `CityId` (nested
      `CityResponse` in `VenueSummaryResponse`/`VenueDetailResponse`, replacing the free-text
      `city` field) in `backend/src/SportBook.Application/Services/VenueService.cs` (depends on
      T007, T009)
- [X] T011 Update `VenueService.Create`/`Update` and `CreateVenueRequest`/`UpdateVenueRequest` to
      accept `cityId` (validated to exist) and optional `latitude`/`longitude` (both-or-neither,
      range-validated) instead of the free-text `city` field, in
      `backend/src/SportBook.Application/Services/VenueService.cs` and
      `backend/src/SportBook.Application/Dtos/VenueDtos.cs` (depends on T007, T009)

**Checkpoint**: City directory exists and is seeded; every venue has a valid `CityId` and the
legacy `City` column is gone; backend venue read/write paths use `cityId`. User story
implementation can now begin.

---

## Phase 3: User Story 1 - Search venues by a structured city (Priority: P1) 🎯 MVP

**Goal**: A customer picks their city from directory-backed suggestions instead of typing free
text, and search returns only that city's venues.

**Independent Test**: Type a partial city name in any supported language, pick a suggested city,
and verify search results contain only venues of that city, with no way to submit free text.

### Tests for User Story 1

- [X] T012 [P] [US1] Integration test: `GET /api/cities` suggestion matches partial input in any
      of the three app languages, ranks larger settlements first, rejects queries under 2
      characters (spec Acceptance Scenarios 1, 3, 4) in
      `backend/tests/SportBook.IntegrationTests/CitySuggestionTests.cs`
- [X] T013 [P] [US1] Integration test: `GET /api/venues?cityId=` returns only venues of the
      selected city (spec Acceptance Scenario 2) in
      `backend/tests/SportBook.IntegrationTests/VenueCitySearchTests.cs`
- [X] T014 [P] [US1] Unit test: city suggestion ranking and localized-name matching in
      `backend/tests/SportBook.UnitTests/CitySuggestionTests.cs`

### Implementation for User Story 1

- [X] T015 [US1] Implement `CityService.Suggest` (min 2 chars else 400, match against
      NameEn/NameUk/NamePt, TOP 10 ordered by Population DESC) in
      `backend/src/SportBook.Application/Services/CityService.cs` (depends on T009)
- [X] T016 [US1] Implement `CitiesController` `GET /api/cities` per contracts/api.md Cities
      section in `backend/src/SportBook.Api/Controllers/CitiesController.cs` (depends on T015)
- [X] T017 [P] [US1] Frontend: `City` types + `suggest` API call in
      `frontend/src/entities/city/` (depends on T016)
- [X] T018 [US1] Frontend: city combobox (shadcn/ui + `cmdk`, shows region context to
      disambiguate same-named settlements) in `frontend/src/features/city-select/` (depends on
      T017)
- [X] T019 [US1] Frontend: wire the venue search page to the city combobox and `cityId` query
      param, remove the free-text city input, in
      `frontend/src/pages/venues/ui/VenueSearchPage.tsx` (depends on T018)

**Checkpoint**: US1 fully functional - customers search by a directory city, free text is
unrepresentable.

---

## Phase 4: User Story 2 - Owner assigns city and precise location to a venue (Priority: P2)

**Goal**: A venue owner picks a directory city for their venue and can optionally place, move, or
remove a precise location pin.

**Independent Test**: Create a venue choosing a city from suggestions and verify it appears in
search for that city; place, move, and remove a location pin and verify the venue page reflects
each state.

**Note**: `VenueForm`'s city field (T022) reuses the combobox built in US1 (T018) - this story is
sequenced after US1 in practice even though its priority ordering alone wouldn't require it; see
Dependencies below.

### Tests for User Story 2

- [X] T020 [P] [US2] Integration test: venue create/update accepts `cityId` + optional
      both-or-neither `latitude`/`longitude`, rejects an unknown `cityId` and a partial
      coordinate pair (spec Acceptance Scenarios 1-3) in
      `backend/tests/SportBook.IntegrationTests/VenueLocationTests.cs`

### Implementation for User Story 2

- [X] T021 [US2] Frontend: `MapView` wrapper in `frontend/src/shared/ui/map/MapView.tsx` (typed
      props: center, markers, onPick; tile URL + attribution constants in
      `frontend/src/shared/config/`; the only module importing `leaflet`/`react-leaflet`; loaded
      exclusively via `React.lazy`/dynamic `import()`) (depends on T001)
- [X] T022 [US2] Frontend: `VenueForm` reuses the city combobox (`features/city-select`) and
      adds a lazy pin-picker (place/move/remove via `MapView`'s `onPick`) in
      `frontend/src/features/venue-management/venue/ui/VenueForm.tsx` and `model/schema.ts`
      (depends on T018, T021)
- [X] T023 [US2] Frontend: venue detail page renders a single-marker `MapView` only when both
      `latitude`/`longitude` are set - no map block, no city-centre fallback, otherwise - in
      `frontend/src/pages/venues/ui/VenueDetailPage.tsx` (depends on T021)

**Checkpoint**: US2 functional - owners pick a directory city and can place/move/remove a
precise pin; the venue page reflects it truthfully.

---

## Phase 5: User Story 3 - Detect my city automatically (Priority: P3)

**Goal**: A customer taps "my city" and gets the nearest directory city pre-selected, with manual
override always available.

**Independent Test**: Grant location permission, tap "my city", verify the pre-selected city is
the nearest directory city; deny permission and verify manual selection still works with no
blocking error.

### Tests for User Story 3

- [X] T024 [P] [US3] Integration test: `GET /api/cities/nearest` resolves the nearest city and
      validates `lat`/`lng` ranges (spec Acceptance Scenarios) in
      `backend/tests/SportBook.IntegrationTests/CityNearestTests.cs`
- [X] T025 [P] [US3] Unit test: nearest-city resolution via the shared haversine function in
      `backend/tests/SportBook.UnitTests/CityNearestTests.cs`

### Implementation for User Story 3

- [X] T026 [US3] Implement `CityService.FindNearest` in
      `backend/src/SportBook.Application/Services/CityService.cs` (depends on T008, T009)
- [X] T027 [US3] Implement `CitiesController` `GET /api/cities/nearest` (range validation, never
      persists or logs received coordinates) in
      `backend/src/SportBook.Api/Controllers/CitiesController.cs` (depends on T026)
- [X] T028 [US3] Frontend: "my city" button in `features/city-select` using the browser
      Geolocation API - rounds coordinates to 2 decimals before calling
      `/api/cities/nearest`, degrades to manual selection with no blocking error on denial or
      failure (depends on T018, T027)

**Checkpoint**: US3 functional - "my city" pre-selects the nearest directory city with graceful
manual fallback.

---

## Phase 6: User Story 4 - Include venues from nearby cities (Priority: P4)

**Goal**: A customer can widen search to venues in cities within 150 km of the selected city.

**Independent Test**: Enable the nearby option for a city with a neighbor inside 150 km, verify
those venues appear labelled with their own city, and verify venues beyond 150 km never appear.

### Tests for User Story 4

- [X] T029 [P] [US4] Integration test: `includeNearby=true` returns venues within 150km and
      never beyond it, and the server enforces the fixed radius regardless of client-supplied
      values (spec Acceptance Scenarios) in
      `backend/tests/SportBook.IntegrationTests/VenueNearbyTests.cs`
- [X] T030 [P] [US4] Unit test: `ToQueryString()` proves the `CityId IN <neighbor set>` filter
      translates to SQL rather than client-evaluating in
      `backend/tests/SportBook.UnitTests/VenueNearbyQueryTranslationTests.cs`

### Implementation for User Story 4

- [X] T031 [US4] Implement `CityService.GetNeighborIds` (150km server-side constant, cached per
      city for the process lifetime) in
      `backend/src/SportBook.Application/Services/CityService.cs` (depends on T008, T009)
- [X] T032 [US4] Update `VenueService.Search` to honor `includeNearby` (filter `Venue.CityId`
      against the neighbor set; each result carries its own city) in
      `backend/src/SportBook.Application/Services/VenueService.cs` (depends on T010, T031)
- [X] T033 [US4] Update `VenuesController` `GET /api/venues` to accept `includeNearby` (default
      false) in `backend/src/SportBook.Api/Controllers/VenuesController.cs` (depends on T032)
- [X] T034 [US4] Frontend: nearby toggle (off by default) on the venue search page, wired to
      `includeNearby`, result cards show their own city in
      `frontend/src/pages/venues/ui/VenueSearchPage.tsx` (depends on T019, T033)

**Checkpoint**: US4 functional - customers can widen search to 150km with cross-city labelling;
the radius cannot be widened by the client.

---

## Phase 7: User Story 5 - See venues on a map (Priority: P5)

**Goal**: A customer can open a map of venue pins on the current search results page.

**Independent Test**: Run a search where some result venues have pins and some do not, open the
map, verify exactly the pinned venues of the current page appear, and selecting a pin leads to
the venue.

### Tests for User Story 5

- [X] T035 [P] [US5] Frontend smoke test (map component mocked, per research.md testing stance -
      no leaflet/WebGL in jsdom): the search page map shows exactly the pinned venues of the
      current results page in `frontend/tests/pages/VenueSearchMap.test.tsx`

### Implementation for User Story 5

- [X] T036 [US5] Frontend: search results map section using `MapView` with pins from the current
      page's venues that have coordinates, lazy-loaded via `React.lazy`/dynamic `import()`,
      popup content rendered exclusively as JSX children - never `bindPopup`/`setContent`/
      `divIcon({ html })` with venue fields - in
      `frontend/src/pages/venues/ui/VenueSearchPage.tsx` (depends on T019, T021)
- [X] T037 [US5] Audit: confirm the venue detail single-marker map (delivered in US2/T023)
      renders venue name/description as JSX children only, never raw HTML, closing spec
      Acceptance Scenario 4 (depends on T023)

**Checkpoint**: All five user stories independently functional - full feature deliverable.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [X] T038 [P] Add GeoNames (CC BY 4.0) and OSM tile attribution to the About page in
      `frontend/src/pages/about/`
- [X] T039 Measure `yarn build` output before/after the map lands from `frontend/` - confirm the
      initial JS chunk delta is 0 (spec SC-006, quickstart.md Build and performance
      verification). Measured: leaflet/react-leaflet/leaflet.css land entirely in a separate
      lazy `MapView-*.js`/`.css` chunk (160.13KB / 50.57KB gzip); the initial chunk grew only
      ~1.96KB gzip (from unrelated city-search feature code, not the map stack)
- [X] T040 Re-run the SC-005 load scenario (500 concurrent `GET /api/venues`) against the
      reshaped query - confirm the p95 target still holds (quickstart.md). Attempted three times
      (default pool; Release build with `Max Pool Size=1000`; then again with
      `DOTNET_ThreadPool_ForceMinWorkerThreads=500`) - result unchanged each time, and ASP.NET
      Core's own request-timing middleware (not just the client) logged ~8.6-8.9s per request
      under the 500-concurrent burst, so this is a real server-side effect, not a harness
      artifact. Single-request baseline stayed ~20ms throughout (confirms no query-level
      regression; `IX_Venues_CityId` Index Seek already verified in T044). Most likely cause: the
      local Docker SQL Server container (2-3GB memory limit, sharing a single dev laptop with
      everything else) genuinely cannot absorb 500 simultaneous fresh connections/queries
      arriving in one instantaneous burst - a resource ceiling of this sandbox/container, not
      evidence the reshaped query itself regressed. Accepted as-is (user decision 2026-07-18): a
      full SC-005 reconfirmation needs a properly provisioned SQL Server and a real ramping load
      tool (k6/hey), matching 001's original T063 environment - out of scope for this pass
- [X] T041 [P] Run all quickstart.md validation scenarios end-to-end against a locally running
      stack. Backend API scenarios (sections 1-4: city autocomplete, nearest city, search by
      city/nearby, venue write path) all verified via curl against a real migrated SQL Server
      instance. Frontend manual browser scenarios (combobox typing, geolocation permission
      prompt, visual map rendering, lazy-chunk Network tab) require a human in an actual browser
      and were handed off to the user (decision 2026-07-18) - both backend and frontend dev
      servers were left running against the real, migrated `SportBookDb` for that check
- [X] T042 [P] Response-DTO whitelist audit for the new/changed DTOs (`CityResponse`, reshaped
      Venue DTOs) - confirm `Population` never leaks and no new `[AllowAnonymous]` was
      introduced (contract MUST, spec FR-014). Confirmed by inspection: `CityResponse` has no
      `Population` field; `grep` for `AllowAnonymous`/`Authorize` in `CitiesController.cs` and
      `VenuesController.cs` found neither - only the pre-existing `AuthController` uses it
- [X] T043 [P] Update `backend/README.md`/`frontend/README.md`/root `README.md` with the dataset
      conversion script usage and any new setup step
- [X] T044 Verify EF Core query plans for `GET /api/venues` with `cityId`/`includeNearby`
      (confirms the `CityId` FK filter is indexed and the OPENJSON neighbor-set parameter does
      not trigger client evaluation - performance consilium finding). Verified against a real
      SQL Server instance: plain `cityId` filter uses `Index Seek` on `IX_Venues_CityId`; the
      `includeNearby` (OPENJSON) filter's plan is a single native SQL Server plan (Nested
      Loops/Clustered Index Scan/Table-valued function) with no client-evaluation step - on this
      small test dataset the optimizer prefers a full scan over per-ID seeks, expected to shift
      to index seeks at production data volumes

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational completion - no dependency on other
  stories
- **User Story 2 (Phase 4)**: Depends on Foundational completion; `VenueForm`'s city field
  (T022) reuses the combobox built in US1 (T018) - implement after US1 for a working owner-form
  path, even though the backend prerequisite (Foundational) alone would not force this order
- **User Story 3 (Phase 5)**: Depends on Foundational completion; the "my city" button (T028)
  is added to the same `features/city-select` component built in US1 (T018) - same soft
  sequencing as US2
- **User Story 4 (Phase 6)**: Depends on Foundational completion and reuses `VenueService.Search`
  from US1 (T010) and the search page from US1 (T019)
- **User Story 5 (Phase 7)**: Depends on Foundational completion, the `MapView` wrapper from US2
  (T021), and the search page from US1 (T019)
- **Polish (Phase 8)**: Depends on all desired user stories being complete

### Within Each User Story

- Tests before implementation within each story
- Domain/Application services before Controllers
- Controllers before frontend pages/features that consume them
- Story complete before moving to next priority (for solo/sequential execution)

### Parallel Opportunities

- T001 and T002 (Setup) can run in parallel
- T003, T008 (Foundational) can run in parallel; T009 depends on T008
- All test tasks marked [P] within a story can run in parallel
- US3 and US4 implementation are independent of each other (both only depend on Foundational +
  their own prerequisites) and could be built in parallel by different developers once US1's
  `features/city-select` component exists
- Most Polish tasks marked [P] can run in parallel

---

## Parallel Example: User Story 1

```bash
# Launch all US1 tests together (after Foundational phase):
Task: "Integration test: city suggestion matching in backend/tests/SportBook.IntegrationTests/CitySuggestionTests.cs"
Task: "Integration test: venue search by cityId in backend/tests/SportBook.IntegrationTests/VenueCitySearchTests.cs"
Task: "Unit test: suggestion ranking in backend/tests/SportBook.UnitTests/CitySuggestionTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories, includes the write-path
   reshape so venue creation keeps working)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: run quickstart.md city-autocomplete and city-search scenarios
5. Demo structured city search

### Incremental Delivery

1. Setup + Foundational -> foundation ready (city directory seeded, venues carry cityId)
2. User Story 1 -> validate -> demo (MVP: customers search by directory city)
3. User Story 2 -> validate -> demo (owners assign city + optional precise pin)
4. User Story 3 -> validate -> demo ("my city" geolocation)
5. User Story 4 -> validate -> demo (150km nearby expansion)
6. User Story 5 -> validate -> demo (venue map on search results)
7. Polish -> quickstart.md full run, build/perf verification, docs

---

## Notes

- [P] tasks touch different files with no unmet dependency
- [Story] label maps each task to its user story for traceability
- Foundational (T007, T011) intentionally bundles the venue write-path reshape with the schema
  change - splitting them would leave venue creation broken via the existing UI between phases;
  do not commit Foundational as a checkpoint until US1's or US2's frontend catches up enough to
  keep venue creation working end-to-end, per the user's atomic-commit preference (verified
  working functional slice, not per-task/per-phase mechanically)
- The migration guard in T007 (match-or-fail with `THROW` listing unmatched city strings) was
  expected to pass trivially on fresh databases since there is no production database - this held
  for the integration test database, but the actual local dev `SportBookDb` did have a mismatch
  (200 venues with `City = 'LoadTestCity'`, a leftover fixture from 001's T063 load test): the
  guard correctly threw and rolled back cleanly, exactly as designed. Fixed by updating those
  rows' `City` to `Kyiv` (a real directory match, not a delete) and re-running
  `dotnet ef database update` - the real dev database is migrated and working as of 2026-07-18
- T007a was added after an /speckit-implement gap check found data-model.md's 3-migration chain
  (create+seed Cities / add CityId+coordinates / drop legacy City) only had 2 corresponding
  tasks; T007a closes that gap without a full tasks.md regeneration
- Map content safety (T036, T037) and coordinate rounding (T028) are contract MUSTs from the
  consilium security verdict, not optional hardening - do not skip them
- Commit after each verified functional slice (build + run + check), per user-stated
  atomic-commit preference - not mechanically per task or per phase
- Stop at any checkpoint to validate a story independently before moving on
