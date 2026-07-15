# API Contracts: SportBook Venue Booking

HTTP/JSON contracts for the backend Web API. DTO type names follow the `research.md` naming
decision (`{Entity}Response`, `Create{Entity}Request`, `Update{Entity}Request`). All endpoints
require a valid JWT access token unless marked **Anonymous**; per spec FR-014 there is no
unauthenticated browsing, so venue search/detail also require authentication.

Standard error shape for all endpoints: `{ "error": { "code": string, "message": string } }`,
with `400` for validation, `401` for missing/invalid auth, `403` for ownership/role violations,
`404` for missing resources, `409` for booking-overlap/cancellation-window conflicts.

## Auth

| Method | Path | Auth | Request | Response |
|---|---|---|---|---|
| POST | /api/auth/register | Anonymous | `RegisterRequest { name, email, password }` | `AuthResponse { accessToken, refreshToken, user: UserResponse }` |
| POST | /api/auth/login | Anonymous | `LoginRequest { email, password }` | `AuthResponse` |
| POST | /api/auth/refresh | Anonymous (bears refresh token) | `RefreshRequest { refreshToken }` | `AuthResponse` (rotates the refresh token) |
| POST | /api/auth/logout | Authenticated | `LogoutRequest { refreshToken }` | `204 No Content` (revokes the token) |

`RegisterRequest` has no `role` field - every account is created as `Customer` (research.md
authorization checklist).

## Users

| Method | Path | Auth | Request | Response |
|---|---|---|---|---|
| GET | /api/users/me | Authenticated | - | `UserResponse { id, name, email, role, subscriptionTier, createdAt }` |
| PUT | /api/users/me | Authenticated | `UpdateUserRequest { name }` | `UserResponse` |

`UpdateUserRequest` has no `role` or `id` field - self-service profile update cannot change role
or identity (research.md authorization checklist).

## Venues

| Method | Path | Auth | Request | Response |
|---|---|---|---|---|
| GET | /api/venues | Authenticated | Query: `city?, sportType?, page=1, pageSize=20` | `PagedResponse<VenueSummaryResponse>` |
| GET | /api/venues/{id} | Authenticated | - | `VenueDetailResponse { id, name, city, address, description, ownerId, courts: CourtResponse[], averageRating, reviewCount }` |
| POST | /api/venues | Authenticated (VenueOwner) | `CreateVenueRequest { name, city, address, description? }` | `VenueDetailResponse` (201) |
| PUT | /api/venues/{id} | Authenticated (owner only) | `UpdateVenueRequest { name, city, address, description? }` | `VenueDetailResponse` |
| DELETE | /api/venues/{id} | Authenticated (owner only) | - | `204` or `409` if upcoming non-cancelled bookings exist (FR-009) |

`GET /venues/{id}` uses `AsSplitQuery()` or a separate aggregate query for `averageRating`/
`reviewCount` (research.md cartesian-explosion decision).

## Courts

| Method | Path | Auth | Request | Response |
|---|---|---|---|---|
| GET | /api/venues/{venueId}/courts | Authenticated | Query: `page=1, pageSize=20` | `PagedResponse<CourtResponse>` |
| POST | /api/venues/{venueId}/courts | Authenticated (venue owner only) | `CreateCourtRequest { name, sportType, pricePerHour, openingTime, closingTime }` | `CourtResponse` (201) |
| PUT | /api/courts/{id} | Authenticated (venue owner only, via Court.Venue.OwnerId) | `UpdateCourtRequest { name, sportType, pricePerHour, openingTime, closingTime, isActive }` | `CourtResponse` |
| DELETE | /api/courts/{id} | Authenticated (venue owner only) | - | `204` or `409` if upcoming non-cancelled bookings exist (FR-009) |

## Availability

| Method | Path | Auth | Request | Response |
|---|---|---|---|---|
| GET | /api/courts/{id}/availability | Authenticated | Query: `date` (yyyy-MM-dd) | `AvailabilityResponse { courtId, date, freeSlots: { start, end }[] }` |

Slots are whole-hour, within the court's operating hours, excluding any hour overlapping a
Pending or Confirmed booking (research.md availability decision).

## Bookings

| Method | Path | Auth | Request | Response |
|---|---|---|---|---|
| GET | /api/bookings | Authenticated (mine) | Query: `page=1, pageSize=20` | `PagedResponse<BookingResponse>` (only the caller's own bookings) |
| POST | /api/bookings | Authenticated | `CreateBookingRequest { courtId, startTime, endTime }` | `BookingResponse` (201) or `409` on overlap/out-of-hours (FR-004) |
| GET | /api/bookings/{id} | Authenticated (owner of the booking only) | - | `BookingResponse { id, courtId, userId, startTime, endTime, status, totalPrice, createdAt }` |
| PUT | /api/bookings/{id}/cancel | Authenticated (booking's customer only) | - | `BookingResponse` or `409` if within the 2h cutoff (FR-005) |
| PUT | /api/bookings/{id}/confirm | Authenticated (venue owner only, via Court.Venue.OwnerId) | - | `BookingResponse` (Pending -> Confirmed) |
| GET | /api/venues/{id}/bookings | Authenticated (venue owner only) | Query: `page=1, pageSize=20` | `PagedResponse<BookingResponse>` (only bookings for the caller's own venue) |

`CreateBookingRequest` has no `userId` or `totalPrice` field - both are derived server-side from
the authenticated caller and `Court.PricePerHour` (research.md authorization checklist).

## Reviews

| Method | Path | Auth | Request | Response |
|---|---|---|---|---|
| GET | /api/venues/{id}/reviews | Authenticated | Query: `page=1, pageSize=20` | `PagedResponse<ReviewResponse>` |
| POST | /api/venues/{id}/reviews | Authenticated | `CreateReviewRequest { rating, comment? }` | `ReviewResponse` (201, or 200 if it replaces the caller's existing review for this venue) |
