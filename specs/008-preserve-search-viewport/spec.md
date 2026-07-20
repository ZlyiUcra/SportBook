# Feature Specification: Preserve map viewport across venue-detail navigation and show a visible-venue count

**Feature Branch**: `008-preserve-search-viewport`

**Created**: 2026-07-20

**Status**: Draft

**Input**: User description: "Preserve the map viewport (zoom/pan) when a customer navigates from
venue search to a venue detail page and returns. Currently the search deliberately resets to the
default full-radius view on return (spec 004 FR-004), which loses the customer's zoomed/panned
position - reverse that. Also show a count of venues currently visible in the map viewport above the
search results, so the customer knows how many venues they are working with at the moment. Filters
(city, sport, reference point) already persist across this navigation and are out of scope. Open
question for plan: how a preserved viewport coexists with the existing auto-framing-on-reference
behavior."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Return to the exact viewport I left (Priority: P1)

A customer runs a radius search, zooms and pans the map to focus on an area they care about, then
opens one of the venues to read its details. When they return to the search, the map is exactly
where they left it - the same zoom and pan position - not the default full-radius framing. The
filters and result set were already restored by spec 004; this restores the geographic focus too,
so the customer resumes exploring from the same spot without re-zooming.

**Why this priority**: This is the reversal that motivated the feature. Today (004 FR-004) the map
snaps back to the full-radius view on return, discarding the zoom/pan the customer just invested.
Re-opening several venues in the same area currently means re-zooming after every return.

**Independent Test**: Run a radius search, zoom into a sub-area and pan sideways, open a venue,
return, and verify the map is at the same zoom/pan position (not the default framing), with the
same filters and results.

**Acceptance Scenarios**:

1. **Given** a customer zoomed and panned the map before opening a venue, **When** they return,
   **Then** the map shows the same zoom and pan position they left, not the default full-radius
   framing.
2. **Given** a customer found venues via "near me" and opened one, **When** they return, **Then**
   the map is at the saved viewport AND the reference point, sport filter, and result set are
   restored (per 004 FR-002) with no location prompt.
3. **Given** a customer opened a venue from the search, **When** they use the browser's back
   navigation instead of the in-page action, **Then** the viewport is restored the same way.
4. **Given** a customer landed on a venue page directly (no prior search), **When** they use the
   return action, **Then** they arrive at the search in its default no-reference state (no viewport
   to restore).
5. **Given** a customer had a saved viewport, **When** they start a genuinely new search by changing
   the reference point (a different selected city, or a fresh "near me"), **Then** the viewport
   resets to the default full-radius framing for that new search.
6. **Given** a customer closes the browser session, **When** they reopen the application, **Then**
   nothing of the previous search - including the viewport - is remembered.

---

### User Story 2 - See how many venues I am looking at right now (Priority: P2)

Above the results list, the customer always sees the count of venues currently visible in the map
viewport - the number they are working with at the moment. As they zoom or pan, the count updates
with the list; when they return to a saved viewport, the count reflects that viewport, not the full
set.

**Why this priority**: The count gives the customer an immediate sense of scale ("am I looking at 4
venues or 40?") before reading the list, and anchors the viewport-synced list that 004 introduced.
It is secondary to US1 because the list itself already works without it; the count is orientation,
not a capability.

**Independent Test**: Run a radius search, zoom in so only some venues remain visible, and verify
the count above the list equals the number of venues visible on the map; zoom out and verify the
count grows with the visible set.

**Acceptance Scenarios**:

1. **Given** a radius search with venues spread across the area, **When** the customer views the
   list, **Then** the count above it equals the number of venues currently visible in the viewport.
2. **Given** a zoomed-in view, **When** the customer zooms or pans so the visible set changes,
   **Then** the count updates to match once the gesture ends (same cadence as the list).
3. **Given** the map just framed a fresh search, **When** the customer views the list, **Then** the
   count equals the full in-range set (the initial framing shows all venues).
4. **Given** the customer panned to an area with no venues, **When** the viewport contains none,
   **Then** the count reads zero and the dedicated "no venues in view" state (004 FR-010) applies.
5. **Given** a customer returns to a saved viewport (US1), **When** they view the count, **Then** it
   reflects the saved viewport's visible set, not the full in-range set.

---

### Edge Cases

- The saved viewport's data has changed on the server (a venue in that area was removed): the
  refreshed result set is shown within the saved viewport; restore is about the customer's viewport,
  not freezing stale data (mirrors 004's existing edge case).
- A venue sits exactly on the viewport edge: it counts as visible per 004's existing rule, so it is
  included in the count.
- The customer changes only the sport filter (same reference point): the result set updates within
  the current viewport (the viewport itself is not reset); the count follows whatever of the new set
  falls inside the viewport.
- Count locale and plural forms: the count label follows each UI locale's plural rules (e.g.,
  English "1 venue" / "5 venues"; other locales follow their own forms).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: When a customer navigates from the venue search to a venue detail page and returns
  (via the in-page action or browser back), the map MUST restore the previously zoomed/panned
  viewport - the same zoom level and pan position the customer left - rather than the default
  full-radius framing. (Supersedes 004 FR-004 and 004 US1 Acceptance Scenario 3.)
- **FR-002**: The viewport MUST still reset to the default full-radius framing when the reference
  point changes (a different selected city, or a fresh "near me") - consistent with 004's existing
  reset-on-reference behavior. Changing only the sport filter does NOT reset the viewport; the new
  result set is shown within the current viewport.
- **FR-003**: Restoring the viewport MUST NOT trigger a location permission prompt on return
  (consistent with 004 FR-003).
- **FR-004**: If no search state exists in the session (direct landing on a venue page, or a new
  browser session), the return action MUST lead to the search in its default no-reference state,
  with no viewport to restore (consistent with 004 FR-005).
- **FR-005**: The viewport - like the rest of the search state per 004 FR-006 - MUST NOT survive the
  browser session, and device coordinates MUST NOT be written to persistent storage.
- **FR-006**: Above the results list, the search MUST display the count of venues currently visible
  in the map viewport.
- **FR-007**: The count MUST update on the same cadence as the list - when a zoom or pan gesture
  ends and when the visible set changes (004 FR-008), not continuously during a gesture.
- **FR-008**: The count MUST equal the number of in-range venues whose location lies within the
  current viewport - the same set the list shows per 004 FR-007 - including zero when the viewport
  is empty.
- **FR-009**: The count label MUST follow each UI locale's pluralization rules.

### Key Entities

- **Viewport**: The currently visible area of the map, as shaped by automatic framing and the
  customer's manual zoom/pan. With this feature it is part of the session's search state across
  venue-detail navigation (in 004 it was discarded on return), while still resetting when the
  reference point changes. Determines which venues the list and the count show.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: After returning from a venue detail page, the map's zoom and pan match the viewport
  the customer left (100% correspondence in validation checks), not the default framing.
- **SC-002**: The count above the list always equals the number of venues visible in the viewport
  (100% correspondence after any completed gesture).
- **SC-003**: Changing the reference point visibly resets the map to the default full-radius
  framing, every time.
- **SC-004**: After the browser session ends, neither the viewport nor any other search input is
  recoverable from the device's persistent storage.

## Assumptions

- This feature amends spec 004 (Return-to-search navigation and viewport-synced venue list). It
  supersedes ONLY 004 FR-004 and 004 US1 Acceptance Scenario 3 (return now restores the saved
  viewport instead of the default framing). All other 004 requirements stand, including: the
  session restore of reference point + sport filter + result set (004 FR-002), no prompt on return
  (FR-003), the viewport-synced list (FR-007), nearest-of-whole-set emphasis (FR-011), pagination
  (FR-012/013/014), and the empty-viewport state (FR-010).
- The earlier 004 decision to reset to the full-radius view on return was confirmed by the user on
  2026-07-19; the user reverses that on 2026-07-20, preferring to preserve the customer's zoom/pan
  across the venue-detail detour.
- "The viewport resets when the reference point changes" matches 004's existing reset-on-reference
  behavior; sport-filter changes keep the viewport (the result set updates within it). This split
  was not explicitly confirmed by the user but preserves 004's current behavior and avoids changing
  more than the asked reversal.
- The count placement (above the results list) was indicated by the user; its exact placement
  relative to the map and its styling are plan-level details.
- The count reflects the same visible set the list already shows (004 FR-007); no new server
  request is needed.
- 004's automatic full-radius framing still applies on a genuinely new search (new reference point);
  on return from a venue page the saved viewport is restored instead. How the preserved viewport
  coexists with the map's auto-framing is an implementation question for /plan.
