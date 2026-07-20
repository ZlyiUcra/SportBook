# Tasks: Review edit window and minimum edit comment length

**Input**: Design documents from `/specs/007-review-edit-window/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api.md, quickstart.md

**Tests**: Included alongside each story (same stance as 002/003/005/006). The contract MUSTs (both
rules server-side, replace-branch-only scope, distinct rejection codes, first-time submission
unaffected) are each backed by a named task below.

**Organization**: Tasks are grouped by user story (from spec.md) to enable independent
implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1-US2)
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

No foundational tasks. Both rules are independent guard clauses on the existing replace branch of
`ReviewService.CreateOrReplaceAsync` - neither depends on the other's logic (US2 does not need the
window check to exist, and vice versa). They land in the same method, so their implementation tasks
run sequentially to avoid clobbering each other's edit, but nothing blocks either story from being
implemented and tested on its own.

---

## Phase 3: User Story 1 - A review is only editable for 24 hours after it was first left (Priority: P1) 🎯 MVP

**Goal**: Replacing an existing venue review is only accepted within 24 hours of that review's
original creation time; past that, the replace is rejected while the review keeps displaying to
everyone, and a replace never resets the window.

**Independent Test**: Replace a review created less than 24h ago - accepted; seed a review created
more than 24h ago and attempt to replace it - rejected `409 REVIEW_EDIT_WINDOW_CLOSED`, content
unchanged.

### Tests for User Story 1

- [x] T001 [P] [US1] Unit test: replacing a review is allowed when `now <= CreatedAt + 24h` and
      rejected when `now > CreatedAt + 24h`; replacing once inside the window then again still
      inside 24h of the *original* creation is allowed (the window is never advanced by a prior
      replace); a first-time submission (no existing review) is never subject to this check, in
      `backend/tests/SportBook.UnitTests/ReviewEditWindowTests.cs`
- [x] T002 [P] [US1] Integration test: `POST /api/venues/{venueId}/reviews` replace succeeds within
      24h of the existing review's creation, is rejected `409 REVIEW_EDIT_WINDOW_CLOSED` when the
      existing review's `CreatedAt` is seeded more than 24h in the past (stored rating/comment
      unchanged after the rejection), and `GET /api/venues/{venueId}/reviews` still lists a
      past-window review normally, in
      `backend/tests/SportBook.IntegrationTests/ReviewEditWindowEndpointTests.cs`

### Implementation for User Story 1

- [x] T003 [US1] In `backend/src/SportBook.Application/Services/ReviewService.cs`
      `CreateOrReplaceAsync`, on the replace branch (an `existing` review was found), before
      mutating it, reject when `now > existing.CreatedAt.AddHours(24)` with
      `throw new ApiException(409, "REVIEW_EDIT_WINDOW_CLOSED", ...)`, reusing the `now` already
      computed from the injected `TimeProvider`; a first-time submission (no `existing`) is
      unaffected (per data-model.md, research.md)
- [x] T004 [US1] Frontend: in `frontend/src/pages/my-bookings/ui/MyBookingsPage.tsx`'s
      `ReviewAction`, compute whether the caller's existing review (`mine`, from `reviewsQuery`) is
      still inside its 24h window from `mine.createdAt`; when open, keep today's edit form; when
      closed, render the review read-only (rating + comment, no `ReviewForm`) inside the dialog
      instead of the edit form - the review stays visible, only editing is withdrawn (depends on
      T003 existing server-side, though the frontend check is independently testable)
- [x] T005 [US1] Frontend test: with an existing review whose `createdAt` is more than 24h in the
      past, the review action shows the review read-only with no edit form; with one less than 24h
      old, the edit form still appears, in `frontend/tests/pages/MyBookings.test.tsx` (depends on
      T004)

**Checkpoint**: US1 functional and independently testable - a review can no longer be changed once
its 24-hour window has passed, and it still displays to everyone.

---

## Phase 4: User Story 2 - An edit within the window must carry a real comment (Priority: P2)

**Goal**: Replacing an already-submitted review (inside the edit window) requires a non-empty
comment of at least 10 characters; a first-time submission's comment stays optional.

**Independent Test**: Replace an existing review with an empty comment (rejected), with a
9-character comment (rejected), and with a 10-character comment (accepted); submit a first-time
review with no comment at all (still accepted).

### Tests for User Story 2

- [ ] T006 [P] [US2] Unit test: replacing a review with a missing/empty/whitespace-only comment or
      one under 10 characters (after trim) is rejected; a comment of exactly 10 characters or more
      is accepted; a first-time submission (no existing review) with no comment is accepted
      regardless of length, in `backend/tests/SportBook.UnitTests/ReviewEditCommentTests.cs`
- [ ] T007 [P] [US2] Integration test: `POST /api/venues/{venueId}/reviews` replace with an
      empty/short comment is rejected `400 REVIEW_COMMENT_TOO_SHORT` (stored review unchanged), a
      10+-character comment on a replace succeeds, and a first-time submission with no comment still
      returns `201`, in `backend/tests/SportBook.IntegrationTests/ReviewEditCommentEndpointTests.cs`

### Implementation for User Story 2

- [ ] T008 [US2] In `backend/src/SportBook.Application/Services/ReviewService.cs`
      `CreateOrReplaceAsync`, on the replace branch, after the T003 edit-window check passes,
      reject when `request.Comment` is null, empty, or (after `.Trim()`) shorter than 10 characters,
      with `throw new ApiException(400, "REVIEW_COMMENT_TOO_SHORT", ...)`; the create branch
      (first-time submission) is unaffected (per data-model.md, research.md; depends on T003 for the
      check ordering)
- [ ] T009 [US2] Frontend: add an `isEdit: boolean` prop to `ReviewForm` in
      `frontend/src/features/review/create/ui/ReviewForm.tsx`; when true, validate `comment` as
      required and at least 10 characters (extend/parameterize `reviewFormSchema` in
      `frontend/src/features/review/create/model/schema.ts` accordingly), otherwise keep it
      optional as today; pass `isEdit={!!mine}` from `ReviewAction` in
      `frontend/src/pages/my-bookings/ui/MyBookingsPage.tsx` (depends on T004's read-only branch
      existing - the form only renders at all when the window is open)
- [ ] T010 [US2] Frontend test: with `isEdit`, submitting an empty or 9-character comment is blocked
      client-side with a validation message; a 10+-character comment submits; without `isEdit`
      (first-time), submitting with no comment still succeeds, in
      `frontend/tests/features/ReviewForm.test.tsx` (depends on T009)

**Checkpoint**: US2 functional - an edit cannot blank out or gut its comment, while a first review
stays as low-friction as before.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: i18n, README, and end-to-end validation

- [ ] T011 [P] i18n: add rejection/read-only-view copy for `REVIEW_EDIT_WINDOW_CLOSED` context
      (read-only label) and the edit-mode comment requirement in
      `frontend/src/shared/i18n/locales/{en,uk,pt}.json`
- [ ] T012 [P] Update root `README.md`: a review stays editable by its author for 24 hours after
      creation and, when edited, requires a real (10+ character) comment; add the 007 spec to
      "Further reading"
- [ ] T013 Run quickstart.md validation scenarios end-to-end against a locally running stack, plus
      non-regression: full `dotnet test`, `yarn test`, and a `yarn build` initial-chunk comparison
      (must be unchanged - no new dependency)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: empty
- **Foundational (Phase 2)**: empty - both stories are logically independent
- **User Story 1 (Phase 3)**: self-contained; T003 (backend) and T004 (frontend) touch different
  files and could proceed in parallel, but T004 is easiest to write against T003's error code
- **User Story 2 (Phase 4)**: T008 lands after T003 in `ReviewService.cs` (same method, ordered
  guard clauses per contracts/api.md); T009's form only matters once T004 exists (the edit form is
  only reachable when the window is open)
- **Polish (Phase 5)**: depends on both stories being complete

### Within Each User Story

- US1: tests (T001, T002) alongside the implementation (T003 backend, T004 frontend); T005 after T004
- US2: tests (T006, T007) alongside the implementation (T008 backend, T009 frontend); T010 after T009

### Parallel Opportunities

- US1: T001 and T002 (different test files) can run in parallel
- US2: T006 and T007 (different test files) can run in parallel
- Polish: T011 and T012 can run in parallel

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 3: User Story 1 (the edit window)
2. **STOP and VALIDATE**: quickstart.md API scenarios 1-3 (in-window OK, post-window rejected,
   window never resets)
3. Demo: a review can no longer be silently blanked out or changed after a day

### Incremental Delivery

1. User Story 1 -> validate -> demo (the edit window, the core fix)
2. User Story 2 -> validate -> demo (a real comment required on edits)
3. Polish -> i18n, README, full quickstart + non-regression

---

## Notes

- No schema, no migration, no new dependency; both rules read/validate data already present
  (`Review.CreatedAt`, the request `comment`)
- The window is measured from the review's original `CreatedAt` and is never reset by a replace -
  guaranteed because the replace branch never writes `CreatedAt` (001/006 invariant, unchanged here)
- Both rules apply ONLY on the replace branch - a first-time submission is untouched by either rule
- The 006 eligibility gate (a completed, confirmed booking) is unchanged and checked independently,
  before both new rules
- The window check (409) runs before the comment check (400) on a replace, per contracts/api.md, so
  the two rejection reasons are never conflated
- Commit after each verified functional slice (build + run + check), per the user-stated
  atomic-commit preference - not mechanically per task or per phase
