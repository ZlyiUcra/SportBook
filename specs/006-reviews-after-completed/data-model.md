# Phase 1 Data Model: Reviews only after a completed, confirmed game

No persistent schema change (no table, column, or migration). This feature adds a write-time
eligibility rule over entities that already exist and relocates a form; the Review shape and the
booking navigations are reused unchanged.

## Review (unchanged shape, new create/replace rule)

No field change. One review per customer per venue (the existing unique index on
`(VenueId, UserId)`), rating 1-5 plus an optional comment, create-or-replace semantics (201 create /
200 replace). What changes is only who may create or replace one - see the eligibility rule below.
Reviews created before this feature are kept as-is (not purged); their authors must now meet the
eligibility rule to replace them.

## BookingResponse (one additive field)

The transport DTO gains `VenueId` (Guid) beside the 005 labels; no other change. It lets a
completed booking on "My bookings" target its venue's review endpoint (keyed by venue id), and is
exposed for the same reason `CourtId` already is - actions/links, not display. No stored-schema
change - `VenueId` is read from the already-loaded `Court.VenueId`.

## Booking / Court / Venue (unchanged, reused for eligibility)

No change. The eligibility rule reads the existing navigation chain:

- `Booking.Court` -> the booked court; `Court.VenueId` -> the court's venue.
- `Booking.Status` (BookingStatus: Pending / Confirmed / Cancelled) and `Booking.EndTime`.

A booking on a court of the venue whose `Status == Confirmed && EndTime <= now` is the proof the
customer played there. Completed and cancelled bookings are never deleted (001), so eligibility, once
earned, persists (spec FR-011).

## Review eligibility rule (new, request-time - not stored)

Evaluated in `ReviewService.CreateOrReplaceAsync` before create-or-replace, using the injected
`TimeProvider` for `now`:

| Actor state | Predicate over existing bookings | Outcome |
|---|---|---|
| Has a completed game at the venue | EXISTS booking: `UserId == userId && Court.VenueId == venueId && Status == Confirmed && EndTime <= now` | eligible - create or replace proceeds |
| No such booking (browsing, only Pending, only future, only Cancelled, other venue, or none) | the EXISTS is false | rejected - `REVIEW_NOT_ELIGIBLE` (409) |

- Same "Completed" definition as feature 005 (`Status == Confirmed && EndTime <= now`); never a
  stored status. Reused verbatim so the two features cannot diverge.
- A stale Pending-past booking does NOT confer eligibility (Status is not Confirmed).
- The rule is checked on every submit, so a replace re-checks eligibility (a legacy author who no
  longer qualifies cannot edit).
- The existing rating validation (1-5) and venue-exists check are independent and still run - a bad
  rating is rejected on its own regardless of eligibility.

## Consumers

- Backend: `ReviewService.CreateOrReplaceAsync` gains the predicate and the `REVIEW_NOT_ELIGIBLE`
  rejection; `ListByVenueAsync` (the review list) is unchanged.
- Customer "My bookings" list: each completed (Confirmed, past) row - the same derived status 005
  already computes - offers the review action; non-completed rows do not.
- Venue detail page: keeps the review LIST (read path) and drops the submission form.
