# Phase 1 Data Model: Review edit window and minimum edit comment length

No persistent schema change (no table, column, or migration). This feature adds two write-time rules
on the replace path of an existing review, over data that already exists.

## Review (unchanged shape, two new replace-time rules)

No field change. `Review.CreatedAt` already exists and is set exactly once at first creation; the
existing replace branch mutates only `Rating` and `Comment` and never writes `CreatedAt` (001/006
invariant). This feature reads that immutable `CreatedAt` to bound the edit window and adds a comment
rule on the edit request - it stores nothing new.

- One review per customer per venue, create-or-replace (201 create / 200 replace) - unchanged (006).
- The 006 eligibility gate (a completed, confirmed booking on the venue) - unchanged, checked
  independently and before the new rules.

## Edit-window rule (new, request-time - not stored)

Evaluated in `ReviewService.CreateOrReplaceAsync` on the replace branch (an existing review was
found), using the injected `TimeProvider` for `now`:

| Actor state | Predicate | Outcome |
|---|---|---|
| Replacing within 24h of original creation | `now <= existing.CreatedAt + 24h` | edit proceeds |
| Replacing after 24h of original creation | `now > existing.CreatedAt + 24h` | rejected - `REVIEW_EDIT_WINDOW_CLOSED` (409) |
| First-time submission (no existing review) | not evaluated | create proceeds (window rule N/A) |

- Window measured from the immutable `CreatedAt`; a replace never advances it (FR-002).
- A rejected replace leaves the stored `Rating`/`Comment` untouched (FR-003).
- Visibility is unaffected - a review past its window still lists to everyone (FR-004).

## Edit-comment rule (new, request-time - not stored)

Evaluated on the replace branch only, after the edit-window check passes:

| Request comment (on a replace) | Outcome |
|---|---|
| present and, trimmed, >= 10 characters | accepted |
| missing, empty, or trimmed < 10 characters | rejected - `REVIEW_COMMENT_TOO_SHORT` (400) |
| (first-time submission) | rule not applied - comment stays optional (FR-006) |

- Trim before counting so whitespace-only does not satisfy the length.
- Rating validation (1-5) is independent and still runs first (006), unchanged.

## Consumers

- Backend: `ReviewService.CreateOrReplaceAsync` gains the two guard clauses on its replace branch;
  `ListByVenueAsync` (the review list) is unchanged.
- Frontend review action (My bookings): reads the existing review's `createdAt` to show the edit
  form (window open) or a read-only view (window closed); the edit form enforces the >=10-char
  comment only in edit mode.
- No response DTO changes - `createdAt` already rides `ReviewResponse` and the frontend `Review`.
