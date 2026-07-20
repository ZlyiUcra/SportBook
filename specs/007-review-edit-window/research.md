# Phase 0 Research: Review edit window and minimum edit comment length

All product choices were confirmed by the user in the 2026-07-20 discussion (spec Assumptions). This
file pins the mechanisms. No NEEDS CLARIFICATION markers remain.

## The 24-hour edit window: measured from the existing CreatedAt, on the replace branch only

- **Decision**: In `ReviewService.CreateOrReplaceAsync`, after loading the caller's existing review
  and finding one (the replace branch), reject the replacement when
  `now > existing.CreatedAt + TimeSpan.FromHours(24)`, where `now` comes from the already-injected
  `TimeProvider`. The error is a new domain code `REVIEW_EDIT_WINDOW_CLOSED` (HTTP 409), with a
  clear message that the 24-hour edit window has passed. A first-time submission (no existing
  review) never hits this check.
- **Rationale**: `Review.CreatedAt` is set exactly once, at first creation, and the existing replace
  branch mutates only `Rating` and `Comment` - it never writes `CreatedAt` (001/006 invariant). So
  "a replace never resets the window" (spec FR-002) is already guaranteed by the data model with
  nothing added; the window is a pure function of the immutable creation time and the request time.
  The check is in-memory on a row already loaded for the upsert, so it costs no extra query.
- **Boundary**: strictly after 24 hours blocks the edit (`now > CreatedAt + 24h` rejects;
  `now <= CreatedAt + 24h` allows), matching the spec edge case.
- **Why 409, not 403/422**: the window being closed is a temporal state of the resource, the same
  shape as the existing `REVIEW_NOT_ELIGIBLE` (409) - a "you cannot do this in the resource's
  current state" conflict rather than an auth failure (403) or a malformed body (422). Reusing 409
  keeps the review endpoint's rejection family consistent; the distinct code, not the status,
  disambiguates it (spec SC-004).
- **Alternatives considered**: storing an `EditableUntil` / `LastEditedAt` column (rejected -
  derivable from the immutable `CreatedAt`; a new column and migration for no gain and a drift
  risk); enforcing the window only on the client (rejected - the client cannot be trusted to limit
  its own edit rights; must be server-side, spec FR-001); resetting the window on each edit
  (rejected - the user explicitly chose "from original creation, never reset").

## The minimum comment on an edit: 10 chars, non-empty, replace branch only

- **Decision**: On the replace branch (existing review present), require the request comment to be
  present and, after trimming, at least 10 characters; otherwise reject with a new domain code
  `REVIEW_COMMENT_TOO_SHORT` (HTTP 400), message stating an edit needs a comment of at least 10
  characters. A first-time submission (no existing review) keeps the comment fully optional - the
  check is not applied there (spec FR-006).
- **Rationale**: the user's complaint is that an edit can blank out or gut the comment; requiring a
  real comment specifically on edits closes that without changing the low-friction first-time
  submission. 400 matches the existing `INVALID_RATING` validation (also 400) - a request-content
  failure. Trimming before counting stops "          " (whitespace) from passing as 10 characters.
- **Ordering vs the window check**: the edit-window check runs first - if the window is closed no
  edit is possible at all, so a closed window is reported before inspecting the comment; only an
  in-window edit proceeds to the comment-length check. This keeps the two reasons distinct and
  gives the more fundamental one precedence (spec SC-004).
- **Ordering vs the 006 gates**: the existing order is rating (400) then venue-exists (404) then
  eligibility (409) then load-existing. The two new checks slot in on the replace branch, after
  `existing` is loaded: window (409) then comment (400). Rating validation still runs first and is
  unchanged, so a bad rating is still rejected on its own regardless of the new rules.
- **Alternatives considered**: applying the min length to first-time submissions too (rejected - the
  user scoped it to edits; a first review's comment stays optional); a different threshold
  (rejected - the user fixed 10 characters); enforcing only on the client (rejected - server-side is
  the source of truth; the client rule is UX, not the guarantee).

## Frontend: edit-mode-only validation and a read-only-after-window action

- **Decision**: The review-create form (`ReviewForm`) takes an `isEdit` flag (true when replacing an
  existing review). Its zod schema requires a non-empty, >=10-char comment only when `isEdit`;
  otherwise the comment stays optional. The My-bookings review action (the sole entry to editing per
  006) computes, from the caller's existing review `createdAt`, whether the 24-hour window is still
  open: open -> the edit form as today; closed -> the caller's review shown read-only (no edit
  form). Both server rejections (`REVIEW_EDIT_WINDOW_CLOSED`, `REVIEW_COMMENT_TOO_SHORT`) are
  surfaced via the existing `ApiRequestError` path if they still occur.
- **Rationale**: the frontend already receives `createdAt` on each review (`ReviewResponse` /
  frontend `Review`), so the window is computable client-side with no new field - the client hides
  an action it knows the server would reject, but the server stays the authority (a stale client
  that submits anyway is still rejected). Making the min-length rule conditional on `isEdit` keeps
  the first-time submission exactly as it is (optional comment) while enforcing the tighter rule on
  edits, mirroring the backend split precisely.
- **Alternatives considered**: adding a server field like `canEdit`/`editableUntil` to the review
  response (rejected - `createdAt` already suffices; no DTO change needed); always enforcing the
  min length in the form regardless of mode (rejected - would wrongly block a valid first-time
  no-comment submission); leaving the window entirely to the server and always showing the edit form
  (rejected - a worse experience; the user sees an edit affordance that always fails after a day).

## Scope confirmation

- **Decision**: This feature only constrains the replace path of an existing review. The 006
  eligibility gate (a completed, confirmed booking), the star-rating widget, the relocation of the
  review entry to My bookings, and the review list on the venue page are all unchanged.
- **Rationale**: the user framed this narrowly as "how long a review stays editable" plus "a real
  comment on edits"; nothing about who may first submit changes (spec FR-007).
