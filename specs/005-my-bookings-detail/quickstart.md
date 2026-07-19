# Quickstart: My bookings - venue detail, status filter, and pagination

Validation guide for proving the feature works end-to-end. Prerequisites, setup, and run commands
are unchanged from `specs/001-sportbook-venue-booking/quickstart.md`; no dependency install and no
database change. Seed a customer with several bookings across statuses (some upcoming, some
confirmed-and-past, some cancelled) at venues in different cities and sports.

## API validation scenarios

Run authenticated (all booking endpoints require auth).

1. **Enriched fields**: `GET /api/bookings` - each item carries `venueName`, `city`, `sport`, and
   `courtName` in addition to the 001 fields; no raw court id is the only venue reference.
2. **Filter - Upcoming**: `GET /api/bookings?status=Upcoming` - only non-cancelled bookings whose
   `endTime` is in the future; a cancelled or already-finished booking never appears.
3. **Filter - Completed**: `GET /api/bookings?status=Completed` - only confirmed bookings whose
   `endTime` has passed; a pending-past or cancelled booking never appears.
4. **Filter - Cancelled**: `GET /api/bookings?status=Cancelled` - only cancelled bookings.
5. **Filter across pages**: with more matching bookings than one page, confirm the filter applies to
   the whole set (page 2 of `status=Cancelled` still holds only cancelled bookings), not just page 1.
6. **Default**: `GET /api/bookings` with no `status` behaves as `All`.
7. **Owner list detail**: `GET /api/venues/{venueId}/bookings` items carry the same
   venue/city/sport/court detail; the endpoint ignores any `status` query value (no filter there).

## Frontend validation scenarios (manual, via `yarn dev`)

1. **Detail (US1)**: open "My bookings" - each row shows the venue name, city, sport, and court name
   next to the date/time, status, and price. Open "Venue bookings" as an owner - the same detail
   shows there.
2. **Filter (US2)**: switch between All / Upcoming / Completed / Cancelled - the list shows only the
   matching bookings each time; All (default) shows everything. A filter with no matches shows a
   "no bookings in this filter" message distinct from the "you have no bookings" empty state.
3. **Pagination (US3)**: with more bookings than one page, Previous/Next move between pages;
   Previous is disabled on page 1 and Next on the last page. Change the filter while on a later page
   and confirm the list returns to page 1.
4. **Cancellation unchanged**: cancel a cancellable booking - it stays in the list with the
   Cancelled status and appears under the Cancelled filter.

## Automated tests

```powershell
# Backend (from backend/)
dotnet test

# Frontend (from frontend/)
yarn test
```

Must include: a unit test for the status-filter predicate (each choice maps to the right
stored-status/time combination) and a `ToQueryString()` guard that the filter + the court->venue->
city Include translate to SQL (no client evaluation); integration tests for the enriched fields and
the filter's cross-page correctness; frontend tests for the filter tabs, the pagination controls
and their reset-on-filter, and the rendered venue/city/sport/court detail.

## Non-regression

- 001 booking flows still pass (create, cancel with 2-hour cutoff, overlap safety, owner confirm).
- `dotnet test` and `yarn test` are green; `yarn build` initial-chunk size is unchanged (no new
  dependency).
