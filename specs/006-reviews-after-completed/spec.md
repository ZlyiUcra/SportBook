# Feature Specification: Reviews only after a completed, confirmed game

**Feature Branch**: `006-reviews-after-completed`

**Created**: 2026-07-20

**Status**: Draft

**Input**: User description: "A customer may review a venue only after they have actually played
there - a booking the venue's owner confirmed and whose time has passed. Today any logged-in
customer can review any venue from the venue page, before ever booking or playing, which undermines
trust in the reviews. Three parts: gate review submission on a completed, confirmed booking; move
the review entry off the venue page to the customer's completed bookings (so it appears only when
relevant, and is never forced); and replace the rating dropdown with an interactive 5-star widget
(hover preview + click, 1 to 5). Reviews stay optional. The 'played but never confirmed' case is
deferred to a later feature."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Only customers who played can review (Priority: P1)

A customer can submit or change a review for a venue only if they have actually completed a game
there: a booking on one of the venue's courts that the owner confirmed and whose end time has
passed. A customer who only browsed, booked but did not play, or whose booking was never confirmed
or was cancelled cannot review that venue.

**Why this priority**: Trust is the entire point of reviews. A review from someone who never played
at the venue is noise that misleads other customers; this gate is the core value and stands on its
own even before the entry point or the star widget change.

**Independent Test**: As a customer with a completed booking at a venue, submit a review and confirm
it is accepted; as a customer with no completed booking there, attempt to submit and confirm it is
rejected with a clear reason.

**Acceptance Scenarios**:

1. **Given** a customer has a booking at a venue whose status is Confirmed and whose end time is in
   the past, **When** they submit a review for that venue, **Then** the review is accepted.
2. **Given** a customer has no Confirmed-and-past booking at a venue (only Pending, Cancelled, or
   future bookings, or none at all), **When** they submit a review for that venue, **Then** the
   submission is rejected with a reason stating they must complete a game there first.
3. **Given** a customer who is eligible to review a venue and has already reviewed it, **When** they
   submit a new review for that venue, **Then** their existing review is replaced (not duplicated).
4. **Given** a customer who is eligible to review a venue, **When** they submit a review with no
   rating or an out-of-range rating, **Then** the submission is rejected with a clear reason (the
   eligibility gate does not weaken the existing rating validation).

---

### User Story 2 - Reach the review from your completed bookings, not the venue page (Priority: P2)

A customer no longer finds a review form on the venue page. Instead, each of their completed
bookings on "My bookings" offers a way to review that booking's venue, leading to the review entry.
The venue page still shows the reviews others left (useful when choosing a venue); only the
submission form is gone from it.

**Why this priority**: This is the natural complement to the gate - the review is surfaced exactly
when and where it is relevant (next to a game you just completed) rather than offered to anyone
browsing. It is valuable on its own but depends on the gate to be meaningful.

**Independent Test**: Open a venue page and confirm there is no review submission form; open "My
bookings", find a completed booking, and confirm the review entry is offered there and leads to
submitting a review for that venue.

**Acceptance Scenarios**:

1. **Given** any visitor on a venue's detail page, **When** they view the page, **Then** no review
   submission form is shown (the reviews others left may still be displayed).
2. **Given** a customer with a completed booking at a venue, **When** they view "My bookings",
   **Then** that completed booking offers an action to review the venue.
3. **Given** a customer uses that action on a completed booking, **When** they follow it, **Then**
   they are taken to the review entry for that booking's venue, pre-filled with their existing
   review if they already left one.
4. **Given** a customer with only upcoming, pending, or cancelled bookings at a venue, **When** they
   view "My bookings", **Then** no review action is offered for those bookings.

---

### User Story 3 - Pick a rating with stars, not a dropdown (Priority: P3)

When leaving a review, the customer sets the 1-to-5 rating by clicking a row of five stars:
hovering previews how many stars would be selected, and clicking picks a value proportional to
where in the row they click, from 1 at the left to 5 at the right.

**Why this priority**: A usability polish to the rating input; the rating still works without it, but
stars are faster and more intuitive than a dropdown.

**Independent Test**: Open the review entry and confirm the rating is a five-star control where
hovering highlights stars and clicking selects a value from 1 to 5.

**Acceptance Scenarios**:

1. **Given** a customer at the review entry with no rating chosen, **When** they hover over the
   third star, **Then** three stars are shown as the preview.
2. **Given** the same, **When** they click at a position corresponding to four stars from the left,
   **Then** the rating is set to 4.
3. **Given** a customer is at the review entry, **When** they use only the keyboard (or a touch
   device), **Then** they can still set any rating from 1 to 5.

---

### Edge Cases

- A customer has several completed bookings at the same venue: the review is one per customer per
  venue, so each completed booking is just another entry point to the same single review; using any
  of them to submit replaces, not duplicates.
- A customer is viewing "My bookings" at the moment a Confirmed booking's end time passes: that
  booking becomes completed and the review option appears for it on the next refresh.
- A completed booking's venue is deleted after the game (deletion is blocked only while an upcoming
  non-cancelled booking exists, so a past booking's venue may be gone): the review action for that
  venue cannot lead to a valid submission - it is hidden, or the submission is rejected with a clear
  reason.
- A review created before this feature by a customer who would not now qualify (legacy review): the
  review stays visible to others, but its author cannot replace it unless they now meet the
  eligibility rule.
- An ineligible visitor opens a venue page: they see the reviews others left (if any) but no
  submission form, and never an error unless they go out of their way to submit.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A customer MUST be allowed to submit or replace a review for a venue only if they have
  at least one booking on a court of that venue whose status is Confirmed and whose end time is in
  the past (a completed game).
- **FR-002**: A customer who does not meet that eligibility MUST be prevented from submitting or
  replacing a review, and the system MUST reject the attempt with a single, clear reason stating the
  customer has not completed a game at that venue.
- **FR-003**: The review submission form MUST NOT be offered on the venue detail page.
- **FR-004**: A customer MUST be able to reach the review entry for a venue from their "My bookings"
  page, via an action shown alongside each of their completed (Confirmed, past) bookings for that
  venue.
- **FR-005**: The review action MUST NOT be shown for bookings that are not completed (upcoming,
  pending, or cancelled).
- **FR-006**: Submitting a review MUST remain entirely optional; the system MUST NOT require,
  obstruct, or repeatedly prompt a customer to leave a review.
- **FR-007**: The existing one-review-per-customer-per-venue rule MUST be preserved - a later
  submission by the same customer for the same venue replaces their prior review instead of adding a
  second one.
- **FR-008**: The rating input MUST be a five-star interactive control where moving the pointer
  previews a value and clicking selects an integer rating from 1 to 5 proportional to the position
  within the star row (1 at the left edge, 5 at the right).
- **FR-009**: The star rating control MUST be operable without a pointer device (keyboard) and on
  touch devices, still covering the full 1-to-5 range.
- **FR-010**: The venue detail page MUST continue to display the reviews customers have left, to any
  viewer; only the submission form is removed from that page by this feature.
- **FR-011**: Eligibility MUST persist once earned: completed and cancelled bookings are never
  deleted, so a customer who has completed a game at a venue remains eligible to review that venue.

### Key Entities

- **Review**: Existing entity - one per customer per venue (rating and optional comment). This
  feature does not change its shape; it adds the rule governing who may create or replace one.
- **Booking**: Existing entity. Its status (Confirmed) and end time (past) now also determine review
  eligibility; a completed booking is the proof a customer played at a venue.
- **Court / Venue**: Existing entities. A booking's court belongs to a venue, so eligibility is "a
  booking on a court of this venue"; that link already exists and is unchanged.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of accepted reviews come from customers who completed a confirmed game at the
  reviewed venue.
- **SC-002**: A customer who has not completed a game at a venue cannot get a review for it accepted,
  in 100% of attempts.
- **SC-003**: The venue detail page offers no review submission to any visitor, in 100% of views.
- **SC-004**: A customer can reach the review for a venue from their completed bookings without
  visiting the venue page.
- **SC-005**: A customer can set any rating from 1 to 5 with a single action on the star control.

## Assumptions

- This feature concerns **venue reviews only** (the existing review target). General feedback about
  the platform or about staff, which the user also mentioned, is out of scope here and would be a
  separate feature.
- "Completed game" means a booking with status Confirmed and end time in the past - the same derived
  "Completed" view as feature 005. The owner confirming a Pending booking (feature 001) is the
  "confirmation" step; there is no separate "played" confirmation.
- The venue detail page continues to **display** existing reviews (social proof for prospective
  customers); only the submission form is removed and relocated. (User may correct.)
- Completed and cancelled bookings are never deleted (feature 001 keeps cancelled bookings as
  records and blocks venue/court deletion only while an upcoming non-cancelled booking exists), so
  once a customer is eligible to review a venue, they remain eligible.
- The review is per customer per venue, not per booking. Several completed bookings at the same
  venue each offer the same single review entry.
- Reviews created before this feature (possibly by customers who never completed a game there) are
  kept as-is, not purged; their authors must now meet the eligibility rule to edit them.
- The "I played but the owner never confirmed" case is explicitly **out of scope** for this feature
  and deferred to a later one.
- Reviews remain optional; the platform never forces or nags.
- Map, search, booking, and cancellation flows (features 001-005) are unchanged except that the
  review submission leaves the venue page.
