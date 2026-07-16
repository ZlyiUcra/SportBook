# Feature Specification: SportBook Venue Booking

**Feature Branch**: `001-sportbook-venue-booking`

**Created**: 2026-07-15

**Status**: Draft

**Input**: User description: "SportBook is a platform for booking sports venues (courts and fields). Customers search venues by city and sport type, view available time slots, and book or cancel bookings. Venue owners manage their own venues and courts, confirm incoming bookings, and see who booked what. Authenticated users leave reviews on venues they have used. The product is designed to support future monetization on both sides of the marketplace."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Book a sports court (Priority: P1)

A customer searches for available sports venues by city and sport type, views available time slots for a chosen court, and completes a booking for a chosen time slot.

**Why this priority**: This is the core value proposition of the product - without booking, there is no product. Every other story only supports this core transaction.

**Independent Test**: Can be fully tested by having a customer with an account search a venue, view free time slots, and submit a booking, then verifying the booking appears in their booking history with a computed total price. Delivers standalone value even before self-service venue management exists, using a seeded venue.

**Acceptance Scenarios**:

1. **Given** a court has no existing bookings for a time slot, **When** a customer requests to book that slot within the court's operating hours, **Then** the booking is created, priced automatically from the court's published hourly rate, and appears in the customer's booking history.
2. **Given** a court already has a pending or confirmed booking overlapping a requested time slot, **When** a customer attempts to book the same or an overlapping slot, **Then** the request is rejected and the customer is told the slot is unavailable.
3. **Given** a customer has an upcoming booking, **When** they cancel it before the start time, **Then** the booking status changes to cancelled and the slot becomes available to others.

---

### User Story 2 - Manage venue and courts (Priority: P2)

A venue owner registers their sports facility, lists one or more courts with pricing and operating hours, and reviews and confirms bookings made against their venue.

**Why this priority**: Without venue owners populating real venues and courts, there is nothing for customers to book - this is the supply side that must exist for Story 1 to have real content.

**Independent Test**: Can be tested by having a venue owner create a venue, add a court with a price and operating hours, and verify the court becomes searchable and bookable, independent of whether any customer has booked yet.

**Acceptance Scenarios**:

1. **Given** a venue owner is logged in, **When** they create a venue and add a court with a sport type, hourly price, and operating hours, **Then** the court becomes searchable and bookable by customers.
2. **Given** a venue owner has courts with bookings, **When** they view their venue's bookings, **Then** they see only bookings for their own venues, never another owner's venue.
3. **Given** a venue owner receives a new booking request, **When** they confirm it, **Then** the booking status changes from pending to confirmed and the customer sees the confirmed status.

---

### User Story 3 - Build trust through reviews (Priority: P3)

An authenticated user reads reviews left by other users on a venue before booking, and leaves their own rating and comment about a venue they have used.

**Why this priority**: Reviews increase booking confidence but are not required for the core booking transaction to function - the product delivers value without them, they just improve conversion and trust over time.

**Independent Test**: Can be tested by having a user post a rating and comment on a venue and verifying it appears in that venue's review list for other users to read.

**Acceptance Scenarios**:

1. **Given** an authenticated user is viewing a venue, **When** they submit a rating and comment, **Then** the review appears in the venue's review list.
2. **Given** a venue has multiple reviews, **When** any user views the venue, **Then** they see the list of reviews and the venue's average rating.

---

### Edge Cases

- What happens when two customers attempt to book the same time slot for the same court at nearly the same moment? The system must guarantee only one booking succeeds (see FR-004).
- What happens when a customer requests a booking outside the court's operating hours, or with a start time in the past? The request is rejected.
- What happens when a venue owner attempts to view, modify, or delete a venue or court they do not own? The request is rejected (see FR-008).
- What happens when a venue owner tries to remove a venue or court that has upcoming, non-cancelled bookings? The removal is blocked until those bookings are resolved (see FR-009).
- What happens when a user who never booked a venue tries to leave a review for it? Allowed in this iteration - review authorship is not tied to a completed booking (documented in Assumptions).
- What happens when a customer attempts to cancel a booking less than 2 hours before its start time? The cancellation is rejected (see FR-005).
- What happens when a venue owner attempts to cancel a customer's booking directly? Not permitted - only the customer may cancel their own booking (see FR-011).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow customers to search venues by city and by sport type.
- **FR-002**: System MUST show customers the available (unbooked, within operating hours) time slots for a selected court on a selected date.
- **FR-003**: System MUST allow customers to book an available time slot for a court, computing the total price automatically from the court's published hourly rate and the requested duration; any price supplied by the customer is ignored.
- **FR-004**: System MUST prevent two overlapping bookings from existing for the same court at the same time, even under concurrent booking attempts.
- **FR-005**: System MUST allow customers to cancel their own upcoming bookings up until 2 hours before the booking's start time; requests to cancel within that window are rejected.
- **FR-006**: System MUST prevent customers from viewing or cancelling bookings that belong to other customers.
- **FR-007**: System MUST allow venue owners to create, update, and remove their own venues and courts.
- **FR-008**: System MUST prevent venue owners from viewing, modifying, or deleting venues, courts, or bookings they do not own.
- **FR-009**: System MUST prevent removal of a venue or court that has upcoming, non-cancelled bookings.
- **FR-010**: System MUST allow venue owners to view all bookings made against their own venues, including the information needed to identify and honor the booking.
- **FR-011**: System MUST allow venue owners to confirm a pending booking, transitioning it to a confirmed status. Venue owners cannot cancel a customer's booking; cancellation is customer-initiated only (see FR-005).
- **FR-012**: System MUST allow authenticated users to leave one rating and comment per venue they choose to review.
- **FR-013**: System MUST display the list of reviews and an average rating for each venue.
- **FR-014**: System MUST require account registration and authentication for all interaction with the platform, including venue search and browsing; there is no unauthenticated access.
- **FR-015**: System MUST record a subscription tier (free or premium) on each user account as a foundation for future monetization; this iteration does not restrict any feature based on tier.

### Key Entities *(include if feature involves data)*

- **User**: A registered account with a role (customer, venue owner, or admin) and a subscription tier (free or premium, currently unused for gating).
- **Venue**: A sports facility listed by a venue owner, with a name, location, and description; owned by exactly one user.
- **Court**: A single bookable playing surface within a venue, with a sport type, hourly price, and operating hours.
- **Booking**: A reservation of a court for a specific time range by a customer, with a status (pending, confirmed, cancelled, or completed) and a computed total price.
- **Review**: A rating and comment left by an authenticated user about a venue.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A customer can find a bookable time slot and complete a booking in under 2 minutes.
- **SC-002**: Zero double-bookings occur for the same court and time slot, even when multiple customers attempt to book concurrently.
- **SC-003**: A venue owner can list a new court and have it appear in customer search results in under 1 minute.
- **SC-004**: 100% of attempts by a user to view or modify another user's venue, court, or booking data are blocked.
- **SC-005**: The platform supports at least 500 concurrent venue search requests (`GET /venues`) with p95 response time under 500ms, no more than 2x the single-user baseline p95 latency.

## Assumptions

- Bookings are made in whole-hour increments, matching the courts' hourly pricing model.
- A review is not required to be tied to a completed booking at the venue in this iteration; any authenticated user may review any venue.
- Standard email/password authentication with short-lived access tokens is used; no third-party identity provider (SSO/OAuth) is required for this iteration.
- No payment processing exists yet; the subscription tier field is a placeholder for future monetization work and does not gate any current functionality.
