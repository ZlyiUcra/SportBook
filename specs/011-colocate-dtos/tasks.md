# Tasks: Colocate single-use DTOs into their owning Features folders

**Input**: Design documents from `/specs/011-colocate-dtos/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api.md, quickstart.md

**Tests**: No new test coverage needed - this is a pure code-organization move with no wire-shape
change; the pre-existing 51 unit + 72 integration tests are the regression net.

**Organization**: A single user story (spec.md has only one), grouped by the shared source file
each DTO currently lives in (moves from the same source file are sequential; moves from different
source files may run in parallel).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Could run in parallel (different source AND destination files, no dependency on an
  incomplete task)
- **[Story]**: Which user story this task belongs to (US1)
- File paths are relative to repo root

## Path Conventions

- Backend only: `backend/src/` - no frontend or test-file changes expected (spec Assumptions)

---

## Phase 1: Setup (Shared Infrastructure)

No setup tasks - no new dependency, no registration change; this is a file/namespace move within
the existing `SportBook.Application` project.

---

## Phase 2: Foundational (Blocking Prerequisites)

No foundational tasks - each move is independent of the others at the compiler level (C# doesn't
care about file location, only namespace), so nothing blocks anything else structurally.

---

## Phase 3: User Story 1 - See an action's complete request/response shape without leaving its folder (Priority: P1) 🎯 MVP

**Goal**: Every single-use DTO (research.md's classification table) lives in its owning action's
`Features/<Resource>/<UseCase>/` file; every shared DTO stays in `Dtos/`, untouched.

**Independent Test**: Pick any action whose DTO is single-use; confirm its DTO record is defined
in the same folder as its Command/Query and Handler, with no separate file to open.

### Implementation for User Story 1

- [x] T001 [US1] Move `FreeSlot` and `AvailabilityResponse` from
      `backend/src/SportBook.Application/Dtos/BookingDtos.cs` into
      `backend/src/SportBook.Application/Features/Availability/GetAvailability/
      GetAvailability.cs` (research.md classification table)
- [x] T002 [US1] Move `CreateBookingRequest` from
      `backend/src/SportBook.Application/Dtos/BookingDtos.cs` into
      `backend/src/SportBook.Application/Features/Bookings/CreateBooking/CreateBooking.cs`;
      update the `using` in `backend/src/SportBook.Api/Endpoints/BookingsEndpoints.cs` (shares a
      source file with T001 - sequential)
- [x] T003 [US1] Move the `BookingStatusFilter` enum from
      `backend/src/SportBook.Application/Dtos/BookingDtos.cs` into
      `backend/src/SportBook.Application/Features/Bookings/ListMyBookings/ListMyBookings.cs`;
      update the `using` in `backend/src/SportBook.Application/Services/BookingHelpers.cs` and
      `backend/src/SportBook.Api/Endpoints/BookingsEndpoints.cs` (shares a source file with
      T001/T002 - sequential)
- [x] T004 [P] [US1] Move `CreateReviewRequest` from
      `backend/src/SportBook.Application/Dtos/ReviewDtos.cs` into
      `backend/src/SportBook.Application/Features/Reviews/CreateOrReplaceReview/
      CreateOrReplaceReview.cs`; update the `using` in
      `backend/src/SportBook.Api/Endpoints/ReviewsEndpoints.cs`
- [x] T005 [US1] Move `CreateVenueRequest` from
      `backend/src/SportBook.Application/Dtos/VenueDtos.cs` into
      `backend/src/SportBook.Application/Features/Venues/CreateVenue/CreateVenue.cs`; update the
      `using` in `backend/src/SportBook.Api/Endpoints/VenuesEndpoints.cs`
- [x] T006 [US1] Move `UpdateVenueRequest` from
      `backend/src/SportBook.Application/Dtos/VenueDtos.cs` into
      `backend/src/SportBook.Application/Features/Venues/UpdateVenue/UpdateVenue.cs`; update the
      `using` in `backend/src/SportBook.Api/Endpoints/VenuesEndpoints.cs` (shares a source file
      with T005 - sequential)
- [x] T007 [US1] Move `VenueSummaryResponse` from
      `backend/src/SportBook.Application/Dtos/VenueDtos.cs` into
      `backend/src/SportBook.Application/Features/Venues/SearchVenues/SearchVenues.cs` (shares a
      source file with T005/T006 - sequential)
- [x] T008 [US1] Move `NearbyVenueResponse` from
      `backend/src/SportBook.Application/Dtos/VenueDtos.cs` into
      `backend/src/SportBook.Application/Features/Venues/SearchNearbyVenues/
      SearchNearbyVenues.cs` (shares a source file with T005-T007 - sequential)
- [x] T009 [US1] Move `CreateCourtRequest` from
      `backend/src/SportBook.Application/Dtos/VenueDtos.cs` into
      `backend/src/SportBook.Application/Features/Courts/CreateCourt/CreateCourt.cs`; update the
      `using` in `backend/src/SportBook.Api/Endpoints/CourtsEndpoints.cs` (shares a source file
      with T005-T008 - sequential)
- [x] T010 [US1] Move `UpdateCourtRequest` from
      `backend/src/SportBook.Application/Dtos/VenueDtos.cs` into
      `backend/src/SportBook.Application/Features/Courts/UpdateCourt/UpdateCourt.cs`; update the
      `using` in `backend/src/SportBook.Api/Endpoints/CourtsEndpoints.cs` (shares a source file
      with T005-T009 - sequential; the last task to touch `VenueDtos.cs`, which should hold only
      `VenueDetailResponse` and `CourtResponse` once T005-T010 are all done)

**Checkpoint**: `Dtos/BookingDtos.cs` holds only `BookingResponse`; `Dtos/ReviewDtos.cs` holds
only `ReviewResponse`; `Dtos/VenueDtos.cs` holds only `VenueDetailResponse` and `CourtResponse`;
`Dtos/AuthDtos.cs` and `Dtos/CityDtos.cs` are unchanged (already shared-only).

---

## Phase 4: Polish & Cross-Cutting Concerns

- [x] T011 Run the full quickstart.md verification: clean `dotnet build` (0 errors, only the
      pre-existing `NU1510`/`NU1903` warnings), 51 unit + 72 integration tests green, the
      structural `grep` checks (7 shared DTOs remain in `Dtos/`, each moved DTO found only in its
      destination file), and the two manual curl spot-checks against a live server

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)** / **Foundational (Phase 2)**: empty - nothing to initialize or block on
- **User Story 1 (Phase 3)**: no dependency on Setup/Foundational
- **Polish (Phase 4)**: depends on Phase 3 being complete

### Within User Story 1

- T001-T003 all remove records from `Dtos/BookingDtos.cs` - sequential among themselves (same
  source file), but as a group independent of T004 and T005-T010 (different source files)
- T004 is fully independent - different source file (`ReviewDtos.cs`) and destination
- T005-T010 all remove records from `Dtos/VenueDtos.cs` - sequential among themselves (same
  source file), but as a group independent of T001-T003 and T004

### Parallel Opportunities

- The three source-file groups (BookingDtos.cs: T001-T003; ReviewDtos.cs: T004;
  VenueDtos.cs: T005-T010) can proceed in parallel with each other; within each group, tasks are
  sequential

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 3: User Story 1 (all 10 DTOs moved)
2. **STOP and VALIDATE**: build clean, full test suite green
3. Demo: open `Features/Venues/CreateVenue/CreateVenue.cs` and confirm `CreateVenueRequest`,
   `CreateVenueCommand`, and `CreateVenueHandler` are all in the same file

### Incremental Delivery

1. User Story 1 (all 10 moves) → one contiguous pass grouped by source file, since a partially-
   moved source file (some records moved, others not) still compiles fine at every step - each
   task is independently safe to stop after
2. Polish (full quickstart validation) → ready to commit

---

## Notes

- [P] tasks touch different source AND destination files with no dependency on an incomplete task
- [Story] label maps each task to its spec.md user story for traceability
- Unlike specs 006-010, this tasks.md was written before implementation - tasks start unchecked
