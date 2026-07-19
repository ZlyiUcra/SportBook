# API Contracts: My bookings - venue detail, status filter, and pagination

Delta contract on top of `specs/001-sportbook-venue-booking/contracts/api.md`. Everything not
listed here is unchanged. Auth posture (JWT required everywhere) and the standard error shape carry
over.

## Booking response - widened (affects every booking endpoint)

`BookingResponse` gains four human-readable fields; all 001 fields remain:

```
BookingResponse {
  id, courtId, userId, startTime, endTime, status, totalPrice, createdAt,   // 001, unchanged
  venueName,        // NEW - the booked court's venue name
  city,             // NEW - CityResponse (same shape/trilingual as 002-004)
  sport,            // NEW - the court's sport type
  courtName         // NEW - the court's name
}
```

- Returned by ALL booking endpoints that already return a `BookingResponse` (create, cancel,
  get-by-id, list-mine, list-by-venue) - one shared shape.
- MUST NOT add any internal-only field (no owner id); only the four display labels above (spec
  FR-011). `status` still carries the read-time-derived `Completed` for a confirmed, past booking
  (001 behavior unchanged).

## GET /api/bookings (customer's own) - + status filter, existing paging surfaced

| Method | Path | Auth | Query | Response |
|---|---|---|---|---|
| GET | /api/bookings | Authenticated | `page?`, `status?` = All \| Upcoming \| Completed \| Cancelled (default All) | `PagedResponse<BookingResponse>` |

- `status` filters the caller's whole booking set BEFORE paging (spec FR-006), mapped as:
  Upcoming = not cancelled and `endTime` in the future; Completed = confirmed and `endTime` in the
  past; Cancelled = cancelled; All = no filter. The filter is enforced server-side; a client cannot
  receive bookings outside the selected group.
- `page` is the existing paging parameter (001); the response is the existing `PagedResponse`
  (items + page + pageSize + totalCount). Page size is unchanged and not client-configurable.
- Ordering is unchanged from 001 (most recent start first).

## GET /api/venues/{venueId}/bookings (owner) - detail only

- Still `PagedResponse<BookingResponse>`, now carrying the widened response (venue/city/sport/court
  detail). NO `status` filter parameter is added here this feature - the status filter is a customer
  concern (spec US2); the owner list is already scoped to one venue.

## Superseded / unchanged

- No 001 endpoint is removed. The booking-overlap, pricing, confirmation, and cancellation-cutoff
  rules are unchanged. This feature only widens the response shape and adds the `status` query
  value on the customer bookings list.
