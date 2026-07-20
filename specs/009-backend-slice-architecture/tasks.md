# Tasks: Backend rearchitecture to vertical slice architecture

**Input**: Design documents from `/specs/009-backend-slice-architecture/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api.md, quickstart.md

**Tests**: Included where behavior could silently drift (see research.md's two gotchas) -
frontend-only-style regression coverage isn't applicable here since this is backend-only; the
pre-existing 51 unit + 71 integration tests are the primary regression net, plus one new test.

**Organization**: Tasks are grouped by user story (from spec.md). All tasks below are already
complete - this file documents the delivered work, the same close-out style used for specs
006-008.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Could have run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- File paths are relative to repo root

## Path Conventions

- Backend only: `backend/src/`, `backend/tests/` - no frontend changes (spec Assumptions)

---

## Phase 1: Setup (Shared Infrastructure)

No setup tasks - this extends an existing, fully-scaffolded four-project solution
(`SportBook.Api`/`Application`/`Domain`/`Infrastructure`); nothing new to initialize.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The dispatch mechanism every Feature slice (US1) and every endpoint wire-up (US2)
needs before either can exist.

**⚠️ CRITICAL**: Blocks Phase 3 and Phase 4. Does NOT block Phase 5 (US3, the Minimal API
conversion, has no dependency on the dispatch mechanism and was shipped independently).

- [x] T001 [P] Add `Mediator.Abstractions` + `Mediator.SourceGenerator` 3.0.2 package references
      to `backend/src/SportBook.Api/SportBook.Api.csproj` and
      `backend/src/SportBook.Application/SportBook.Application.csproj` (research.md Decision 2)
- [x] T002 Register the dispatcher in `backend/src/SportBook.Api/Program.cs` via
      `services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped)` - the
      library's Singleton default fails DI validation against every handler's scoped
      `SportBookDbContext` (research.md Decision 2 gotcha) (depends on T001)

**Checkpoint**: Dispatch mechanism ready - Feature slices and endpoint wiring can now proceed.

---

## Phase 3: User Story 1 - Find and change one use case without touching unrelated code (Priority: P1) 🎯 MVP

**Goal**: Every client-reachable action lives in its own self-contained folder (a request
definition plus its handler), replacing the prior flat, multi-purpose service classes.

**Independent Test**: Pick any one action (e.g. "cancel a booking"); confirm its request shape
and its handling logic are both defined in one narrowly-scoped location with no unrelated action
in the same file.

### Implementation for User Story 1

- [x] T003 [P] [US1] Availability slice: `GetAvailabilityQuery`/`Handler` in
      `backend/src/SportBook.Application/Features/Availability/GetAvailability/GetAvailability.cs`,
      replacing `Services/AvailabilityService.cs` (depends on T002)
- [x] T004 [P] [US1] Cities slices: `SuggestCitiesQuery`/`Handler` and
      `FindNearestCityQuery`/`Handler` under
      `backend/src/SportBook.Application/Features/Cities/`; `Services/CityService.cs` slimmed to
      its internal-only `GetNeighborIdsAsync` collaborator (data-model.md Shared component)
      (depends on T002)
- [x] T005 [P] [US1] Auth slices: Register/Login/Refresh/Logout Command+Handler pairs under
      `backend/src/SportBook.Application/Features/Auth/`; shared token issuance extracted to
      `Services/AuthTokenIssuer.cs`; `Services/AuthService.cs` removed (depends on T002)
- [x] T006 [P] [US1] Courts slices: ListCourtsByVenue/CreateCourt/UpdateCourt/DeleteCourt under
      `backend/src/SportBook.Application/Features/Courts/`; `Services/CourtService.cs` removed
      (depends on T002)
- [x] T007 [P] [US1] Reviews slices: ListReviewsByVenue/CreateOrReplaceReview under
      `backend/src/SportBook.Application/Features/Reviews/` (the create-or-replace result kept as
      a named `CreateOrReplaceReviewResult` record, preserving the 200-vs-201 distinction);
      `Services/ReviewService.cs` removed (depends on T002)
- [x] T008 [P] [US1] Venues slices: SearchVenues/SearchNearbyVenues/GetVenueById/CreateVenue/
      UpdateVenue/DeleteVenue under `backend/src/SportBook.Application/Features/Venues/`; shared
      `Services/VenueDetailReader.cs` and `Services/VenueLocationValidator.cs` extracted so
      CreateVenue/UpdateVenue call the same detail-read logic as GetVenueById directly, not
      through a nested `mediator.Send` (research.md Decision 3); `Services/VenueService.cs`
      removed (depends on T002)
- [x] T009 [US1] Bookings slices: CreateBooking/CancelBooking/GetBookingById/ListMyBookings/
      ListVenueBookingsForOwner/ConfirmBooking under
      `backend/src/SportBook.Application/Features/Bookings/`; shared query/validation helpers
      extracted to static `Services/BookingHelpers.cs`; CreateBooking's serializable-transaction
      deadlock-retry loop carried over verbatim (given dedicated review per the mediator-adoption
      consilium's nitpicker finding); `Services/BookingService.cs` removed (depends on T002)
- [x] T010 [US1] Users slice: `GetMeQuery`/`Handler` in
      `backend/src/SportBook.Application/Features/Users/GetMe/GetMe.cs` - authored fresh, since
      this endpoint had no prior service to convert (depends on T002)

**Checkpoint**: Every action has a self-contained Handler; picking any one and reading only its
own file/folder is sufficient to understand and change its logic.

---

## Phase 4: User Story 2 - Every backend action follows one uniform shape (Priority: P2)

**Goal**: Every HTTP endpoint hands its request off through the identical dispatch call, and the
prior flat service layer no longer exists as an alternate calling convention.

**Independent Test**: Compare two unrelated actions from different resources (e.g. "register a
new account" and "delete a court"); confirm both are dispatched the same way from their endpoint.

### Implementation for User Story 2

- [x] T011 [P] [US2] Wire `backend/src/SportBook.Api/Endpoints/AvailabilityEndpoints.cs`,
      `CitiesEndpoints.cs`, and `AuthEndpoints.cs` to dispatch via `IMediator.Send(...)` instead
      of a direct service-method call (depends on T003, T004, T005)
- [x] T012 [P] [US2] Wire `CourtsEndpoints.cs`, `ReviewsEndpoints.cs`, `VenuesEndpoints.cs`,
      `BookingsEndpoints.cs`, and `UsersEndpoints.cs` to dispatch via `IMediator.Send(...)`
      (depends on T006, T007, T008, T009, T010)
- [x] T013 [US2] Remove DI registrations for the six now-deleted service classes in
      `backend/src/SportBook.Application/ServiceCollectionExtensions.cs`, replacing them with the
      shared-collaborator registrations from T004-T009 (depends on T011, T012)

**Checkpoint**: Every one of the 26 actions is reached from its endpoint through the identical
`mediator.Send` call; no endpoint calls a service method directly anymore.

---

## Phase 5: User Story 3 - The web framework layer stays a thin, swappable shell (Priority: P3)

**Goal**: Every HTTP endpoint definition contains only routing information and a single hand-off
call - no business logic, database access, or validation of its own.

**Independent Test**: Read any endpoint definition; confirm it contains no database query or
business rule, only route/verb registration and a hand-off.

**Note**: Independent of Phases 2-4 by design (research.md Decision 1) - this story does not
depend on the dispatch mechanism and was implemented and shipped first, in its own commit
(`ce6f30a`), before the mediator-adoption consilium even ran.

### Implementation for User Story 3

- [x] T014 [P] [US3] Convert `AuthController`/`AvailabilityController`/`BookingsController`/
      `CitiesController` to Minimal API `MapXxxEndpoints` files under
      `backend/src/SportBook.Api/Endpoints/`, preserving every route/verb/status/auth requirement
- [x] T015 [P] [US3] Convert `CourtsController`/`ReviewsController`/`UsersController`/
      `VenuesController` to Minimal API `MapXxxEndpoints` files, same preservation rule
- [x] T016 [US3] Replace `AddControllers()`/`MapControllers()` with `ConfigureHttpJsonOptions`
      (carrying the `JsonStringEnumConverter` over - research.md Decision 1 gotcha) and 8
      `MapXxxEndpoints()` calls in `Program.cs`; delete `backend/src/SportBook.Api/Controllers/`
      (depends on T014, T015)

**Checkpoint**: Zero MVC controllers remain; every endpoint is a Minimal API registration.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Gaps the endpoint-binder switch (Phase 5) exposed, regression coverage, and
documentation - affects all three stories.

- [x] T017 [P] Fix binding gaps Minimal API's stricter defaults exposed: explicit C# default
      values for `bool includeNearby`/`mine` and `BookingStatusFilter status`; converted
      `PageRequest` (`backend/src/SportBook.Application/Common/PagedResponse.cs`) from a
      property-only record to a positional record with default constructor parameters so
      `[AsParameters]` treats `page`/`pageSize` as optional (research.md Decision 4) - found by
      18 integration tests failing on first run after Phase 5, all one root cause
- [x] T018 [P] Add `VenueManagementTests.
      Creating_a_court_with_a_raw_string_valued_sportType_body_succeeds` - a raw JSON string body
      regression test, since the existing suite's own request serialization is structurally blind
      to the enum-converter gap (research.md Decision 1 gotcha; contracts/api.md)
- [x] T019 [P] Update the 9 unit/integration test files that constructed the old service classes
      directly (`AuthTests.cs`, `ApiClientExtensions.cs`, `CitySuggestionTests.cs`,
      `ReviewEligibilityTests.cs`, `ReviewEditWindowTests.cs`, `ReviewEditCommentTests.cs`,
      `BookingPricingTests.cs`, `BookingOverlapCheckTests.cs`, `BookingStatusFilterTests.cs`,
      `VenueNearbyDistanceTests.cs`) to construct the new Handlers instead
- [x] T020 [P] Pin `Mediator.Abstractions`/`Mediator.SourceGenerator` to the exact resolved
      version (`3.0.2`) in both `.csproj` files, replacing the initial floating `3.0.*` range -
      every other package in these files is pinned exactly, no exception for a new dependency
- [x] T021 [P] Update `README.md` (Components tree, Backend stack line) and `backend/README.md`
      (Project layout) to describe Minimal API endpoints and Feature slices, replacing the
      "controllers"/"services" wording left over from before this rework
- [x] T022 Run the full quickstart.md verification: clean `dotnet build` (0 warnings beyond the
      pre-existing NU1510/NU1903), 51 unit + 72 integration tests green, manual curl checklist
      (auth flow, pagination clamp, nested-route auth, GetMe scope, raw-string enum body) against
      a live server, throwaway test data cleaned up afterward

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: empty - nothing to initialize
- **Foundational (Phase 2)**: no dependencies - BLOCKS Phase 3 and Phase 4 only
- **User Story 1 (Phase 3)**: depends on Phase 2 (needs `IRequest`/`IRequestHandler` from the
  package, and the DI container to resolve handlers)
- **User Story 2 (Phase 4)**: depends on Phase 3 (an endpoint can only dispatch to a Handler that
  already exists)
- **User Story 3 (Phase 5)**: NO dependency on Phase 2, 3, or 4 - independent by design
  (research.md Decision 1), shipped first in practice
- **Polish (Phase 6)**: depends on Phases 3-5 all being complete (T017 was discovered by running
  the suite against the finished Phase 5 + Phase 4 combination)

### Within Each User Story

- US1: the 8 resource groups (T003-T010) are mutually independent - different files, all depend
  only on Phase 2, not on each other
- US2: T011/T012 (endpoint wiring) depend on their corresponding US1 slice tasks; T013 (DI
  cleanup) depends on both being done, so nothing still references a class about to be deleted
- US3: T014/T015 (controller-by-controller conversion) are mutually independent; T016
  (`Program.cs` + deleting `Controllers/`) depends on both being complete first

### Parallel Opportunities

- US1: T003-T008 (6 of 8 resource groups - different files, all depend only on Foundational)
- US2: T011 and T012 (different endpoint files)
- US3: T014 and T015 (different controller files)
- Polish: T017-T021 (different files/concerns)

---

## Parallel Example: User Story 1

```bash
# 6 of the 8 resource groups have no inter-dependency once Foundational (T002) is done:
Task: "Availability slice in Features/Availability/GetAvailability/GetAvailability.cs"
Task: "Cities slices in Features/Cities/{SuggestCities,FindNearestCity}/"
Task: "Auth slices in Features/Auth/{Register,Login,Refresh,Logout}/"
Task: "Courts slices in Features/Courts/{ListCourtsByVenue,CreateCourt,UpdateCourt,DeleteCourt}/"
Task: "Reviews slices in Features/Reviews/{ListReviewsByVenue,CreateOrReplaceReview}/"
Task: "Venues slices in Features/Venues/{SearchVenues,...,DeleteVenue}/"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 2: Foundational (dispatch mechanism)
2. Complete Phase 3: User Story 1 (all 26 actions as self-contained slices)
3. **STOP and VALIDATE**: pick any one action and confirm it's fully understandable from its own
   folder alone
4. Demo: "find and change booking cancellation" without opening any other action's file

### Incremental Delivery (as actually shipped)

1. User Story 3 (Minimal API conversion) → validate → commit (`ce6f30a`) - independent, shipped
   first
2. Foundational + User Story 1 + User Story 2 (dispatch mechanism, slices, endpoint wiring) →
   validate together (they share one regression net) → ready to commit
3. Polish (binding-gap fixes, regression test, doc updates, version pin) → full quickstart
   validation → ready to commit

---

## Notes

- [P] tasks touch different files with no dependency on an incomplete task
- [Story] label maps each task to its spec.md user story for traceability
- This feature was implemented and verified before this tasks.md was written (spec.md
  Assumptions) - task order above reflects actual dependency structure, not a literal
  chronological log; the Implementation Strategy section's "as actually shipped" subsection notes
  where the real order diverged from priority order (US3 shipped before US1/US2, since it has no
  dependency on them)
- Every phase's regression net was the same pre-existing 51 unit + 71 (then 72) integration test
  suite, run to green after each phase before moving to the next
