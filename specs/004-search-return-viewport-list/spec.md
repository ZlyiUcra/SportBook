# Feature Specification: Return-to-search navigation and viewport-synced venue list

**Feature Branch**: `004-search-return-viewport-list`

**Created**: 2026-07-19

**Status**: Draft

**Input**: User description: "Return-to-search navigation and viewport-synced venue list. Builds on
feature 003 (reference-point radius map). Three parts: (1) Back navigation: a customer who opened a
venue's page from the radius search results can return to the search with one action; the search
restores its previous state - the active reference point (device location from \"near me\" or the
selected city), the sport filter, and the result set - without re-prompting for geolocation and
without the customer re-doing the search. On return the map shows the default full-radius framing
(all in-range venues framed), not the previously zoomed viewport. Works also for browser back and
for customers who landed on a venue page directly. Search state must not survive the browser
session (device coordinates are never persisted to storage). (2) Viewport-synced list: the results
list below the map shows only the venues currently visible in the map viewport, still ordered
nearest-first; zooming out shows more, zooming/panning in shows fewer; the list updates when the
pan/zoom gesture ends, not continuously during it. Initially (after the automatic framing) the
viewport contains all in-range venues, so the list starts as the full in-range set. An empty
viewport (customer panned away from all venues) shows its own empty state, distinct from \"no
venues within 75 km\". The emphasized nearest-venue marker stays the nearest of the whole in-range
set, not of the visible subset. (3) List pagination: the list is paginated at 10 venues per page (a
constant that can be raised later), client-side over the already-loaded in-range set; changing the
viewport resets to page 1; pagination affects only the list - the map always shows all visible
venues (clustered). This feature modifies FR-013 of spec 003: the list now reflects the
viewport-visible subset of the in-range set rather than always the whole set."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Return to my search from a venue page (Priority: P1)

A customer runs a radius search (via "near me" or a selected city), opens one of the found venues
to look at its courts and reviews, and then returns to the search with a single action. The search
looks the way they left it: the same reference point, the same sport filter, and the same venues on
the map and in the list - without being asked for their location again and without re-doing the
search. The map shows the default framing with all in-range venues visible.

**Why this priority**: This is the pain that motivated the feature - today returning from a venue
page lands on an empty search and the customer has to redo everything. Without this, browsing
several candidate venues one after another is tedious enough to abandon.

**Independent Test**: Search by "near me" or a city, open a venue from the results, use the return
action, and verify the search reappears with the same reference point, filter, and venues, with no
geolocation prompt and no manual re-search.

**Acceptance Scenarios**:

1. **Given** a customer found venues via "near me" and opened one of them, **When** they use the
   return action on the venue page, **Then** the search reappears with the same reference point,
   sport filter, and result set, and no location permission prompt is shown.
2. **Given** a customer found venues via a selected city and opened one of them, **When** they
   return, **Then** the search shows the same city-centered results without re-selecting the city.
3. **Given** a customer zoomed and panned the map before opening a venue, **When** they return,
   **Then** the map shows the default full-radius framing (all in-range venues framed), not the
   zoomed viewport they left.
4. **Given** a customer opened a venue from the search, **When** they use the browser's own back
   navigation instead of the in-page action, **Then** the search state is restored the same way.
5. **Given** a customer landed on a venue page directly (a shared link, no prior search), **When**
   they use the return action, **Then** they arrive at the search in its default state (the prompt
   to pick a city or use "near me").
6. **Given** a customer had an active search, **When** they close the browser session and open the
   application again, **Then** the search starts fresh - no previous reference point, filter, or
   coordinates are remembered.

---

### User Story 2 - See in the list exactly what I see on the map (Priority: P2)

A customer zooms and pans the radius map to focus on an area they care about. The results list
below the map always shows exactly the venues currently visible on the map - zooming in narrows
the list to what is on screen, zooming out widens it again - still ordered nearest-first.

**Why this priority**: It makes the map and the list one coherent tool: the map is the filter, the
list is the detail. Without it, the list keeps showing venues the customer has deliberately zoomed
away from, and finding "that pin I am looking at" in the list is guesswork.

**Independent Test**: Run a radius search, zoom into a sub-area, and verify the list contains
exactly the venues visible on the map, nearest-first; zoom back out and verify the full in-range
list returns.

**Acceptance Scenarios**:

1. **Given** a radius search with venues spread across the area, **When** the customer zooms in so
   only some venues remain visible, **Then** the list shows only those visible venues,
   nearest-first.
2. **Given** a zoomed-in view, **When** the customer zooms back out so more venues become visible,
   **Then** the list grows to match what is on screen.
3. **Given** the customer is dragging or zooming, **When** the gesture is still in progress,
   **Then** the list does not flicker through intermediate states - it updates once the gesture
   ends.
4. **Given** the map has just framed the search results automatically, **When** the customer looks
   at the list, **Then** it contains the full in-range set (the initial framing shows all venues).
5. **Given** the customer pans to an area with no venues, **When** the viewport contains none,
   **Then** the list shows an "no venues in view" state, distinct from the "no venues within
   75 km" state.
6. **Given** the nearest venue of the whole in-range set is outside the current viewport, **When**
   the customer looks at the map, **Then** the emphasized marker still belongs to that overall
   nearest venue (emphasis does not jump to the nearest visible one).

---

### User Story 3 - Browse a long list page by page (Priority: P3)

When the visible area contains many venues, the customer browses the list in pages of 10 instead
of scrolling one long column, moving between pages with previous/next controls.

**Why this priority**: A convenience on top of US2 - it only matters when the viewport holds more
than a pageful of venues. The feature is fully usable without it for small result sets.

**Independent Test**: Produce a viewport with more than 10 visible venues and verify the list
shows 10 per page with working previous/next controls; change the viewport and verify the list
returns to the first page.

**Acceptance Scenarios**:

1. **Given** more than 10 venues are visible in the viewport, **When** the customer views the
   list, **Then** it shows the first 10 nearest venues and controls to move to the next page.
2. **Given** the customer is on a later page, **When** they zoom or pan the map, **Then** the list
   resets to the first page of the new visible set.
3. **Given** the customer is on a later page, **When** they change the sport filter, **Then** the
   list resets to the first page.
4. **Given** any list page is shown, **When** the customer looks at the map, **Then** the map
   still shows all visible venues (clustered as needed) - pagination never hides map pins.

---

### Edge Cases

- Customer revokes location permission while on a venue page: on return, the restored reference
  point from the session is still used for display; the customer can re-run "near me" or pick a
  city to change it - no error is shown.
- The restored search's data has changed on the server (a venue was removed): the refreshed
  result set is shown; the restore is about not re-entering the search, not about freezing stale
  data.
- A venue sits exactly on the viewport edge: it counts as visible when its location point is
  within the visible map area.
- Exactly 10 venues visible: a single page, no pagination controls needed.
- Fewer venues visible than the current page would need (e.g. viewport shrinks while on page 3):
  the reset-to-first-page rule covers this - the customer is never stranded on an empty page.
- The customer uses the return action repeatedly (search -> venue -> search -> venue): each return
  restores the same session state; nothing accumulates.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The venue page MUST offer a single, always-visible action that returns the customer
  to the venue search.
- **FR-002**: Returning to the search (via the in-page action or browser back) MUST restore the
  session's search state: the active reference point (device location or selected city) and the
  sport filter, and MUST show the corresponding result set without the customer re-doing the
  search.
- **FR-003**: Restoring the search MUST NOT trigger a location permission prompt.
- **FR-004**: On return, the map MUST show the default full-radius framing (all in-range venues
  framed), not the previously zoomed viewport.
- **FR-005**: If no search state exists in the session (direct landing on a venue page, or a new
  browser session), the return action MUST lead to the search in its default no-reference state.
- **FR-006**: Search state MUST NOT survive the browser session, and device coordinates MUST NOT
  be written to any persistent storage at any time.
- **FR-007**: The results list MUST contain exactly the in-range venues whose location lies within
  the current map viewport, ordered nearest-first. (This supersedes 003's FR-013 whole-set rule:
  the list now reflects the viewport-visible subset of the in-range set.)
- **FR-008**: The list MUST update when a zoom or pan gesture ends, not continuously during the
  gesture.
- **FR-009**: Immediately after the automatic framing of a search, the list MUST contain the full
  in-range set.
- **FR-010**: An empty viewport MUST show a dedicated "no venues in view" state, distinct from the
  "no venues within 75 km" state.
- **FR-011**: The emphasized nearest-venue marker MUST remain the nearest venue of the whole
  in-range set, regardless of the current viewport.
- **FR-012**: The list MUST be paginated at 10 venues per page; the page size is a single fixed
  constant that can be raised later without redesign.
- **FR-013**: Any change of the visible set (viewport change or sport filter change) MUST reset
  the list to its first page.
- **FR-014**: Pagination MUST affect only the list; the map MUST always show all visible in-range
  venues (clustered as needed) regardless of the current list page.

### Key Entities

- **Search state**: The session-scoped memory of the customer's search - the active reference
  point (device location or selected city) and the sport filter. Lives only for the browser
  session; never persisted.
- **Viewport**: The currently visible area of the map, as shaped by automatic framing and the
  customer's manual zoom/pan. Determines which venues the list shows.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Returning from a venue page to the previous search takes exactly one action and zero
  re-entered inputs (no re-picking the city, no re-activating "near me", no re-selecting the
  sport).
- **SC-002**: No location permission prompt appears on any return to a restored search.
- **SC-003**: After any completed zoom or pan, the list matches the viewport-visible venues
  exactly (100% correspondence in validation checks), ordered nearest-first.
- **SC-004**: No list page ever shows more than 10 venues.
- **SC-005**: After the browser session ends, no search input (reference point, coordinates,
  filter) is recoverable from the device's persistent storage.

## Assumptions

- This feature builds on 003's radius search and changes no server behavior: the in-range set
  (fixed 75 km, capped, nearest-first) stays as is; visibility filtering and pagination happen on
  the already-delivered result set.
- 003's FR-013 is explicitly superseded by FR-007 of this spec (list = viewport-visible subset,
  not always the whole in-range set). All other 003 requirements stand.
- The page size (10) and its raisability were confirmed by the user (2026-07-19); raising it is a
  constant change, not a redesign.
- Emphasis staying with the overall nearest venue (not the nearest visible one) was confirmed by
  the user (2026-07-19).
- Returning to the default full-radius framing (rather than restoring the zoomed viewport) was
  confirmed by the user (2026-07-19).
- "Result set restored without re-doing the search" allows a background refresh of the same
  search - the guarantee is about the customer's effort, not about byte-identical data.
