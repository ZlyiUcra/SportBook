# Phase 0 Research: Reviews only after a completed, confirmed game

All product choices were made by the user in the 2026-07-20 discussion (spec Assumptions). This
file pins the mechanisms. No NEEDS CLARIFICATION markers remain.

## Eligibility gate: a server-side EXISTS over the customer's completed bookings

- **Decision**: In `ReviewService.CreateOrReplaceAsync`, before creating or replacing the review,
  assert the customer has a completed game at the venue with an existence check:
  `db.Bookings.AnyAsync(b => b.UserId == userId && b.Court!.VenueId == venueId && b.Status ==
  BookingStatus.Confirmed && b.EndTime <= now, ct)`, where `now` comes from the already-injected
  `TimeProvider`. If it returns false, reject with a new domain error `REVIEW_NOT_ELIGIBLE` (mapped
  to HTTP 409) carrying a single clear reason ("complete a game at this venue first"). The existing
  rating validation (400 `INVALID_RATING`) and the venue-exists check (404 `VENUE_NOT_FOUND`) stay,
  and run so a bad rating is still rejected on its own regardless of eligibility.
- **Rationale**: "Completed" is exactly feature 005's derivation - `Status == Confirmed &&
  EndTime <= now` - reused verbatim so the two features cannot drift on what "played" means. An
  `EXISTS` is the minimal check: it stops at the first qualifying booking, translates to a bounded
  indexed subquery, and never materializes the customer's booking history. It composes with the
  existing create-or-replace: eligibility is checked first, then the one-per-user-per-venue upsert
  proceeds unchanged, so replacing a review re-checks eligibility (a legacy author who no longer
  qualifies cannot edit - spec edge case).
- **Why the join is `b.Court!.VenueId`**: a booking references a court, and a court belongs to a
  venue; eligibility is "a booking on a court of this venue", so the predicate reaches through the
  court to its `VenueId`. No extra Include is needed - EF translates the navigation into the SQL
  join for the `EXISTS`.
- **Alternatives considered**: gating on the client by checking the customer's bookings in the SPA
  (rejected - a client cannot be trusted to certify a completed game; the gate must be server-side,
  spec SC-002); a stored `HasPlayed`/eligibility flag on the user or a review-eligibility table
  (rejected - derivable from bookings that already exist, storing it invites drift and a migration
  for no gain); gating per booking, i.e. one review per completed booking (rejected - the user chose
  one review per customer per venue; several completed bookings are just multiple entry points to
  the same single review, spec edge case).

## Relocating the review entry: off the venue page, onto completed bookings

- **Decision**: Remove the review submission `Card`/`ReviewForm` from the venue-detail page and keep
  its review-list display (and the list query) intact. On the customer "My bookings" page, each row
  whose derived status is Completed (Confirmed, past - already computed for the 005 status filter)
  gains a review action that opens the review entry for that booking's venue, pre-filled with the
  customer's existing review for that venue if one exists. The action is absent on non-completed
  rows (upcoming, pending, cancelled).
- **Rationale**: The gate makes an always-present venue-page form misleading (most viewers cannot
  submit); surfacing the entry only next to a completed game shows it exactly when it is relevant and
  never to a browser (spec US2, FR-003/FR-004/FR-005). The venue page keeps the review LIST because
  prospective customers still need social proof (user's explicit correction: "review list stays on
  the venue page for next users"). "My bookings" already loads the customer's bookings with the
  derived status, so the action reads data it already has - no new query on that page. The prefill
  reuses the existing "find my review for this venue" lookup, just moved to the point of entry.
- **Where the form lives**: the `ReviewForm` (review-create feature slice) is unchanged as a
  component boundary; only its host moves from the venue page to a review entry reachable from My
  bookings (a dedicated route or an in-place panel on the bookings page - an implementation choice
  for tasks, both keep the form optional and un-nagged). The venue page keeps only the read path.
- **Alternatives considered**: leaving the form on the venue page but disabling it for ineligible
  users (rejected - still advertises reviewing to everyone and clutters the page; the user asked to
  move it, not gate it in place); a separate top-level "write a review" page decoupled from a
  booking (rejected - loses the "next to the game you played" context that makes the entry
  meaningful); removing the review list too (rejected - the user explicitly kept it for social
  proof).

## Rating input: an in-house five-star widget

- **Decision**: Replace the `<select>` in `ReviewForm` with a local five-star control. Hovering a
  star (or moving across the row) previews that many filled stars; clicking sets an integer 1-5
  proportional to the horizontal position within the row (leftmost -> 1, rightmost -> 5). It is a
  real form control wired to the existing RHF field (`rating`, still `valueAsNumber` semantics and
  the same 1-5 zod validation), so submission and validation are unchanged. It is operable by
  keyboard (focusable; arrow/number keys move the rating across the full 1-5 range) and by touch
  (tap a star), not pointer-hover only, with aria labels for the current and previewed rating.
- **Rationale**: Stars are faster and more intuitive than a dropdown (spec US3) while keeping the
  underlying value an integer 1-5, so nothing downstream changes. Building it from existing
  primitives (buttons/spans + the icon set already bundled) avoids a new dependency, which per repo
  rules would need explicit user sign-off; the widget is small and self-contained. Keyboard/touch
  operability is a hard requirement (spec FR-009), so the control is a set of focusable elements with
  a keyboard model, not a bare pointer-driven strip.
- **Proportional click**: the row is treated as five equal segments; a click's x offset from the
  left edge maps to segment index + 1 (clamped 1-5), matching the user's "click proportionally from
  1 to 5" request. Hover preview uses the same segment mapping.
- **Alternatives considered**: adding a rating-widget npm dependency (rejected pending explicit user
  approval - a small in-house control is enough and repo rules require sign-off for new deps); a
  native `<input type="range">` styled as stars (rejected - weaker affordance, awkward discrete 1-5
  snapping and hover preview); keeping the dropdown (rejected - the user asked for stars). No new
  dependency is introduced by this decision; if the user later wants a library, that is a separate,
  explicitly-approved change.

## Deferred: "played but never confirmed"

- **Decision**: A customer who genuinely played but whose booking the owner never confirmed is NOT
  eligible under this feature and has no in-app recourse here. This case, and any email/feedback
  mechanism for it, is explicitly deferred to a later feature (user's instruction).
- **Rationale**: The confirmation step (owner confirms a Pending booking, feature 001) is the only
  "played" signal available today; inventing an alternate proof-of-play is its own feature. Scoping
  it out keeps this feature to the gate + relocation + widget the user asked to build now.
- **Alternatives considered**: auto-confirming past Pending bookings to grant eligibility (rejected -
  changes booking semantics and would let no-shows review); a dispute flow (rejected - explicitly
  deferred). A stale Pending-past booking remains non-eligible, consistent with 005 where it matches
  only the "All" filter.
