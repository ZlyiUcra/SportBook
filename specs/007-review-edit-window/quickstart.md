# Quickstart: Review edit window and minimum edit comment length

Validation guide for proving the feature works end-to-end. Prerequisites, setup, and run commands
are unchanged from `specs/001-sportbook-venue-booking/quickstart.md`; no dependency install and no
database change. Seed a customer who is 006-eligible at a venue (a Confirmed, past booking on one of
its courts) and has already left a review there, so the replace path can be exercised. To exercise
the closed-window case, seed (or directly set) that review's creation time more than 24 hours in the
past.

## API validation scenarios

Run authenticated (review submission requires auth). Assume the caller is 006-eligible at the venue.

1. **Edit within window - OK**: with an existing review created less than 24h ago, POST a
   replacement with a comment of >=10 characters - accepted (200), review updated.
2. **Edit after window - rejected**: with an existing review whose creation time is more than 24h
   ago, POST a replacement - rejected `409 REVIEW_EDIT_WINDOW_CLOSED`; GET the venue's reviews and
   confirm the review's rating and comment are unchanged.
3. **Window not reset by a replace**: create a review, replace it once within the window, then set
   its creation time to just over 24h ago and replace again - the second replace is rejected
   (the window is measured from original creation, not the last edit).
4. **Edit with empty comment - rejected**: with an existing in-window review, POST a replacement
   with an empty (or missing) comment - rejected `400 REVIEW_COMMENT_TOO_SHORT`, review unchanged.
5. **Edit with short comment - rejected**: same, with a 9-character comment - rejected
   `400 REVIEW_COMMENT_TOO_SHORT`.
6. **Edit with valid comment - OK**: same, with a 10+-character comment - accepted (200).
7. **First-time submission unaffected**: as an eligible customer with NO existing review at the
   venue, POST a review with no comment at all - still accepted (201); the min-length rule does not
   apply to a first submission.
8. **List unchanged**: GET the venue's reviews - a review past its edit window still appears
   normally; the list endpoint is unaffected.

## Frontend validation scenarios (manual, via `yarn dev`)

1. **In-window edit (US1/US2)**: open "My bookings", use the review action on a completed booking
   whose review you left less than 24h ago - the edit form opens, pre-filled; a comment under 10
   characters is refused with a clear message, a 10+-character comment saves.
2. **Closed-window read-only (US1)**: for a review left more than 24h ago, the review action no
   longer offers an edit form - the review is shown read-only (still visible), and any forced submit
   is refused with the window-closed message.
3. **First-time submission still optional (US2)**: leaving a first review for a venue (no prior
   review) still accepts an empty comment - the stricter rule applies only to edits.
4. **Reasons are distinct**: a closed-window rejection and a too-short-comment rejection show
   different messages, never conflated.

## Automated tests

```powershell
# Backend (from backend/)
dotnet test

# Frontend (from frontend/)
yarn test
```

Must include: unit tests for the edit-window predicate (in-window replace allowed, post-window
replace rejected, replace never advances the window) and the edit-comment rule (empty/short rejected
on a replace, >=10 accepted, first-time no-comment accepted), each independent of eligibility;
integration tests for the gated endpoint (in-window OK, post-window 409, empty/short comment 400,
first-time no-comment 201) and the review list unchanged; frontend tests that the edit form enforces
the min length only in edit mode and that the edit action is unavailable once the window has closed.

## Non-regression

- 001-006 flows still pass (booking create/cancel/confirm, the 005 enriched rows + status filter +
  paging, the 006 eligibility gate, the relocated review entry, and the star widget). The venue
  detail page still shows its review list.
- `dotnet test` and `yarn test` are green; `yarn build` initial-chunk size is unchanged (no new
  dependency).
