# Feature Specification: My bookings - venue detail, status filter, and pagination

**Feature Branch**: `005-my-bookings-detail`

**Created**: 2026-07-19

**Status**: Draft

**Input**: User description: "Enrich the customer \"My bookings\" page with status filters, venue
and sport detail, and pagination. Today each booking row shows only the time, status, and price -
it carries just a court id, so the customer cannot tell which venue, city, sport, or court a
booking is for, cannot filter by status, and the page ignores the pagination the API already
returns (it always shows the first page). Three parts: booking detail (venue name, city, sport,
court), status filter (All / Upcoming / Completed / Cancelled), and pagination (Previous/Next,
resets to page 1 on filter change). The booking contract from feature 001 exposes only the court
id and must be extended; this affects both the customer \"My bookings\" list and the owner \"Venue
bookings\" list, since both render the same booking shape. Cancellation behavior is unchanged - a
cancelled booking stays in the list with the Cancelled status, by design."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - See what each booking is for (Priority: P1)

A customer opens "My bookings" and, for every booking, sees which venue it is at, in which city,
what sport the court is for, and the court's name - in addition to the date/time, status, and
price already shown. They no longer have to remember or guess which court a booking refers to.

**Why this priority**: This is the core gap - a list of bookings with no venue, city, or sport is
close to unusable for anyone with more than one booking. It delivers value on its own even without
filtering or pagination.

**Independent Test**: Make a booking, open "My bookings", and verify the row shows the venue name,
its city, the sport, and the court name together with the time, status, and price.

**Acceptance Scenarios**:

1. **Given** a customer has a booking at a venue in a city on a court of a given sport, **When**
   they view "My bookings", **Then** the booking shows the venue name, the city, the sport, and the
   court name alongside the time, status, and price.
2. **Given** a venue owner viewing their venue's bookings, **When** they view the "Venue bookings"
   list, **Then** each booking shows the same venue/city/sport/court detail (both lists render the
   same booking shape).
3. **Given** a booking whose venue or court detail is available, **When** the booking is shown,
   **Then** no raw internal identifier (such as a court id) is displayed to the customer in place
   of a human-readable name.

---

### User Story 2 - Filter bookings by status (Priority: P2)

A customer filters their bookings to just the ones they care about: All, Upcoming (still to come),
Completed (already happened), or Cancelled. The default view is All.

**Why this priority**: A customer with a history of bookings wants to focus on what is upcoming, or
review what is done, without scrolling past cancelled or completed ones. It builds on the enriched
list but is independently valuable.

**Independent Test**: With bookings across the different statuses, select each filter and verify
only the matching bookings appear.

**Acceptance Scenarios**:

1. **Given** a customer has upcoming, completed, and cancelled bookings, **When** they select
   "Upcoming", **Then** only bookings that are still to come (not cancelled, end time in the
   future) are shown.
2. **Given** the same, **When** they select "Completed", **Then** only bookings that already
   finished (confirmed and past) are shown.
3. **Given** the same, **When** they select "Cancelled", **Then** only cancelled bookings are
   shown.
4. **Given** the same, **When** they select "All" (the default), **Then** every booking is shown
   regardless of status.
5. **Given** a customer has many bookings spanning several pages, **When** a filter is applied,
   **Then** the filter applies across the whole set (not only the currently visible page).

---

### User Story 3 - Page through a long booking history (Priority: P3)

A customer with many bookings moves through them a page at a time using Previous/Next controls,
rather than seeing only the first page or one endless list.

**Why this priority**: A convenience that only matters once a customer has more than one page of
bookings; the page is fully usable without it for small histories.

**Independent Test**: With more bookings than fit on one page, verify Previous/Next move between
pages and that changing the filter returns to the first page.

**Acceptance Scenarios**:

1. **Given** a customer has more bookings than one page holds, **When** they view "My bookings",
   **Then** they see the first page and controls to move to the next page.
2. **Given** a customer is on a later page, **When** they change the status filter, **Then** the
   list returns to the first page of the filtered set.
3. **Given** a customer is on the first page, **When** there is no previous page, **Then** the
   "Previous" control is unavailable; likewise "Next" is unavailable on the last page.

---

### Edge Cases

- A booking's venue or court was deleted after the booking was made: deletion is blocked while a
  venue/court still has an upcoming, non-cancelled booking (feature 001), so an upcoming booking
  always has its venue/court detail; this feature does not need a "venue removed" placeholder for
  upcoming bookings.
- A confirmed booking whose end time passes moves from "Upcoming" to "Completed" purely by the
  passage of time, with no action by anyone - the filters reflect that automatically.
- A pending booking whose end time has passed (never confirmed) is neither "Upcoming" nor
  "Completed" nor "Cancelled" - it is a stale pending booking; it appears only under "All".
- Changing the filter to one with no matching bookings shows an appropriate empty state, distinct
  from "you have no bookings at all".
- The last booking on a page is cancelled or filtered away such that a page would be empty: paging
  never strands the customer on an empty page (the filter-change reset to page one covers the
  common case).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Each booking shown to a customer MUST display the venue name, the venue's city, the
  court's sport, and the court name, in addition to the existing date/time, status, and price.
- **FR-002**: The booking data returned by the system MUST carry the venue name, city, sport, and
  court name for each booking (extending the feature 001 booking contract, which exposed only the
  court identifier).
- **FR-003**: The same enriched booking detail MUST appear in both the customer "My bookings" list
  and the owner "Venue bookings" list, since both present the same booking shape.
- **FR-004**: A customer MUST be able to filter their bookings by All, Upcoming, Completed, or
  Cancelled, with All as the default.
- **FR-005**: "Upcoming" MUST include bookings that are not cancelled and whose end time is in the
  future; "Completed" MUST include bookings that are confirmed and whose end time has passed;
  "Cancelled" MUST include cancelled bookings.
- **FR-006**: The status filter MUST be applied across the customer's whole set of bookings, not
  only the currently displayed page.
- **FR-007**: The booking list MUST be paginated, with controls to move to the previous and next
  pages, and the previous/next controls MUST be unavailable when there is no previous/next page.
- **FR-008**: Changing the status filter MUST return the list to the first page.
- **FR-009**: A filter that matches no bookings MUST show an empty state distinct from the "no
  bookings at all" state.
- **FR-010**: Cancellation behavior MUST remain unchanged - a cancelled booking stays in the list
  as a record with the Cancelled status, and cancelling is still allowed only while a booking is
  cancellable per feature 001's cutoff rule.
- **FR-011**: No raw internal identifier MUST be shown to the customer in place of a human-readable
  venue, city, sport, or court name.

### Key Entities

- **Booking**: Existing entity; the feature reads its associated court, that court's venue and
  sport, and the venue's city to present each booking. The booking itself is unchanged; only what
  is returned about it is enriched.
- **Court / Venue / City**: Existing entities already linked to a booking (booking -> court ->
  venue -> city; sport is on the court). This feature surfaces their names/labels on the booking,
  it does not change them.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: For 100% of listed bookings, the customer can identify the venue, city, sport, and
  court without opening anything else.
- **SC-002**: A customer can narrow their bookings to a single status group in one action, and the
  result contains only bookings of that group across their entire history.
- **SC-003**: No booking row shows a raw internal identifier in place of a name.
- **SC-004**: A customer with more bookings than one page can reach any of their bookings using the
  paging controls.
- **SC-005**: Selecting a filter always shows the first page of that filtered set.

## Assumptions

- The server already paginates booking lists and accepts a page parameter (feature 001); this
  feature surfaces that paging in the UI and adds a status filter to the same server-side list, so
  filtering and paging stay consistent.
- The four filter groups (All / Upcoming / Completed / Cancelled) and showing venue + city + sport
  + court per booking were confirmed by the user (2026-07-19).
- "Completed" is a derived view of a stored Confirmed booking whose end time has passed (feature
  001's read-time derivation), not a stored status; the filter maps each choice to the correct
  combination of stored status and time so it is computed consistently on the server.
- The page size is the existing server-side booking page size; no new page-size control is
  introduced by this feature.
- Map, geolocation, and venue-search features (002-004) are unrelated and unchanged.
