# Phase 0 Research: My bookings - venue detail, status filter, and pagination

All product choices were made by the user in the 2026-07-19 discussion (spec Assumptions). This
file pins the mechanisms. No NEEDS CLARIFICATION markers remain.

## Enriching the booking response: one widened shared DTO + an Include chain

- **Decision**: `BookingResponse` gains `VenueName` (string), `City` (the existing `CityResponse`),
  `Sport` (string, the court's `SportType.ToString()`), and `CourtName` (string). Every path that
  materializes a booking for a response loads `Booking -> Court -> Venue -> City` via
  `Include(b => b.Court).ThenInclude(c => c.Venue).ThenInclude(v => v.City)`; `Mapping.ToResponse`
  reads the loaded chain.
- **Rationale**: One shared DTO means both the customer `ListMineAsync` and the owner
  `ListByVenueForOwnerAsync` gain the detail from a single change (spec FR-003), and the row
  component is written once. `CityResponse` already exists and is trilingual, so the city is
  reused, not reinvented. The join is bounded (one court, one venue, one city per booking) and the
  lists are already paged, so cost stays flat in the page size.
- **Single-booking paths** (`CreateAsync`, `CancelAsync`, `GetByIdAsync`) return the same DTO, so
  they must also populate the chain: Create already loads the court - it loads it with
  `Include(c => c.Venue).ThenInclude(v => v.City)` and assigns `booking.Court = court` in memory so
  the new booking maps fully without an extra round-trip; Cancel/GetById add the Include chain to
  their booking load.
- **Alternatives considered**: A separate "detailed booking" DTO only for the lists (rejected - two
  shapes for the same entity, and the row component would diverge); a second query per booking to
  fetch venue/court names (rejected - N+1); exposing raw ids and resolving names on the client
  (rejected - the client has no bulk court->venue->city lookup, and spec FR-011 forbids showing raw
  ids).

## Status filter: a translatable predicate applied before paging

- **Decision**: The customer bookings endpoint takes an optional `status` of `All` (default),
  `Upcoming`, `Completed`, or `Cancelled`. In `ListMineAsync` each maps to a `WHERE` predicate
  applied to the `IQueryable` before `Skip`/`Take`, using the request time `now`:
  - `Upcoming`: `Status != Cancelled && EndTime > now` (a pending or confirmed booking still to
    come).
  - `Completed`: `Status == Confirmed && EndTime <= now`.
  - `Cancelled`: `Status == Cancelled`.
  - `All`: no predicate.
- **Rationale**: "Completed" is not stored - 001 derives it at read time (Confirmed + past end).
  Encoding each choice as a stored-status + time predicate keeps the derivation server-side and
  consistent, and applying it before `Skip`/`Take` makes the filter act on the whole history, not
  just the visible page (spec FR-006). All three predicates are plain column comparisons, so they
  translate to SQL (asserted with `ToQueryString()`); no client evaluation, no full-history
  materialization.
- **Modeling the choice**: a small `BookingStatusFilter` enum in the Application layer
  (All/Upcoming/Completed/Cancelled), bound from the query string like the existing `SportType?`
  pattern on `GET /api/venues`. A stale pending-past booking (Pending + EndTime <= now) matches
  none of Upcoming/Completed/Cancelled and so appears only under All (spec edge case).
- **Alternatives considered**: filtering the materialized page on the client (rejected - wrong
  across pages, spec FR-006); adding a stored `Completed` status / a background job to transition
  bookings (rejected - 001 deliberately derives it on read; storing it invites drift); a free-form
  status string equal to the raw enum (rejected - would expose Pending/Confirmed separately and
  could not express the derived Upcoming/Completed groups the user chose).

## Pagination: surface the existing server paging

- **Decision**: The customer page reads the `PagedResponse` the endpoint already returns and adds
  Previous/Next controls (the same pattern the venue search and 002 list used), passing `page` to
  the API. Changing the status filter resets `page` to 1. Controls disable at the first/last page.
- **Rationale**: The list is already paged server-side (`ListMineAsync` + `PageRequest`); the UI
  just never surfaced it. Server paging (not client) is correct here because the filter is
  server-side too - both live on one query, so the page always reflects the filtered set. Page size
  stays the server default; the user asked for paging, not a page-size control.
- **Alternatives considered**: client-side paging over the whole history (rejected - would require
  fetching the entire history to page it, and would not compose with the server-side filter);
  infinite scroll (rejected - not requested; Prev/Next already exists as a pattern here).

## Scope of the owner "Venue bookings" page

- **Decision**: The owner page gains the enriched row detail (venue/city/sport/court) for free from
  the shared DTO. It does NOT gain the status-filter tabs in this feature - the filter is a customer
  journey (spec US2), and the owner list is scoped to one venue already.
- **Rationale**: FR-003 requires the detail on both lists; the filter requirement (FR-004) is about
  the customer's own bookings. Keeping the owner page's filter/paging behavior as-is limits scope to
  what was asked. The owner endpoint still gains the Include chain (for detail) but no `status`
  parameter.
- **Alternatives considered**: adding the same filter to the owner page (rejected - not in the
  spec; would broaden scope without a stated need).
