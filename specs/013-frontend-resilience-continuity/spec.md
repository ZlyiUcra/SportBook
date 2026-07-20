# Feature Specification: Frontend resilience and continuity polish

**Feature Branch**: `013-frontend-resilience-continuity`

**Created**: 2026-07-21

**Status**: Draft

**Input**: User description: "Frontend resilience and continuity polish: an error boundary, a
universal page loader, and persisted venue-search state. Already implemented and verified. Three
related pieces: (1) The React app had no error boundary anywhere - an unhandled render error in
any page produced a blank white screen with no fallback; now a top-level error boundary wraps the
whole app and shows a "something went wrong" message with a reload action. (2) Loading states were
inconsistent small inline text ("Loading...") scattered across pages and Suspense fallbacks;
replaced everywhere with one universal centered page loader - a full-viewport overlay with a
blurred/dimmed backdrop (matching the existing modal dialogs' visual treatment) and a spinner, so
every page's data-loading moment looks the same. (3) The venue search page's search state
(selected city, sport filter, map viewport, and results list page number) previously lived in
memory only and was wiped by any page reload, showing an empty "pick a reference point" prompt
even for a user who had just been actively searching; it is now persisted to localStorage so a
reload restores the same search instead of starting over. The one exception is raw device GPS
coordinates from the "near me" button, which stay strictly in-memory only, preserving the original
privacy-motivated constraint that raw location data must never reach persistent storage - only the
derived, less sensitive state (a chosen city, a map camera position, a filter, a page number) is
now persisted. No backend changes."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A page-rendering error never shows a blank screen (Priority: P1)

A user encounters an unexpected rendering error somewhere in the app (a bug, unexpected data
shape, or similar). Previously this produced a blank white page with no explanation and no way
forward short of guessing to reload manually.

**Why this priority**: A blank white screen is the worst possible failure mode - the user has no
information and no offered recovery. This is the single highest-value, lowest-effort fix in this
feature.

**Independent Test**: Force a rendering error anywhere in the app; confirm a visible message and a
working recovery action appear instead of a blank page.

**Acceptance Scenarios**:

1. **Given** a rendering error occurs anywhere in the app, **When** the user is looking at the
   page, **Then** they see a clear "something went wrong" message instead of a blank screen.
2. **Given** the error message is showing, **When** the user chooses to recover, **Then** the app
   returns to a normal, working state.

---

### User Story 2 - Every page's loading moment looks and behaves the same (Priority: P2)

A user waiting for data to load on any page previously saw small, inconsistent inline "Loading..."
text that varied by page and was easy to miss, especially when it appeared inline among other
content rather than clearly indicating that the whole view was not yet ready.

**Why this priority**: A real but smaller consistency/clarity improvement than Story 1 - it makes
loading moments easier to notice and recognize, but nothing was broken beforehand, only
inconsistent.

**Independent Test**: Trigger a data-loading moment on several different pages; confirm each shows
the same visually distinct, centered loading indicator rather than page-specific inline text.

**Acceptance Scenarios**:

1. **Given** any page is waiting on data before it can show its content, **When** the user looks
   at the screen, **Then** they see the same centered loading indicator every time, regardless of
   which page they are on.
2. **Given** the loading indicator is showing, **When** the data finishes loading, **Then** it is
   replaced by the page's real content without leaving any loading artifact behind.

---

### User Story 3 - Reloading the venue search page returns to the same search (Priority: P2)

A user actively searching for venues (a chosen city, a sport filter, a specific map position, a
specific page of results) reloads the page - intentionally or not (for example, the browser
reloads it on its own). Previously this discarded the entire search and returned to an empty
prompt to pick a starting point again, even though the user had just been actively engaged with a
specific search.

**Why this priority**: Meaningful continuity improvement, same priority tier as Story 2 - a real
recurring annoyance, but the user could always redo the search from scratch, so it is not as
severe as Story 1's dead end.

**Independent Test**: Search for venues (pick a city, apply a sport filter, pan/zoom the map,
navigate to a later results page), reload the page, and confirm the same city, filter, map
position, and results page are restored rather than an empty starting prompt.

**Acceptance Scenarios**:

1. **Given** a user has an active venue search (city, optional sport filter, map position, and
   results page), **When** they reload the page, **Then** all of that is restored and results
   appear without the user having to pick a starting point again.
2. **Given** a user has previously used the "use my current location" option, **When** they
   reload the page, **Then** their raw device location is NOT silently reused - it stays specific
   to that browsing session, unlike the rest of the restored search state.
3. **Given** a restored results page number no longer has any results at that position (for
   example, fewer results now than before), **When** the page reloads, **Then** the user is shown
   the nearest valid page of results rather than an empty list.
4. **Given** a user on a later results page opens a venue's detail page and then returns to
   search (without a page reload), **When** the search page reappears, **Then** they are still on
   that same later results page, not bounced back to the first page.

---

### Edge Cases

- What happens if restored search state (city, filter) no longer corresponds to a real, current
  answer (for example, a city that no longer exists)? Out of scope for this feature - the search
  simply produces whatever result an equivalent fresh search with that same input would produce,
  same as if the user had typed/selected it themselves.
- What happens if an error occurs while the error-boundary fallback itself is showing? Out of
  scope - the fallback is deliberately minimal (a message and one recovery action) specifically to
  minimize its own chance of failing.
- What happens when the map reports its position immediately after appearing (which it always
  does, once, whether that position is newly discovered or simply confirming an already-restored
  one)? Only a position that differs from what was already restored counts as a real change; the
  map settling at a position it was already told to show does not reset the results page, whether
  that restoration happened via a page reload or via returning from a venue's detail page.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST show a clear, user-facing message instead of a blank screen when an
  unexpected rendering error occurs anywhere in the app.
- **FR-002**: The error message MUST offer the user a way to recover without needing technical
  knowledge (for example, a single action that returns the app to a working state).
- **FR-003**: Every page's data-loading moment MUST use the same single, visually distinct,
  centered loading indicator, replacing the previous page-by-page inline loading text.
- **FR-004**: The venue search page's search state (selected city, sport filter, map position,
  results page number) MUST survive a page reload.
- **FR-005**: Raw device location data (obtained via an explicit "use my current location" action)
  MUST NOT be included in what survives a page reload - it remains specific to the browsing
  session in which it was captured, preserving the app's existing rule that raw location data
  never reaches persistent storage.
- **FR-006**: If a restored results page number no longer corresponds to any actual results, the
  system MUST show the nearest valid page instead of an empty result list.
- **FR-007**: A results page number restored via returning from a venue's detail page (not only
  via a page reload) MUST be preserved, not reset to the first page, by the map's own initial
  position report confirming an already-restored position.

### Key Entities

- **Error fallback**: The message and recovery action shown in place of the app's normal content
  when a rendering error occurs.
- **Page loading indicator**: The single, consistent visual signal shown while any page is waiting
  on data before it can display its real content.
- **Restorable search state**: The subset of the venue search page's inputs (city, sport filter,
  map position, results page) that survives a page reload - explicitly excluding raw device
  location data.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of unhandled rendering errors show a recovery message instead of a blank
  screen - zero blank-screen failures.
- **SC-002**: Every page's loading moment is visually identical to every other page's - a user
  cannot tell which page they are on from the loading indicator alone.
- **SC-003**: A user reloading the venue search page mid-search sees their prior city, filter, map
  position, and results page restored in 100% of cases, without needing to repeat any input.
- **SC-004**: A user's raw device location is never present in the app's persisted storage at any
  point - verifiable by inspecting stored data directly.

## Assumptions

- This specification documents work already implemented and verified before this spec was
  written - it captures the agreed intent and observed outcome, the same documentation-after-the-
  fact pattern used for specs 006-012.
- "Reload" throughout this spec means any full reload of the page - a manual browser refresh, a
  closed-and-reopened tab (within the persisted-session window established by spec 012), or a
  reload triggered by the environment itself (for example, during active development). The
  results-page number specifically is also restorable via a second, pre-existing mechanism -
  returning from a venue's detail page without any reload at all (specs 004/008's return-to-search
  behavior) - since that mechanism already restored the other search inputs before this feature
  added the results page number to what it covers.
- No backend change is required or included - all three pieces are frontend-only.
