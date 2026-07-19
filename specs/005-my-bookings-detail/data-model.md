# Phase 1 Data Model: My bookings - venue detail, status filter, and pagination

No persistent schema change (no table, column, or migration). This feature widens one transport DTO
and adds one request-time filter value. Entities from 001 are reused unchanged.

## Booking / Court / Venue / City (unchanged, reused)

No change. The feature reads the existing navigation chain to build the response:

- `Booking.Court` -> the booked court
- `Court.Venue` -> the court's venue; `Court.SportType`, `Court.Name`
- `Venue.City` -> the venue's city; `Venue.Name`

Deletion of a venue/court is already blocked while it has an upcoming, non-cancelled booking (001),
so an upcoming booking always resolves its full chain.

## BookingResponse (widened transport DTO)

The existing booking response gains four human-readable labels. All other fields are unchanged from
001.

| Field | Type | Notes |
|---|---|---|
| Id | Guid | unchanged |
| CourtId | Guid | unchanged (still present for actions/links) |
| UserId | Guid | unchanged |
| StartTime | DateTime | unchanged |
| EndTime | DateTime | unchanged |
| Status | string | unchanged - derived Completed still computed on read (001) |
| TotalPrice | decimal | unchanged |
| CreatedAt | DateTime | unchanged |
| VenueName | string | NEW - `Court.Venue.Name` |
| City | CityResponse | NEW - `Court.Venue.City` mapped with the existing `ToResponse` (trilingual) |
| Sport | string | NEW - `Court.SportType.ToString()` |
| CourtName | string | NEW - `Court.Name` |

No new internal-only field is added (no owner id); only display labels (spec FR-011). The DTO stays
the single shape returned by every booking endpoint, so both booking lists render identically.

## BookingStatusFilter (new request-time value)

The customer bookings endpoint's optional `status` query value; default `All`.

| Value | Meaning (predicate over stored status + request time `now`) |
|---|---|
| All | no filter - every booking |
| Upcoming | `Status != Cancelled && EndTime > now` |
| Completed | `Status == Confirmed && EndTime <= now` |
| Cancelled | `Status == Cancelled` |

Applied to the `IQueryable` before `Skip`/`Take`, so it filters the whole history, not a page (spec
FR-006). Translatable to SQL (plain column comparisons). A stale pending-past booking matches only
`All`.

## Consumers

- Customer "My bookings" list: enriched rows + the status filter + Prev/Next paging.
- Owner "Venue bookings" list: enriched rows only (shared shape); no status filter this feature.
- The nested `City` is the same `CityResponse` used across 002-004, so its trilingual name selection
  works with no new client logic.
