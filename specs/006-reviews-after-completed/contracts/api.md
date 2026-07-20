# API Contracts: Reviews only after a completed, confirmed game

Delta contract on top of `specs/001-sportbook-venue-booking/contracts/api.md`. Everything not listed
here is unchanged. Auth posture (JWT required to submit) and the standard error shape carry over. No
endpoint is added, removed, or has its request/response shape changed by this feature - the only
change is a new rejection reason on the review create/replace endpoint.

## POST review (create or replace) - + eligibility gate

The existing "create or replace my review for a venue" endpoint is unchanged in path, auth, request
body (`rating` 1-5, optional `comment`), and success responses (201 create / 200 replace). It gains
one server-side precondition:

- The authenticated customer MUST have at least one booking on a court of the target venue whose
  status is Confirmed and whose end time is in the past (a completed game). This is checked
  server-side before the review is written.
- If the precondition fails, the endpoint MUST reject with `409 Conflict` and error code
  `REVIEW_NOT_ELIGIBLE`, message stating the customer must complete a game at the venue first. No
  review is created or modified.
- The gate does NOT weaken existing validation: an out-of-range or missing `rating` is still
  rejected with the existing `400 INVALID_RATING`, and an unknown venue with the existing
  `404 VENUE_NOT_FOUND`. These run independently of eligibility.
- Replacing an existing review re-checks eligibility (a legacy author who no longer qualifies is
  rejected the same way).

Error codes on this endpoint after this feature:

- `400 INVALID_RATING` - rating outside 1-5 (unchanged).
- `404 VENUE_NOT_FOUND` - no such venue (unchanged).
- `409 REVIEW_NOT_ELIGIBLE` - caller has no completed (Confirmed, past) booking at the venue (NEW).
- `401` - unauthenticated (unchanged).

## GET venue reviews (list) - unchanged

The endpoint that lists a venue's reviews is unchanged in path, auth, paging, and response shape. It
remains available to any viewer and still backs the review list on the venue detail page (spec
FR-010). This feature does not filter, hide, or purge existing reviews.

## Superseded / unchanged

- No booking, venue, court, city, or auth endpoint changes. The one-review-per-customer-per-venue
  rule and the create-or-replace (201 vs 200) semantics are unchanged. This feature only adds the
  `REVIEW_NOT_ELIGIBLE` precondition to the review create/replace endpoint; the relocation of the
  review entry and the star widget are frontend-only and introduce no new endpoint.
