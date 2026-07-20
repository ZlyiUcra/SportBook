# Tasks: Switch the backend's dispatch mechanism to MediatR

**Input**: Design documents from `/specs/010-switch-to-mediatr/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api.md, quickstart.md

**Tests**: No new test coverage needed - this is a library swap under an already-tested
architecture (spec 009); the pre-existing 51 unit + 72 integration tests are the regression net,
with 4 unit test files edited to drop a call that stopped compiling under the new library.

**Organization**: A single user story (spec.md has only one) plus Foundational and Polish phases.
All tasks below are already complete - this file documents the delivered work, the same close-out
style used for specs 006-009.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Could have run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: Which user story this task belongs to (US1)
- File paths are relative to repo root

## Path Conventions

- Backend only: `backend/src/`, `backend/tests/` - no frontend changes (spec Assumptions)

---

## Phase 1: Setup (Shared Infrastructure)

No setup tasks - this swaps a package reference inside an existing, fully-scaffolded solution;
nothing new to initialize.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The new dispatch library must be referenced and registered before any Handler file
can be converted to its API shape.

**⚠️ CRITICAL**: Blocks Phase 3.

- [x] T001 [P] Swap package references in `backend/src/SportBook.Api/SportBook.Api.csproj` and
      `backend/src/SportBook.Application/SportBook.Application.csproj`: remove
      `Mediator.Abstractions`/`Mediator.SourceGenerator` 3.0.2, add `MediatR` 14.2.0
      (research.md Decision)
- [x] T002 Replace `builder.Services.AddMediator(options => options.ServiceLifetime =
      ServiceLifetime.Scoped)` with `builder.Services.AddMediatR(cfg =>
      cfg.RegisterServicesFromAssembly(typeof(SportBook.Application.ServiceCollectionExtensions)
      .Assembly))` in `backend/src/SportBook.Api/Program.cs` - MediatR's default Transient handler
      lifetime has no captive-dependency conflict with the scoped `SportBookDbContext`, so no
      lifetime override is needed (research.md gotcha) (depends on T001)

**Checkpoint**: The new dispatch library is referenced and registered - Handler files can now be
converted to its API shape.

---

## Phase 3: User Story 1 - Use the ecosystem-standard dispatch library once the earlier objection is confirmed not to apply (Priority: P1) 🎯 MVP

**Goal**: Every command/query dispatch in the backend resolves through MediatR instead of
martinothamar/Mediator, with no change to any action's request shape, handling logic, or the
`Features/<Resource>/<UseCase>/` structure spec 009 established.

**Independent Test**: Confirm every command/query dispatch still resolves to the same handler and
produces the same result as before the swap, for every existing action, with no observable
difference from a caller's point of view.

### Implementation for User Story 1

- [x] T003 [P] [US1] Auth slices (`backend/src/SportBook.Application/Features/Auth/`):
      `using Mediator;` -> `using MediatR;` in Login/Logout/Refresh/Register;
      `LogoutHandler.Handle` changed from `async ValueTask<Unit>` (ending `return Unit.Value;`) to
      `async Task` (no return statement) - MediatR's non-generic `IRequestHandler<TCommand>` has
      no `Unit` type (depends on T002)
- [x] T004 [P] [US1] Availability slice
      (`backend/src/SportBook.Application/Features/Availability/GetAvailability/`):
      `using Mediator;` -> `using MediatR;`; `Handle` return type `async ValueTask<T>` ->
      `async Task<T>` (depends on T002)
- [x] T005 [P] [US1] Bookings slices (`backend/src/SportBook.Application/Features/Bookings/`):
      same using-directive and return-type changes across CancelBooking/ConfirmBooking/
      CreateBooking/GetBookingById/ListMyBookings/ListVenueBookingsForOwner - CreateBooking's
      serializable-transaction deadlock-retry loop untouched (depends on T002)
- [x] T006 [P] [US1] Cities slices (`backend/src/SportBook.Application/Features/Cities/`): same
      changes across FindNearestCity/SuggestCities (depends on T002)
- [x] T007 [P] [US1] Courts slices (`backend/src/SportBook.Application/Features/Courts/`): same
      changes across CreateCourt/ListCourtsByVenue/UpdateCourt; `DeleteCourtHandler.Handle`
      changed from `async ValueTask<Unit>` to `async Task`, same as T003's Logout case
      (depends on T002)
- [x] T008 [P] [US1] Reviews slices (`backend/src/SportBook.Application/Features/Reviews/`): same
      changes across CreateOrReplaceReview/ListReviewsByVenue (depends on T002)
- [x] T009 [P] [US1] Users slice
      (`backend/src/SportBook.Application/Features/Users/GetMe/`): same changes (depends on T002)
- [x] T010 [P] [US1] Venues slices (`backend/src/SportBook.Application/Features/Venues/`): same
      changes across CreateVenue/SearchNearbyVenues/SearchVenues/UpdateVenue;
      `GetVenueByIdHandler.Handle` simplified from `new(reader.GetByIdAsync(...))` (constructing a
      `ValueTask<T>` from a `Task<T>`) to returning `reader.GetByIdAsync(...)` directly, since
      `Task<T>` needs no such wrapping; `DeleteVenueHandler.Handle` changed from
      `async ValueTask<Unit>` to `async Task`, same as T003/T007's pattern (depends on T002)
- [x] T011 [US1] Wire all 8 `backend/src/SportBook.Api/Endpoints/*.cs` files:
      `using Mediator;` -> `using MediatR;` - no other change, since `IMediator`/`mediator.Send`
      have the identical shape in both libraries (depends on T003-T010)

**Checkpoint**: Every one of the 26 actions dispatches through MediatR; zero remaining references
to martinothamar/Mediator anywhere in the backend.

---

## Phase 4: Polish & Cross-Cutting Concerns

**Purpose**: Fix the one place the old library's return-type convention had leaked into test code,
and verify the swap end-to-end.

- [x] T012 [P] Drop the now-invalid `.AsTask()` call (used to bridge martinothamar's
      `ValueTask<T>` to xUnit's `Task`-based `Assert.ThrowsAsync`) from
      `backend/tests/SportBook.UnitTests/BookingOverlapCheckTests.cs`,
      `ReviewEditCommentTests.cs`, `ReviewEditWindowTests.cs`, and `ReviewEligibilityTests.cs` -
      `Task<T>` already satisfies the expected signature (research.md gotcha)
- [x] T013 Run the full quickstart.md verification: clean `dotnet build` (0 errors, only the
      pre-existing `NU1510`/`NU1903` warnings), 51 unit + 72 integration tests green, manual curl
      checklist (login, logout - the trickiest case, a void-command handler) against a live server

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: empty - nothing to initialize
- **Foundational (Phase 2)**: no dependencies - BLOCKS Phase 3
- **User Story 1 (Phase 3)**: depends on Phase 2 (needs the new package referenced and the
  dispatcher registered before any Handler can compile against its API)
- **Polish (Phase 4)**: depends on Phase 3 being complete (T012 was needed once Phase 3's
  return-type change made the old `.AsTask()` calls invalid; T013 verifies the whole feature)

### Within User Story 1

- T003-T010 (the 8 resource groups) are mutually independent - different files, all depend only
  on Phase 2, not on each other
- T011 (endpoint using-directive wiring) depends on all 8 resource groups being done first, so no
  endpoint file references a Features type mid-conversion

### Parallel Opportunities

- US1: T003-T010 (8 resource groups - different files, all depend only on Foundational)
- Polish: T012 (different files from T013, which is a verification-only task)

---

## Parallel Example: User Story 1

```bash
# All 8 resource groups have no inter-dependency once Foundational (T002) is done:
Task: "Auth slices in Features/Auth/{Login,Logout,Refresh,Register}/"
Task: "Availability slice in Features/Availability/GetAvailability/"
Task: "Bookings slices in Features/Bookings/{CancelBooking,...,ListVenueBookingsForOwner}/"
Task: "Cities slices in Features/Cities/{FindNearestCity,SuggestCities}/"
Task: "Courts slices in Features/Courts/{CreateCourt,DeleteCourt,ListCourtsByVenue,UpdateCourt}/"
Task: "Reviews slices in Features/Reviews/{CreateOrReplaceReview,ListReviewsByVenue}/"
Task: "Users slice in Features/Users/GetMe/"
Task: "Venues slices in Features/Venues/{CreateVenue,...,UpdateVenue}/"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 2: Foundational (package swap + dispatcher registration)
2. Complete Phase 3: User Story 1 (all 26 actions converted, endpoints wired)
3. **STOP and VALIDATE**: build clean, full test suite green
4. Demo: `POST /api/auth/logout` (the trickiest case - a void-command handler) still returns `204`

### Incremental Delivery (as actually shipped)

1. Foundational + User Story 1 (package swap, all 26 Handlers, all 8 endpoint files) → one
   contiguous mechanical pass, since an intermediate state (some Handlers converted, others still
   on the old library) does not compile
2. Polish (test-file `.AsTask()` cleanup, full quickstart validation) → ready to commit

---

## Notes

- [P] tasks touch different files with no dependency on an incomplete task
- [Story] label maps each task to its spec.md user story for traceability
- This feature was implemented and verified before this tasks.md was written (spec.md
  Assumptions) - task order above reflects actual dependency structure, not a literal
  chronological log
- The regression net was the same pre-existing 51 unit + 72 integration test suite spec 009 left
  in place, run to green after the swap
