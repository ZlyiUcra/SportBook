# Tasks: Reviews only after a completed, confirmed game

**Input**: Design documents from `/specs/006-reviews-after-completed/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api.md, quickstart.md

**Tests**: Included alongside each story (same stance as 002/003/005). The contract MUSTs
(server-side eligibility gate, rating validation stays independent, review list unchanged, review
stays optional) are each backed by a named task below.

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

No setup tasks - no dependencies, no configuration, no schema, no migration. Existing tooling covers
everything.

---

## Phase 2: Foundational (Blocking Prerequisites)

No foundational tasks. Each user story is independent: US1 is the backend gate (self-contained in
`ReviewService`), US2 relocates the entry (its one backend need - `venueId` on the booking response -
lives inside US2), and US3 is a self-contained widget swap. Nothing blocks across stories.

---

## Phase 3: User Story 1 - Only customers who played can review (Priority: P1) 🎯 MVP

**Goal**: The API accepts a venue review only from a customer with a Confirmed, past booking on one
of the venue's courts; every other case is rejected with one clear reason. Rating and venue-exists
validation are unchanged.

**Independent Test**: As a customer with a completed booking at a venue, POST a review - accepted;
as a customer with no completed booking there, POST - rejected `409 REVIEW_NOT_ELIGIBLE`, nothing
written.

### Tests for User Story 1

- [x] T001 [P] [US1] Unit test: the eligibility predicate accepts a `Confirmed` booking with
      `EndTime <= now` on a court of the target venue and rejects each of Pending, future
      (`EndTime > now`), Cancelled, no booking, and a Confirmed-past booking on a DIFFERENT venue's
      court; and a bad rating is still rejected with `INVALID_RATING` regardless of eligibility, in
      `backend/tests/SportBook.UnitTests/ReviewEligibilityTests.cs`
- [x] T002 [P] [US1] Integration test: `POST /api/venues/{venueId}/reviews` returns 201 then 200 for
      an eligible customer (create then replace, still one review), `409 REVIEW_NOT_ELIGIBLE` for an
      ineligible one (no review written), `400 INVALID_RATING` for a bad rating, and
      `GET /api/venues/{venueId}/reviews` (the list) is unaffected, in
      `backend/tests/SportBook.IntegrationTests/ReviewEligibilityEndpointTests.cs`

### Implementation for User Story 1

- [x] T003 [US1] In `backend/src/SportBook.Application/Services/ReviewService.cs`
      `CreateOrReplaceAsync`, after the existing rating check (400) and venue-exists check (404) and
      before the create-or-replace, add the eligibility gate: `if (!await db.Bookings.AnyAsync(b =>
      b.UserId == userId && b.Court!.VenueId == venueId && b.Status == BookingStatus.Confirmed &&
      b.EndTime <= now, ct)) throw new ApiException(409, "REVIEW_NOT_ELIGIBLE", ...)`, reusing the
      `now` already computed from the injected `TimeProvider`; keep the one-per-user-per-venue
      create-or-replace semantics and the existing validations unchanged (per data-model.md,
      research.md)

**Checkpoint**: US1 functional and independently testable via the API - a review is accepted only
after a completed, confirmed game.

---

## Phase 4: User Story 2 - Reach the review from your completed bookings (Priority: P2)

**Goal**: The review submission form is gone from the venue detail page (the review LIST stays);
each completed (Confirmed, past) booking on "My bookings" offers a review action leading to the
review entry for that booking's venue, pre-filled with the customer's existing review if any.

**Independent Test**: Open a venue page - no submission form, list still present; open "My
bookings" - a completed booking offers a review action that opens the review entry for that venue;
non-completed bookings offer none.

### Implementation for User Story 2

- [x] T004 [US2] Backend: add `Guid VenueId` to `BookingResponse` in
      `backend/src/SportBook.Application/Dtos/BookingDtos.cs` and populate it in
      `Mapping.ToResponse(this Booking, ...)` in `backend/src/SportBook.Application/Dtos/Mapping.cs`
      from the already-loaded `booking.Court!.VenueId` (no new Include, no other field added), per
      contracts/api.md and data-model.md
- [x] T005 [P] [US2] Integration test: a booking response carries `venueId` equal to its court's
      venue, alongside the 005 labels, in
      `backend/tests/SportBook.IntegrationTests/BookingDetailTests.cs`
- [x] T006 [P] [US2] Frontend: add `venueId: string` to the `Booking` type in
      `frontend/src/entities/booking/model/types.ts` (depends on the contract; parallel to backend)
- [x] T007 [US2] Frontend: remove the review submission `Card`/`ReviewForm` block from
      `frontend/src/pages/venue-detail/ui/VenueDetailPage.tsx` (and its now-unused `createReview`
      mutation, `ReviewForm` import, and current-user prefill lookup); KEEP the review list display
      and its `listReviews` query untouched (spec FR-003, FR-010)
- [x] T008 [US2] Frontend: on `frontend/src/pages/my-bookings/ui/MyBookingsPage.tsx`, show a review
      action only on rows whose status is `Completed`; opening it reveals the review entry (in-place
      panel or dialog) hosting `ReviewForm`, submitting via `createReview(booking.venueId, values)`,
      pre-filled with the caller's existing review for that venue (reuse `listReviews(venueId)`,
      find the row with the current user's id) and surfacing the `REVIEW_NOT_ELIGIBLE`/other API
      message via `ApiRequestError`; the action is absent on Pending/Confirmed-upcoming/Cancelled
      rows (spec FR-004, FR-005). Add i18n keys (review-action label, entry title) in
      `frontend/src/shared/i18n/locales/{en,uk,pt}.json` (depends on T006, T007)
- [x] T009 [US2] Frontend test: the venue page renders the review list but NO submission form; on
      "My bookings" the review action shows only on a Completed row and submitting posts to that
      booking's venue, in `frontend/tests/pages/VenueDetail.test.tsx` and
      `frontend/tests/pages/MyBookings.test.tsx` (depends on T007, T008)

**Checkpoint**: US2 functional - the review is reached from a completed booking, never from the
venue page, which still shows the list.

---

## Phase 5: User Story 3 - Pick a rating with stars, not a dropdown (Priority: P3)

**Goal**: The rating input is an interactive five-star control - hover previews the value, clicking
picks 1-5 by position in the row, and it works by keyboard and touch.

**Independent Test**: Open the review entry - hovering a star previews that many stars; clicking
sets 1-5 by position; the rating is fully settable by keyboard and on touch.

### Implementation for User Story 3

- [ ] T010 [US3] Frontend: build a self-contained `StarRating` component (no new dependency) in
      `frontend/src/features/review/create/ui/StarRating.tsx` - a controlled `value`/`onChange`
      integer 1-5, five focusable stars where pointer hover previews the value, a click sets 1-5
      proportional to the x-position within the row (five equal segments, clamped), and keyboard
      (arrow/number keys) and touch (tap) set the full 1-5 range, with aria labels for current and
      previewed rating; i18n aria keys in `frontend/src/shared/i18n/locales/{en,uk,pt}.json`
- [ ] T011 [US3] Frontend: replace the `<select>` rating in
      `frontend/src/features/review/create/ui/ReviewForm.tsx` with `StarRating`, wired to the RHF
      `rating` field (via `Controller` or `setValue`, keeping the same 1-5 zod validation and
      submit shape); remove the `RATINGS`/`<select>` markup (depends on T010)
- [ ] T012 [US3] Frontend test: hovering the third star previews three, clicking near the right edge
      yields 5 and the fourth star yields 4, and the rating is settable by keyboard, in
      `frontend/tests/features/StarRating.test.tsx` (depends on T011)

**Checkpoint**: All three user stories independently functional - full feature deliverable.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Contract audit and end-to-end validation

- [ ] T013 [P] Response-DTO audit for `BookingResponse` - confirm `venueId` is the only field added
      this feature and no other internal data leaks (contract MUST, data-model.md)
- [ ] T014 [P] Update root `README.md`: reviews are now gated on a completed booking and submitted
      from "My bookings" (not the venue page, which keeps the review list) with a five-star rating;
      add the 006 spec to "Further reading"
- [ ] T015 Run quickstart.md validation scenarios end-to-end against a locally running stack, plus
      non-regression: full `dotnet test`, `yarn test`, and a `yarn build` initial-chunk comparison
      (must be unchanged - no new dependency)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: empty
- **Foundational (Phase 2)**: empty - stories are independent
- **User Story 1 (Phase 3)**: self-contained backend gate; no dependency on US2/US3
- **User Story 2 (Phase 4)**: self-contained; its backend `venueId` (T004) and frontend relocation
  are internal to the story. Independent of US1 (though US1's gate makes the relocated entry
  meaningful) and of US3 (hosts the existing `ReviewForm`, which US3 later re-skins)
- **User Story 3 (Phase 5)**: self-contained widget swap inside `ReviewForm`; independent of US1/US2
- **Polish (Phase 6)**: depends on all desired user stories being complete

### Within Each User Story

- US1: tests (T001, T002) with the implementation (T003); the predicate before the endpoint verify
- US2: backend `venueId` (T004) before the frontend that consumes it (T006, T008); venue-page
  removal (T007) before/with the My-bookings entry (T008)
- US3: the `StarRating` component (T010) before it replaces the `<select>` (T011) before its test

### Parallel Opportunities

- US1: T001 and T002 (different test files) can run in parallel; T003 is the single implementation
- US2: T004 (backend) and T006 (frontend type) can run in parallel; T005 parallel to frontend work
- Polish: T013 and T014 can run in parallel

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 3: User Story 1 (the eligibility gate)
2. **STOP and VALIDATE**: quickstart.md API scenarios 1-5 (eligible create/replace, ineligible 409,
   rating still 400)
3. Demo: a review is accepted only after a completed, confirmed game - the trust guarantee

### Incremental Delivery

1. User Story 1 -> validate -> demo (gated reviews, the core value)
2. User Story 2 -> validate -> demo (review reached from completed bookings; venue page keeps list)
3. User Story 3 -> validate -> demo (five-star rating)
4. Polish -> DTO audit, README, full quickstart + non-regression

---

## Notes

- No schema, no migration, no new dependency; eligibility reads existing Booking/Court navigations
  and the injected `TimeProvider`, and the star widget is built from existing primitives
- "Completed" stays the derived read-time view (001/005 invariant): eligibility is
  `Status == Confirmed && EndTime <= now`, never a stored status
- The gate is server-side (spec SC-002) - the client is never trusted to certify a completed game
- The venue detail page keeps rendering the review LIST (social proof); only the submission form
  moves (user's explicit correction)
- Reviews stay optional - no forced modal, no repeat prompt (spec FR-006)
- The "played but never confirmed" case is out of scope, deferred to a later feature
- `venueId` is the only response field added; it is a navigation id (like the already-exposed
  `courtId`), not display data
- Commit after each verified functional slice (build + run + check), per the user-stated
  atomic-commit preference - not mechanically per task or per phase
