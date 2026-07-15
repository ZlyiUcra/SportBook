# Phase 1 Data Model: SportBook Venue Booking

Entities as agreed in the consilium artifact and spec Key Entities section, with fields,
relationships, and validation rules derived from the functional requirements. Field names use C#
PascalCase; no implementation types (EF Core attributes, SQL types) are specified here - those
belong in the Infrastructure layer during implementation.

## User

Represents a registered account.

| Field | Type | Notes |
|---|---|---|
| Id | Guid | Primary key |
| Name | string | Required, max 200 chars |
| Email | string | Required, unique, valid email format |
| PasswordHash | string | Never exposed in any response DTO |
| Role | enum: Customer, VenueOwner, Admin | Set at registration to `Customer`; changed only by an Admin-only process outside this iteration's scope |
| SubscriptionTier | enum: Free, Premium | Defaults to `Free`; not used for feature gating in this iteration (future monetization placeholder) |
| CreatedAt | DateTime (UTC) | Set on creation |

**Validation rules**: Email must be unique across all users (registration fails otherwise).
`Role` cannot be supplied by the client on registration - always `Customer` (FR from consilium
authorization checklist).

## RefreshToken

Supports `/auth/refresh` and `/auth/logout` (see research.md).

| Field | Type | Notes |
|---|---|---|
| Id | Guid | Primary key |
| UserId | Guid (FK -> User) | Owner of the token |
| TokenHash | string | Hash of the refresh token value, never the raw token |
| ExpiresAt | DateTime (UTC) | |
| RevokedAt | DateTime? (UTC) | Null while active; set on logout or rotation |
| CreatedAt | DateTime (UTC) | |

**Validation rules**: A refresh request with an expired or revoked token is rejected and requires
re-login.

## Venue

A sports facility listed by a venue owner.

| Field | Type | Notes |
|---|---|---|
| Id | Guid | Primary key |
| OwnerId | Guid (FK -> User) | Must reference a user with Role = VenueOwner |
| Name | string | Required, max 200 chars |
| City | string | Required - used as a search filter |
| Address | string | Required |
| Description | string? | Optional |
| CreatedAt | DateTime (UTC) | |

**Relationships**: One Venue has many Courts, many Reviews. One User (VenueOwner) has many Venues.

**Validation rules**: Only the owning user (or Admin) may update/delete a Venue. A Venue with any
Court that has an upcoming, non-cancelled Booking cannot be deleted (spec FR-009).

## Court

A single bookable playing surface within a Venue.

| Field | Type | Notes |
|---|---|---|
| Id | Guid | Primary key |
| VenueId | Guid (FK -> Venue) | |
| Name | string | Required, e.g. "Court 1" |
| SportType | enum: Tennis, Football, Basketball, Padel, Badminton, Volleyball, Other | Fixed vocabulary so search filtering is exact-match, not free text |
| PricePerHour | decimal | Required, > 0 |
| OpeningTime | TimeOnly | Daily opening time |
| ClosingTime | TimeOnly | Daily closing time, must be after OpeningTime |
| IsActive | bool | Defaults true; inactive courts are hidden from search but keep booking history |
| CreatedAt | DateTime (UTC) | |

**Relationships**: One Court belongs to one Venue, has many Bookings.

**Validation rules**: Only the owning venue's owner (or Admin) may create/update/delete a Court. A
Court with an upcoming, non-cancelled Booking cannot be deleted (spec FR-009).

## Booking

A reservation of a Court for a time range by a Customer.

| Field | Type | Notes |
|---|---|---|
| Id | Guid | Primary key |
| CourtId | Guid (FK -> Court) | |
| UserId | Guid (FK -> User) | The customer who made the booking |
| StartTime | DateTime (UTC) | Must align to a whole-hour boundary within the court's operating hours |
| EndTime | DateTime (UTC) | Must be `StartTime` + a whole number of hours |
| Status | enum: Pending, Confirmed, Cancelled, Completed | See state transitions below |
| TotalPrice | decimal | Server-computed as `Court.PricePerHour * hours`; never accepted from the client |
| CreatedAt | DateTime (UTC) | |

**Relationships**: One Booking belongs to one Court and one User (customer).

**State transitions** (Booking.Status):

```text
Pending --(venue owner confirms)--> Confirmed
Pending --(customer cancels, >2h before start)--> Cancelled
Confirmed --(customer cancels, >2h before start)--> Cancelled
Confirmed --(EndTime has passed)--> Completed   [computed on read, not a stored transition]
```

- Created as `Pending`.
- Only the owning customer may cancel (`Pending` or `Confirmed` -> `Cancelled`), and only more
  than 2 hours before `StartTime` (spec FR-005). Venue owners cannot cancel a customer's booking
  (spec FR-011).
- Only the venue owner (via the Court's Venue) may confirm (`Pending` -> `Confirmed`).
- `Completed` is derived: any `Confirmed` booking whose `EndTime` is in the past is treated as
  Completed when displayed; this does not require a background job or a stored transition for
  this iteration.

**Validation rules**: No two Bookings for the same Court may have overlapping
`[StartTime, EndTime)` ranges unless one of them is `Cancelled` (spec FR-004); this MUST be
enforced with a mechanism safe under concurrent requests (e.g., a database-level exclusion
constraint or a serializable transaction around the check-then-insert), not only an
application-level check, given the confirmed consilium finding that this is the single
highest-priority gap in the whole review.

## Review

A rating and comment left by an authenticated user about a Venue.

| Field | Type | Notes |
|---|---|---|
| Id | Guid | Primary key |
| VenueId | Guid (FK -> Venue) | |
| UserId | Guid (FK -> User) | Author |
| Rating | int | Required, 1-5 |
| Comment | string? | Optional, max 2000 chars |
| CreatedAt | DateTime (UTC) | |

**Relationships**: One Review belongs to one Venue and one User (author).

**Validation rules**: A user may leave at most one Review per Venue (one rating represents their
current opinion; a second submission updates the existing review rather than creating a
duplicate). Review authorship is not tied to a completed Booking at that venue in this iteration
(spec Assumptions).
