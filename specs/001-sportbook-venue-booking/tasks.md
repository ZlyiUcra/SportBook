---

description: "Task list template for feature implementation"
---

# Tasks: SportBook Venue Booking

**Input**: Design documents from `/specs/001-sportbook-venue-booking/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api.md, quickstart.md

**Tests**: Included - the consilium transfer artifact (`.specify/consilium/2026-07-15-sportbook.md`)
and data-model.md make the booking-overlap and cancellation-cutoff behavior explicit test
requirements, not optional polish, so contract/integration/unit tests are part of each story.

**Organization**: Tasks are grouped by user story (from spec.md) to enable independent
implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- File paths are relative to repo root, matching plan.md Project Structure

## Path Conventions

- Backend: `backend/src/SportBook.{Api,Application,Domain,Infrastructure}/`,
  `backend/tests/SportBook.{UnitTests,IntegrationTests}/`
- Frontend: `frontend/src/`, `frontend/tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [ ] T001 Create backend solution layout: `backend/src/SportBook.Api`,
      `backend/src/SportBook.Application`, `backend/src/SportBook.Domain`,
      `backend/src/SportBook.Infrastructure` (.NET 10 class libraries / web project per
      plan.md Project Structure), add all four to `Solution1.sln`
- [ ] T002 Initialize `backend/src/SportBook.Api` as an ASP.NET Core 10 Web API project (MVC
      Controllers) referencing Application; add NuGet packages
      `Microsoft.AspNetCore.Authentication.JwtBearer`,
      `Microsoft.AspNetCore.DataProtection` (pinned >=10.0.7, CVE-2026-40372), and built-in
      `Microsoft.AspNetCore.OpenApi`
- [ ] T003 [P] Add `Npgsql.EntityFrameworkCore.PostgreSQL` and `Microsoft.EntityFrameworkCore`
      to `backend/src/SportBook.Infrastructure`; enable nullable reference types and
      file-scoped namespaces solution-wide per `CLAUDE.md`
- [ ] T004 [P] Create `backend/tests/SportBook.UnitTests` (xUnit +
      `Microsoft.EntityFrameworkCore.Sqlite` for in-memory EF Core) and
      `backend/tests/SportBook.IntegrationTests` (xUnit + `Microsoft.AspNetCore.Mvc.Testing`
      for `WebApplicationFactory`), referencing the relevant backend projects
- [ ] T005 [P] Initialize `frontend/` with Vite 7 + React 19 + TypeScript 5.9; set
      `base: ''` in `vite.config.ts` per `CLAUDE.md`; add `react-router-dom` and
      `@tanstack/react-query`
- [ ] T006 [P] Add Vitest + `@testing-library/react` + `@testing-library/jest-dom` to
      `frontend/` and a `frontend/tests/` setup file
- [ ] T007 [P] Verify `docker-compose.yml` Postgres service (host port 5434) starts cleanly
      with `docker compose up -d` and is reachable; document the connection string shape in
      `backend/src/SportBook.Api/appsettings.Development.json` (read from configuration, not
      hardcoded, per plan.md Storage constraint)

**Checkpoint**: Solution builds, both projects run empty, Postgres container reachable.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T008 [P] Create Domain enums in `backend/src/SportBook.Domain/Enums/`: `Role`,
      `SubscriptionTier`, `SportType`, `BookingStatus` per data-model.md
- [ ] T009 [P] Create Domain entities in `backend/src/SportBook.Domain/Entities/`: `User`,
      `RefreshToken`, `Venue`, `Court`, `Booking`, `Review` per data-model.md field/relationship
      tables (depends on T008)
- [ ] T010 Create `SportBookDbContext` in
      `backend/src/SportBook.Infrastructure/SportBookDbContext.cs` with entity configurations
      (depends on T009)
- [ ] T011 Isolate Npgsql provider registration to a single DI extension method
      `AddSportBookInfrastructure` in
      `backend/src/SportBook.Infrastructure/ServiceCollectionExtensions.cs`, called once from
      `backend/src/SportBook.Api/Program.cs`, per research.md portability decision (depends on
      T010)
- [ ] T012 Create the initial EF Core migration in `backend/src/SportBook.Infrastructure/Migrations/`
      including a concurrency-safe uniqueness/exclusion constraint preventing overlapping
      `Booking` rows for the same `CourtId` (data-model.md concurrency requirement) (depends on
      T010)
- [ ] T013 [P] Implement password hashing (`IPasswordHasher`) in
      `backend/src/SportBook.Application/Security/PasswordHasher.cs`
- [ ] T014 [P] Implement JWT issuance/validation (access + refresh token generation, claims
      including `sub`, `role`) in `backend/src/SportBook.Application/Security/TokenService.cs`
      (depends on T009 for `RefreshToken`)
- [ ] T015 Wire JWT bearer authentication + authorization policies in
      `backend/src/SportBook.Api/Program.cs` (depends on T014)
- [ ] T016 [P] Implement the error-handling middleware producing the
      `{ error: { code, message } }` shape in
      `backend/src/SportBook.Api/Middleware/ErrorHandlingMiddleware.cs`
- [ ] T017 [P] Implement the shared ownership-check helpers (Venue/Court/Booking ownership
      chains from research.md Authorization checklist) in
      `backend/src/SportBook.Application/Authorization/OwnershipChecks.cs` (depends on T009)
- [ ] T018 [P] Implement the shared `PagedResponse<T>` type and page/pageSize query-parameter
      binding in `backend/src/SportBook.Application/Common/PagedResponse.cs`
- [ ] T019 [P] Create `frontend/src/api/client.ts` (base fetch wrapper with auth header
      injection) and `frontend/src/context/AuthContext.tsx` (current user + token state)

**Checkpoint**: Foundation ready - user story implementation can now begin.

---

## Phase 3: User Story 1 - Book a sports court (Priority: P1) 🎯 MVP

**Goal**: A customer can search venues, see availability, book a court, and cancel a booking,
with the platform guaranteeing no double-booking.

**Independent Test**: Register a customer and a venue owner (via the owner endpoints stubbed in
this phase's setup, or seeded directly), search a venue, book an available slot, verify it
appears in booking history with the correct price, verify a second overlapping booking attempt
is rejected, and verify cancellation before/after the 2-hour cutoff behaves per FR-005.

### Tests for User Story 1

- [ ] T020 [P] [US1] Integration test: register, login, and refresh flow in
      `backend/tests/SportBook.IntegrationTests/AuthTests.cs`
- [ ] T021 [P] [US1] Integration test: book an available slot end-to-end, price computed
      server-side (spec Acceptance Scenario 1) in
      `backend/tests/SportBook.IntegrationTests/BookingCreationTests.cs`
- [ ] T022 [P] [US1] Integration test: overlapping booking is rejected, including two
      concurrent requests for the same slot where only one may succeed (spec Acceptance
      Scenario 2, FR-004) in
      `backend/tests/SportBook.IntegrationTests/BookingOverlapTests.cs`
- [ ] T023 [P] [US1] Integration test: cancellation is rejected inside the 2h cutoff and
      succeeds outside it (spec Acceptance Scenario 3, FR-005) in
      `backend/tests/SportBook.IntegrationTests/BookingCancellationTests.cs`
- [ ] T024 [P] [US1] Unit test: `TotalPrice` computation from `PricePerHour` and duration,
      ignoring any client-supplied price, in
      `backend/tests/SportBook.UnitTests/BookingPricingTests.cs`
- [ ] T025 [P] [US1] Unit test: overlap-check logic against a set of existing bookings
      (including cancelled bookings, which must not block) in
      `backend/tests/SportBook.UnitTests/BookingOverlapCheckTests.cs`

### Implementation for User Story 1

- [ ] T026 [US1] Implement `AuthService` (register/login/refresh/logout, forces
      `Role = Customer` on register per research.md) in
      `backend/src/SportBook.Application/Services/AuthService.cs` (depends on T013, T014)
- [ ] T027 [US1] Implement `AuthController` in
      `backend/src/SportBook.Api/Controllers/AuthController.cs` per contracts/api.md Auth
      section (depends on T026)
- [ ] T028 [P] [US1] Implement `VenueService.Search`/`GetById` (with `AsSplitQuery()` for
      Courts+Reviews per research.md) in
      `backend/src/SportBook.Application/Services/VenueService.cs` (depends on T011, T018)
- [ ] T029 [P] [US1] Implement `VenuesController` GET endpoints (list, get-by-id) in
      `backend/src/SportBook.Api/Controllers/VenuesController.cs` per contracts/api.md Venues
      section (depends on T028)
- [ ] T030 [P] [US1] Implement `CourtService.ListByVenue` and `CourtsController` GET
      list-by-venue in `backend/src/SportBook.Application/Services/CourtService.cs` and
      `backend/src/SportBook.Api/Controllers/CourtsController.cs` (depends on T011, T018)
- [ ] T031 [US1] Implement `AvailabilityService` (whole-hour free-slot computation per
      research.md) and `AvailabilityController` in
      `backend/src/SportBook.Application/Services/AvailabilityService.cs` and
      `backend/src/SportBook.Api/Controllers/AvailabilityController.cs` per contracts/api.md
      Availability section (depends on T011)
- [ ] T032 [US1] Implement `BookingService.Create` (server-computed `TotalPrice`,
      concurrency-safe overlap enforcement per T012's DB constraint, operating-hours
      validation) in `backend/src/SportBook.Application/Services/BookingService.cs` (depends
      on T011, T012)
- [ ] T033 [US1] Implement `BookingService.Cancel` (2h cutoff, owner-of-booking check via
      T017) in `backend/src/SportBook.Application/Services/BookingService.cs` (depends on
      T032, T017)
- [ ] T034 [US1] Implement `BookingsController` POST, GET (mine), GET-by-id, PUT cancel in
      `backend/src/SportBook.Api/Controllers/BookingsController.cs` per contracts/api.md
      Bookings section (depends on T032, T033)
- [ ] T035 [P] [US1] Frontend: typed request/response types + API calls for auth, venues,
      courts, availability, bookings in `frontend/src/api/` (depends on T019)
- [ ] T036 [P] [US1] Frontend: `Login`/`Register` pages wired to `AuthContext` in
      `frontend/src/pages/Login.tsx`, `frontend/src/pages/Register.tsx` (depends on T035)
- [ ] T037 [US1] Frontend: `VenueSearch` page (city/sport filter, paginated list) in
      `frontend/src/pages/VenueSearch.tsx` (depends on T035)
- [ ] T038 [US1] Frontend: `VenueDetail` page with court list, availability picker, and
      booking form in `frontend/src/pages/VenueDetail.tsx` (depends on T035, T037)
- [ ] T039 [US1] Frontend: `MyBookings` page (list + cancel action) in
      `frontend/src/pages/MyBookings.tsx` (depends on T035)

**Checkpoint**: User Story 1 is fully functional and independently testable/demoable.

---

## Phase 4: User Story 2 - Manage venue and courts (Priority: P2)

**Goal**: A venue owner can create/update/delete their own venues and courts, see only their
own venue's bookings, and confirm pending bookings - all scoped to their own resources.

**Independent Test**: As a venue owner, create a venue and court and verify it appears in
search; as a different venue owner, verify all write/read attempts on the first owner's
resources return 403; confirm a pending booking and verify its status changes.

### Tests for User Story 2

- [ ] T040 [P] [US2] Integration test: venue owner creates a venue and court, which becomes
      searchable/bookable (spec Acceptance Scenario 1) in
      `backend/tests/SportBook.IntegrationTests/VenueManagementTests.cs`
- [ ] T041 [P] [US2] Integration test: cross-owner access to venue/court/booking write and
      read endpoints returns 403 (spec Acceptance Scenario 2, SC-004, research.md
      Authorization checklist) in
      `backend/tests/SportBook.IntegrationTests/OwnershipBoundaryTests.cs`
- [ ] T042 [P] [US2] Integration test: owner confirms a pending booking; non-owner confirm
      attempt is rejected (spec Acceptance Scenario 3, FR-011) in
      `backend/tests/SportBook.IntegrationTests/BookingConfirmationTests.cs`
- [ ] T043 [P] [US2] Integration test: deleting a venue/court with an upcoming non-cancelled
      booking is rejected (FR-009) in
      `backend/tests/SportBook.IntegrationTests/VenueDeletionTests.cs`
- [ ] T044 [P] [US2] Unit test: ownership-check helpers for Venue/Court/Booking chains in
      `backend/tests/SportBook.UnitTests/OwnershipChecksTests.cs`

### Implementation for User Story 2

- [ ] T045 [US2] Implement `VenueService.Create`/`Update`/`Delete` (owner-only, FR-009 delete
      guard) in `backend/src/SportBook.Application/Services/VenueService.cs` (depends on T028,
      T017)
- [ ] T046 [US2] Implement `VenuesController` POST/PUT/DELETE in
      `backend/src/SportBook.Api/Controllers/VenuesController.cs` (depends on T045, T029)
- [ ] T047 [US2] Implement `CourtService.Create`/`Update`/`Delete` (owner-only via Venue
      chain, FR-009 delete guard) in
      `backend/src/SportBook.Application/Services/CourtService.cs` (depends on T030, T017)
- [ ] T048 [US2] Implement `CourtsController` POST/PUT/DELETE in
      `backend/src/SportBook.Api/Controllers/CourtsController.cs` (depends on T047, T030)
- [ ] T049 [US2] Implement `BookingService.ListByVenueForOwner` and `Confirm` (owner-only via
      Court->Venue chain) in
      `backend/src/SportBook.Application/Services/BookingService.cs` (depends on T032, T017)
- [ ] T050 [US2] Implement `BookingsController` GET `/venues/{id}/bookings` and PUT
      `/bookings/{id}/confirm` in
      `backend/src/SportBook.Api/Controllers/BookingsController.cs` (depends on T049, T034)
- [ ] T051 [P] [US2] Frontend: `OwnerDashboard` page (venue/court create/edit/delete forms)
      in `frontend/src/pages/OwnerDashboard.tsx` (depends on T035, T036)
- [ ] T052 [US2] Frontend: `OwnerBookings` page (list own venue's bookings, confirm action)
      in `frontend/src/pages/OwnerBookings.tsx` (depends on T035, T051)

**Checkpoint**: User Stories 1 AND 2 both work independently.

---

## Phase 5: User Story 3 - Build trust through reviews (Priority: P3)

**Goal**: Authenticated users can read and submit venue reviews, and see an up-to-date average
rating per venue.

**Independent Test**: As any authenticated user, submit a rating/comment for a venue and
verify it appears in that venue's review list and its average rating updates.

### Tests for User Story 3

- [ ] T053 [P] [US3] Integration test: submit a review, verify it appears in the venue's
      review list and the venue's average rating updates (spec Acceptance Scenarios 1-2) in
      `backend/tests/SportBook.IntegrationTests/ReviewTests.cs`
- [ ] T054 [P] [US3] Integration test: a second review by the same user for the same venue
      replaces the first rather than duplicating it (data-model.md Review validation rule) in
      `backend/tests/SportBook.IntegrationTests/ReviewUpsertTests.cs`

### Implementation for User Story 3

- [ ] T055 [US3] Implement `ReviewService` (list-by-venue paginated, create-or-replace,
      average rating aggregate query) in
      `backend/src/SportBook.Application/Services/ReviewService.cs` (depends on T011, T018)
- [ ] T056 [US3] Implement `ReviewsController` GET/POST in
      `backend/src/SportBook.Api/Controllers/ReviewsController.cs` per contracts/api.md
      Reviews section (depends on T055)
- [ ] T057 [US3] Frontend: review list + review submission form on `VenueDetail` page in
      `frontend/src/pages/VenueDetail.tsx` (depends on T035, T038)

**Checkpoint**: All three user stories are independently functional.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T058 [P] Run all `quickstart.md` validation scenarios end-to-end against a locally
      running stack (Docker Postgres + backend + frontend)
- [ ] T059 [P] Response-DTO whitelist audit across every controller - confirm
      `PasswordHash` and `Email` never appear in a response reachable by another user
      (security consilium finding)
- [ ] T060 [P] Add `Microsoft.AspNetCore.OpenApi` document annotations to all controllers for
      API documentation
- [ ] T061 Verify EF Core query plans for `GET /venues/{id}` (confirms `AsSplitQuery()` avoids
      the cartesian-explosion pattern from the performance consilium finding) and for
      `GET /courts/{id}/availability` (confirms an index exists on
      `Booking(CourtId, StartTime, EndTime)`)
- [ ] T062 [P] Update `backend/README.md` / `frontend/README.md` with setup commands mirroring
      quickstart.md (docker compose, dotnet, yarn)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational completion - no dependency on US2/US3
- **User Story 2 (Phase 4)**: Depends on Foundational completion; reuses Venue/Court/Booking
  services from US1 (T028, T030, T032) but adds owner-only write paths - implement after US1
  for a working demo path, though the two could proceed in parallel with two developers
- **User Story 3 (Phase 5)**: Depends on Foundational completion only - fully independent of
  US1/US2 implementation, could be built in parallel by a third developer
- **Polish (Phase 6)**: Depends on all desired user stories being complete

### Within Each User Story

- Tests are written before implementation and MUST fail first
- Domain/Application services before Controllers
- Controllers before frontend pages that consume them
- Story complete before moving to next priority (for solo/sequential execution)

### Parallel Opportunities

- All Setup tasks marked [P] (T003-T007) can run in parallel after T001-T002
- All Foundational tasks marked [P] (T008, T009, T013, T016-T019) can run in parallel where
  their dependencies are met
- Once Foundational completes, US1, US2, and US3 test-writing can start in parallel; US2/US3
  implementation tasks that touch shared services (VenueService, CourtService,
  BookingService) should follow US1's creation of those services to avoid merge conflicts in
  the same files

---

## Parallel Example: User Story 1

```bash
# Launch all US1 tests together (after Foundational phase):
Task: "Integration test: register, login, refresh in backend/tests/SportBook.IntegrationTests/AuthTests.cs"
Task: "Integration test: book available slot in backend/tests/SportBook.IntegrationTests/BookingCreationTests.cs"
Task: "Integration test: overlap rejection in backend/tests/SportBook.IntegrationTests/BookingOverlapTests.cs"
Task: "Integration test: cancellation cutoff in backend/tests/SportBook.IntegrationTests/BookingCancellationTests.cs"
Task: "Unit test: pricing in backend/tests/SportBook.UnitTests/BookingPricingTests.cs"
Task: "Unit test: overlap-check logic in backend/tests/SportBook.UnitTests/BookingOverlapCheckTests.cs"

# Launch independent US1 read-side services together:
Task: "VenueService.Search/GetById in backend/src/SportBook.Application/Services/VenueService.cs"
Task: "CourtService.ListByVenue in backend/src/SportBook.Application/Services/CourtService.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: run quickstart.md Scenarios 1, 2, 4 against User Story 1 alone
   (using directly-seeded venues/courts if US2 isn't built yet)
5. Demo the core booking flow

### Incremental Delivery

1. Setup + Foundational → foundation ready
2. User Story 1 → validate → demo (MVP: customers can book seeded venues)
3. User Story 2 → validate → demo (venue owners self-serve instead of seeded data)
4. User Story 3 → validate → demo (reviews layer on top)
5. Polish → quickstart.md full run, security/perf audits, docs

---

## Notes

- [P] tasks touch different files with no unmet dependency
- [Story] label maps each task to its user story for traceability
- Booking overlap safety (T012, T022, T025, T032) is the single highest-priority item per the
  consilium review - do not treat its tests as optional even under time pressure
- Commit after each task or logical group, per `CLAUDE.md` git workflow (confirm each commit
  with the user)
- Stop at any checkpoint to validate a story independently before moving on
