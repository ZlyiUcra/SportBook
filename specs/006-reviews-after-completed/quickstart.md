# Quickstart: Reviews only after a completed, confirmed game

Validation guide for proving the feature works end-to-end. Prerequisites, setup, and run commands
are unchanged from `specs/001-sportbook-venue-booking/quickstart.md`; no dependency install and no
database change. Seed a customer with bookings across statuses at one venue: at least one Confirmed
booking whose end time is in the past (a completed game), plus a Pending, a future, and a cancelled
booking; and a second venue where the customer has no completed booking.

## API validation scenarios

Run authenticated (review submission requires auth).

1. **Eligible - create**: POST a review (rating + comment) for the venue where the customer has a
   Confirmed-and-past booking - accepted (201).
2. **Eligible - replace**: POST a second review for that same venue - the existing review is
   replaced, not duplicated (200); still one review for that customer + venue.
3. **Ineligible - no completed game**: POST a review for the second venue (no Confirmed-past
   booking there) - rejected `409 REVIEW_NOT_ELIGIBLE`, no review written.
4. **Ineligible - only non-qualifying bookings**: with a customer whose only bookings at a venue are
   Pending, future, or cancelled, POST a review - rejected `409 REVIEW_NOT_ELIGIBLE`.
5. **Rating still validated**: as an eligible customer, POST a review with rating 0 or 6 - rejected
   `400 INVALID_RATING` (the gate does not replace rating validation).
6. **List unchanged**: GET the venue's reviews - returns the reviews as before, to any viewer,
   including any legacy reviews; the list endpoint is unaffected by the gate.

## Frontend validation scenarios (manual, via `yarn dev`)

1. **Venue page has no form (US2/FR-003)**: open a venue detail page - there is no review submission
   form. The reviews others left are still displayed (FR-010).
2. **Review from completed booking (US2)**: open "My bookings"; a completed (Confirmed, past)
   booking offers a review action. Follow it - it opens the review entry for that booking's venue,
   pre-filled with the existing review if one was left.
3. **No action on non-completed rows (FR-005)**: upcoming, pending, and cancelled bookings offer no
   review action.
4. **Gate feedback**: attempting to submit when ineligible (e.g. a legacy author who no longer
   qualifies) surfaces the single clear reason, not a generic error.
5. **Star widget - hover (US3)**: at the review entry, hover the third star - three stars preview;
   hover the fifth - five preview.
6. **Star widget - click proportional (US3)**: click near the right edge - rating 5; click at the
   fourth star - rating 4; leftmost - rating 1.
7. **Star widget - keyboard/touch (FR-009)**: focus the control and set any rating 1-5 by keyboard;
   on a touch device, tap a star to set the rating.
8. **Optional, never nagged (FR-006)**: skipping the review action anywhere leaves no blocking modal
   or repeat prompt.

## Automated tests

```powershell
# Backend (from backend/)
dotnet test

# Frontend (from frontend/)
yarn test
```

Must include: unit tests for the eligibility predicate (accept Confirmed-and-past on a court of the
venue; reject Pending / future / cancelled / none / a booking on another venue's court) and that the
rating validation fires independently of eligibility; integration tests for the gated endpoint
(201/200 eligible create-then-replace, 409 ineligible, 400 bad rating) and that the review list
endpoint is unchanged; frontend tests that the venue page renders no submission form but still the
list, that the review action shows only on completed bookings, and the star widget (hover preview,
click-to-value, keyboard).

## Non-regression

- 001-005 flows still pass (create/cancel/confirm bookings, the 005 enriched rows + status filter +
  paging, venue search and map). The venue detail page still shows its review list.
- `dotnet test` and `yarn test` are green; `yarn build` initial-chunk size is unchanged (no new
  dependency).
