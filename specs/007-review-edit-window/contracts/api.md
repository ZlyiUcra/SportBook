# API Contracts: Review edit window and minimum edit comment length

Delta contract on top of `specs/006-reviews-after-completed/contracts/api.md`. Everything not listed
here is unchanged. No endpoint is added, removed, or has its request/response shape changed - the
only change is two new rejection reasons on the review create/replace endpoint, applied only when
the caller already has a review for the venue (a replace).

## POST review (create or replace) - two new replace-only preconditions

The existing "create or replace my review for a venue" endpoint is unchanged in path, auth, request
body (`rating` 1-5, optional `comment`), and success responses (201 create / 200 replace). When the
authenticated customer already has a review for the venue (this call is a replace), two new
server-side preconditions apply:

- **Edit window**: the replace is allowed only while the current time is within 24 hours of the
  existing review's original creation time. Past that, it MUST be rejected with `409 Conflict` and
  error code `REVIEW_EDIT_WINDOW_CLOSED`. No review is modified. The window is measured from the
  original creation time and is never reset by an earlier replace.
- **Comment length**: the request `comment` MUST be present and, trimmed, at least 10 characters.
  Otherwise the replace MUST be rejected with `400 Bad Request` and error code
  `REVIEW_COMMENT_TOO_SHORT`. No review is modified.

Both preconditions apply ONLY to a replace. A first-time submission (the customer has no existing
review for the venue) is unaffected: its comment stays optional and no edit window applies.

Check order on this endpoint after this feature:

- `400 INVALID_RATING` - rating outside 1-5 (006, unchanged; runs first).
- `404 VENUE_NOT_FOUND` - no such venue (006, unchanged).
- `409 REVIEW_NOT_ELIGIBLE` - caller has no completed (Confirmed, past) booking at the venue (006,
  unchanged).
- On the replace branch only, in order: `409 REVIEW_EDIT_WINDOW_CLOSED` (NEW) then
  `400 REVIEW_COMMENT_TOO_SHORT` (NEW).
- `401` - unauthenticated (unchanged).

The window check precedes the comment check: if the window is closed the edit is impossible, so that
reason is reported before the comment is inspected. The two codes are distinct so a caller can tell
the two failures apart.

## GET venue reviews (list) - unchanged

Unchanged in path, auth, paging, response shape, and visibility. A review past its 24-hour edit
window still lists normally to every viewer - the window governs only its author's ability to change
it, never its display. The response still carries `createdAt`, which the client uses to decide
whether to offer an edit.

## Superseded / unchanged

- No booking, venue, court, city, or auth endpoint changes. The 006 eligibility gate, the
  one-review-per-customer-per-venue rule, and the create-or-replace (201 vs 200) semantics are
  unchanged. This feature only adds the two replace-only rejection reasons above; no new endpoint and
  no response-shape change (the star widget and the review entry's location are unchanged too).
