# Tasks: My bookings - venue detail, status filter, and pagination

**Input**: Design documents from `/specs/005-my-bookings-detail/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api.md, quickstart.md

**Tests**: Included alongside each story (same stance as 002/003). The contract MUSTs (translatable
server-side filter applied before paging, no raw-id leakage, no owner-id leakage, derived-Completed
stays unstored) are each backed by a named task below.

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

No setup tasks - no dependencies, no configuration, no schema. Existing tooling covers everything.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The widened booking response and its Include chain - every user story and both
booking lists depend on the response carrying venue/city/sport/court. No user story work can begin
until this phase is complete.

- [x] T001 Widen `BookingResponse` in `backend/src/SportBook.Application/Dtos/BookingDtos.cs`: add
      `string VenueName`, `CityResponse City`, `string Sport`, `string CourtName` after the existing
      001 fields, per data-model.md (no owner id, no other internal field)
- [x] T002 Update `Mapping.ToResponse(this Booking, DateTime utcNow)` in
      `backend/src/SportBook.Application/Dtos/Mapping.cs` to read the loaded chain
      (`booking.Court!.Venue!.Name`, `booking.Court.Venue.City!.ToResponse()`,
      `booking.Court.SportType.ToString()`, `booking.Court.Name`); document that callers MUST load
      `Court -> Venue -> City` before mapping (depends on T001)
- [x] T003 Add the `Include(b => b.Court).ThenInclude(c => c.Venue).ThenInclude(v => v.City)` chain
      to every booking path in `backend/src/SportBook.Application/Services/BookingService.cs` that
      maps to a response: `ListMineAsync`, `ListByVenueForOwnerAsync`, `GetByIdAsync`, `CancelAsync`;
      in `CreateAsync` load the court with `Include(c => c.Venue).ThenInclude(v => v.City)` and set
      `booking.Court = court` in memory so the new booking maps fully without a second round-trip
      (depends on T002)

**Checkpoint**: Every booking endpoint returns the enriched shape; both lists can render detail.

---

## Phase 3: User Story 1 - See what each booking is for (Priority: P1) 🎯 MVP

**Goal**: Every booking row shows venue name, city, sport, and court name on both the customer and
owner lists.

**Independent Test**: Make a booking; verify "My bookings" and "Venue bookings" rows show the
venue, city, sport, and court name alongside time/status/price, with no raw court id shown.

### Tests for User Story 1

- [x] T004 [P] [US1] Integration test: `GET /api/bookings` and `GET /api/venues/{id}/bookings`
      items carry `venueName`, `city`, `sport`, `courtName` (and still the 001 fields), and expose
      no owner id, in `backend/tests/SportBook.IntegrationTests/BookingDetailTests.cs`

### Implementation for User Story 1

- [x] T005 [P] [US1] Frontend: extend the `Booking` type in
      `frontend/src/entities/booking/model/types.ts` with `venueName: string`, `city: City`,
      `sport: SportType`, `courtName: string` (depends on nothing beyond the contract)
- [x] T006 [US1] Frontend: render the venue name, city (localized via `cityName`), sport, and court
      name on each row in `frontend/src/pages/my-bookings/ui/MyBookingsPage.tsx` and
      `frontend/src/pages/owner-bookings/ui/OwnerBookingsPage.tsx` (shared shape), with any new
      i18n keys in `frontend/src/shared/i18n/locales/{en,uk,pt}.json` (depends on T005)
- [x] T007 [US1] Frontend test: a booking row shows venue/city/sport/court alongside time, status,
      and price (map/detail per research.md), in `frontend/tests/pages/MyBookings.test.tsx`
      (depends on T006)

**Checkpoint**: US1 functional - a booking is legible without opening anything else.

---

## Phase 4: User Story 2 - Filter bookings by status (Priority: P2)

**Goal**: The customer filters their bookings by All / Upcoming / Completed / Cancelled,
server-side, across their whole history.

**Independent Test**: With bookings across statuses, select each filter and verify only matching
bookings appear (including on later pages).

### Tests for User Story 2

- [x] T008 [P] [US2] Unit test: the `BookingStatusFilter` predicate maps each choice to the right
      stored-status/time combination over materialized rows (Sqlite path) - Upcoming excludes
      cancelled and past, Completed is confirmed-and-past only, Cancelled is cancelled only, a
      stale pending-past booking matches only All - in
      `backend/tests/SportBook.UnitTests/BookingStatusFilterTests.cs`
- [x] T009 [P] [US2] Unit test: `ToQueryString()` proves the status predicate plus the
      court->venue->city Include translate to SQL (no client evaluation) in
      `backend/tests/SportBook.UnitTests/BookingStatusFilterTests.cs`

### Implementation for User Story 2

- [x] T010 [US2] Add a `BookingStatusFilter` enum (All/Upcoming/Completed/Cancelled) in
      `backend/src/SportBook.Application/` and apply it as a translatable predicate BEFORE
      `Skip`/`Take` in `BookingService.ListMineAsync` per data-model.md (Upcoming =
      `Status != Cancelled && EndTime > now`; Completed = `Status == Confirmed && EndTime <= now`;
      Cancelled = `Status == Cancelled`; All = no predicate) (depends on T003)
- [x] T011 [US2] Add the optional `status` query parameter (default All) to `GET /api/bookings` in
      `backend/src/SportBook.Api/Controllers/BookingsController.cs`, bound like the existing
      `SportType?` pattern; do NOT add it to the owner venue-bookings endpoint (depends on T010)
- [x] T012 [P] [US2] Integration test: `status=Upcoming|Completed|Cancelled` each return only their
      group and the filter holds across pages (page 2 of a filter still matches), default is All, in
      `backend/tests/SportBook.IntegrationTests/BookingStatusFilterEndpointTests.cs`
- [x] T013 [US2] Frontend: add the optional `status` arg to `listMyBookings` in
      `frontend/src/entities/booking/api/bookingApi.ts` and filter tabs (All/Upcoming/Completed/
      Cancelled, default All) on `frontend/src/pages/my-bookings/ui/MyBookingsPage.tsx`, with a
      "no bookings in this filter" empty state distinct from "no bookings at all"; i18n keys in
      `frontend/src/shared/i18n/locales/{en,uk,pt}.json` (depends on T011, T006)
- [x] T014 [US2] Frontend test: selecting each filter requests that `status` and shows the matching
      empty state when none match, in `frontend/tests/pages/MyBookings.test.tsx` (depends on T013)

**Checkpoint**: US2 functional - the customer narrows their bookings by status across all pages.

---

## Phase 5: User Story 3 - Page through a long booking history (Priority: P3)

**Goal**: Previous/Next controls move through the paged history; changing the filter resets to
page 1.

**Independent Test**: With more bookings than one page, Prev/Next move between pages, disable at the
ends, and a filter change returns to page 1.

### Implementation for User Story 3

- [x] T015 [US3] Frontend: Prev/Next pagination on
      `frontend/src/pages/my-bookings/ui/MyBookingsPage.tsx` driven by the `PagedResponse`
      (page/pageSize/totalCount), passing `page` to `listMyBookings`; Previous disabled on page 1,
      Next on the last page; changing the status filter resets `page` to 1 (spec FR-008) (depends
      on T013)
- [x] T016 [US3] Frontend test: Prev/Next move pages and disable at the ends, and changing the
      filter resets to page 1, in `frontend/tests/pages/MyBookings.test.tsx` (depends on T015)

**Checkpoint**: All three user stories independently functional - full feature deliverable.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Contract audits and end-to-end validation

- [x] T017 [P] Response-DTO audit for `BookingResponse` - confirm only venue/city/sport/court
      labels were added and no owner id or other internal field leaks (contract MUST, spec FR-011)
- [x] T018 [P] Update root `README.md` "Using the application" (My bookings now shows venue/sport
      detail, status filter, and paging) and add the 005 spec to "Further reading"
- [x] T019 Run all quickstart.md validation scenarios end-to-end against a locally running stack,
      plus non-regression: full `dotnet test`, `yarn test`, and a `yarn build` initial-chunk
      comparison (must be unchanged - no new dependency)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: empty
- **Foundational (Phase 2)**: T001 -> T002 -> T003; BLOCKS all user stories
- **User Story 1 (Phase 3)**: depends on Foundational (the enriched shape)
- **User Story 2 (Phase 4)**: depends on Foundational (T003) and US1's row work (T006 for the tabs'
  host page); the filter itself only needs T003
- **User Story 3 (Phase 5)**: depends on US2's page wiring (T013)
- **Polish (Phase 6)**: depends on all desired user stories being complete

### Within Each User Story

- Tests before/with implementation per story; implementation before its frontend test
- Backend predicate/param before the frontend that consumes it

### Parallel Opportunities

- T004 and T005 (different files) can run in parallel early in US1
- T008 and T009 (unit tests) and T012 (integration) can run in parallel in US2
- T017 and T018 can run in parallel in Polish

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 2: Foundational (widened response + Include chain)
2. Complete Phase 3: User Story 1 (enriched rows on both lists)
3. **STOP and VALIDATE**: quickstart.md API scenario 1 + frontend scenario 1
4. Demo: a booking now shows its venue, city, sport, and court

### Incremental Delivery

1. Foundational -> enriched booking shape everywhere
2. User Story 1 -> validate -> demo (legible bookings)
3. User Story 2 -> validate -> demo (status filter across pages)
4. User Story 3 -> validate -> demo (paging + reset)
5. Polish -> DTO audit, README, full quickstart + non-regression

---

## Notes

- No schema, no migration, no new dependency; the enriched fields reuse existing navigations and
  `CityResponse`
- "Completed" stays a derived read-time view (001 invariant) - the filter encodes it as
  Confirmed + past end time, never a stored status
- The status filter is server-side and applied before Skip/Take so it composes with paging across
  the whole history (spec FR-006) - never filter a materialized page on the client
- The owner "Venue bookings" list gains the detail (shared DTO) but NOT the status filter this
  feature (research.md scope decision)
- Commit after each verified functional slice (build + run + check), per user-stated atomic-commit
  preference - not mechanically per task or per phase
