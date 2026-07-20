# Feature Specification: Review edit window and minimum edit comment length

**Feature Branch**: `007-review-edit-window`

**Created**: 2026-07-20

**Status**: Draft

**Input**: User description: "Review edit window and minimum comment length: a customer may edit
(replace) their venue review only within 24 hours of when they first created it - after that
window the review becomes read-only to its author. When editing an already-submitted review within
that window, the comment must not be empty and must be at least 10 characters long. This does not
change who may submit a review in the first place (006 eligibility gate stays); it only limits how
long an existing review stays editable and tightens the comment requirement on edits."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A review is only editable for 24 hours after it was first left (Priority: P1)

A customer who left a venue review can still change it - but only within 24 hours of the moment
they first created it. Once that window has passed, the review becomes read-only to its author: it
still displays to everyone (including the customer), but no further edit succeeds, no matter how
many times the customer replaces it in between.

**Why this priority**: This is the core behavior change - it directly answers the complaint that a
review can be altered or blanked out indefinitely. It stands on its own: even without the comment
rule (US2), a time-boxed edit window already closes the loophole.

**Independent Test**: Create a review, replace it again immediately (succeeds), then attempt to
replace it again after the 24-hour window has passed (using a seeded old CreatedAt) and confirm the
attempt is rejected with a clear reason while the review's current content is unchanged.

**Acceptance Scenarios**:

1. **Given** a customer created a review less than 24 hours ago, **When** they submit a replacement
   for that same venue, **Then** the replacement is accepted and the review is updated.
2. **Given** a customer created a review more than 24 hours ago, **When** they submit a replacement
   for that same venue, **Then** the submission is rejected with a reason stating the edit window
   has closed, and the review's stored content is unchanged.
3. **Given** a customer replaced their review once within the window, **When** they submit another
   replacement still within 24 hours of the *original* creation, **Then** the replacement is
   accepted - replacing does not restart the 24-hour clock.
4. **Given** any visitor (including the review's own author) viewing a venue's reviews, **When** the
   24-hour window for a given review has closed, **Then** the review still displays normally - only
   further edits are blocked, not visibility.

---

### User Story 2 - An edit within the window must carry a real comment (Priority: P2)

When a customer edits (replaces) an already-submitted review while still inside the 24-hour window,
the comment field can no longer be left empty or blanked out - it must contain at least 10
characters. The very first time a customer submits a review, the comment stays optional as before;
this stricter rule applies only to editing a review that already exists.

**Why this priority**: This is the direct fix for "a user can erase their comment" - the time window
(US1) stops edits after a day, but within that day a customer could otherwise still blank the
comment out; this closes that gap. It depends on there being an edit action to apply the rule to
(US1), so it is the second priority.

**Independent Test**: Create a review with a comment, then attempt to replace it with an empty
comment (rejected) and with a comment under 10 characters (rejected), and confirm a replacement with
a comment of 10 or more characters succeeds.

**Acceptance Scenarios**:

1. **Given** a customer is replacing their existing review within the edit window, **When** they
   submit with an empty or missing comment, **Then** the submission is rejected with a reason
   stating a comment of at least 10 characters is required.
2. **Given** the same, **When** they submit a comment of 9 characters or fewer, **Then** the
   submission is rejected with the same reason.
3. **Given** the same, **When** they submit a comment of 10 or more characters, **Then** the
   replacement is accepted.
4. **Given** a customer submitting a review for a venue for the very first time (no existing
   review), **When** they submit with no comment at all, **Then** the submission is still accepted
   - the stricter comment rule applies only to replacing an existing review, not to a first-time
   submission.

---

### Edge Cases

- A review created before this feature existed (no prior edit-window concept): its 24-hour window is
  computed the same way, from its existing `CreatedAt` - if that is already more than 24 hours in
  the past (true for effectively every review that predates this feature), it is immediately
  read-only to its author under the new rule.
- A customer whose 006 eligibility (a completed, confirmed booking at the venue) has lapsed in some
  future sense is unaffected here - eligibility and the edit window are independent checks; this
  feature does not change 006's eligibility gate.
- A customer attempts to edit a review exactly at the 24-hour boundary: the window is closed
  strictly after 24 hours have elapsed since creation (i.e. `now > createdAt + 24h` blocks the edit;
  `now <= createdAt + 24h` still allows it).
- A rejected edit attempt (window closed, or comment too short) never modifies the stored review -
  the customer's last valid content stays exactly as it was.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A customer MUST be able to replace their existing venue review only while the current
  time is within 24 hours of that review's original creation time; once 24 hours have elapsed since
  creation, further replacement attempts by that customer MUST be rejected.
- **FR-002**: The 24-hour window MUST be measured from the review's original creation time, not from
  its most recent edit - replacing a review must never extend or restart the window.
- **FR-003**: A rejected replacement (window closed) MUST leave the review's stored rating and
  comment unchanged, and MUST report a single clear reason distinguishable from other rejection
  reasons (e.g. the existing 006 eligibility rejection).
- **FR-004**: The 24-hour edit window MUST NOT affect whether a review is displayed to any viewer -
  it only governs whether its author can further change it.
- **FR-005**: When a customer replaces an existing review (within the edit window), the comment MUST
  be present and MUST be at least 10 characters long; a missing, empty, or under-length comment MUST
  be rejected with a clear reason.
- **FR-006**: The minimum-comment-length rule from FR-005 MUST NOT apply to a customer's first-ever
  submission of a review for a venue (no prior review by that customer at that venue) - a first
  submission's comment remains optional, unchanged from 006 behavior.
- **FR-007**: This feature MUST NOT change who is allowed to submit or replace a review in the first
  place - the 006 eligibility gate (a completed, confirmed booking at the venue) remains the
  precondition checked independently of the rules in this feature.

### Key Entities

- **Review**: Existing entity (006). This feature adds two rules governing *replacing* an existing
  review: a 24-hour edit window measured from its original creation time, and a stricter (at least
  10 characters, non-empty) comment requirement on the edit itself. The entity's shape and the
  006 eligibility precondition are unchanged.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of review replacement attempts more than 24 hours after original creation are
  rejected, and the review's content is provably unchanged by the attempt.
- **SC-002**: 100% of review replacement attempts within 24 hours of original creation, with a
  comment of at least 10 characters, are accepted.
- **SC-003**: 100% of review replacement attempts with an empty or under-10-character comment are
  rejected, while a first-time review submission with no comment is still accepted in 100% of
  attempts.
- **SC-004**: A customer can always tell, from the rejection reason alone, whether their replacement
  failed because the edit window closed or because the comment was too short - the two reasons are
  never conflated.

## Assumptions

- All key decisions were confirmed by the user in the 2026-07-20 discussion: the edit window is
  fixed at 24 hours, measured from the review's original creation time (a replace never resets the
  clock); the minimum comment length on an edit is 10 characters, non-empty.
- The minimum-comment-length rule applies only to *editing* (replacing) an existing review, per the
  user's own framing ("if editing, after it was already submitted") - a first-time submission's
  comment stays optional, as it already is under 006.
- The 006 eligibility gate (a completed, confirmed booking at the venue) is unchanged and independent
  of this feature; both checks apply together whenever a customer attempts to replace a review that
  is inside 006's eligibility rule.
- A review that becomes read-only under the 24-hour rule is not deleted, hidden, or flagged in any
  way to other viewers - it simply cannot be changed further by its author.
- Rating is unaffected by this feature's comment-length rule - the existing 1-5 rating validation
  (006) is untouched; only the comment gets the new minimum-length check, and only on edits.
