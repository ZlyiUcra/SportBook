# Feature Specification: Geolocation-centered radius map of nearby venues

**Feature Branch**: `003-venue-radius-map`

**Created**: 2026-07-19

**Status**: Draft

**Input**: User description: "Опис фічі - в артефакті консиліуму .specify/consilium/2026-07-19-venue-radius-map.md: розділ \"Узгоджена пропозиція\" (WHAT/WHY), межі й обмеження - у відповідних розділах артефакту. Відкриті питання для clarify також перелічені там."

## Clarifications

### Session 2026-07-19

- Q: Does the radius map replace the current results map, or is it a separate view? → A: It
  replaces it and the search experience unifies around the reference point - the map shows the
  in-range venues, and the textual results list shows the SAME in-range venue set, ideally ordered
  by distance from the reference point (nearest first). The distance ordering is a nice-to-have,
  not a hard requirement.
- Q: Marker density - clustering, or just natural marker spread? → A: Clustering - venues that are
  close together group into a single count marker that expands into individual markers as the
  customer zooms in (adds a small map-clustering capability).
- Q: How does the map obtain the customer's device location? → A: Via an explicit "near me" action
  (a control the customer activates); the map never silently prompts for location on page or map
  load. If permission was already granted, it may center automatically.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - See venues near me on a map (Priority: P1)

A customer opens the venue search experience and activates the "near me" action to share their
location; they then see a map centered on where they are, showing the venues within 75 km that
they could travel to. The map is framed so all of those venues are visible at once without
panning, venues that are close together are grouped into count markers that expand as the
customer zooms in, the venue closest to them stands out with a larger marker, and they can freely
zoom and pan to explore. The results list below shows the same in-range venues, nearest first.

**Why this priority**: This is the whole point of the feature - "where can I go nearby to play".
It delivers value on its own: even with nothing else, a customer can open the map and see
reachable venues around them.

**Independent Test**: Activate "near me" to grant location, and verify the map centers on the
device position, shows exactly the venues within 75 km that have a precise location (and none
beyond), frames them all on screen, marks the nearest one distinctly, groups close venues into
clusters that expand on zoom, and shows the same venues in the results list nearest-first; then
zoom/pan and verify the view is not snapped back.

**Acceptance Scenarios**:

1. **Given** a customer has activated "near me" and venues with precise locations exist within
   75 km, **When** the map appears, **Then** it is centered on their position and shows the
   in-range venues, framed so all are visible.
2. **Given** the map is showing in-range venues, **When** the customer looks at the markers,
   **Then** the venue nearest their position is visually emphasized (larger/distinct) relative to
   the others.
3. **Given** the map has framed the venues, **When** the customer zooms or pans manually,
   **Then** the map stays where they moved it and is not automatically re-framed by unrelated
   screen updates.
4. **Given** a venue lies beyond 75 km from the customer, **When** the map is shown, **Then**
   that venue does not appear on it and is not in the results list.
5. **Given** several in-range venues sit close together, **When** the map is at its default
   framing, **Then** they are shown as a grouped count marker that separates into individual
   markers as the customer zooms in.
6. **Given** in-range venues are shown, **When** the customer reads the results list below the
   map, **Then** it lists the same in-range venues, ordered nearest-first.

---

### User Story 2 - See venues near a chosen city without sharing my location (Priority: P2)

A customer who does not want to (or cannot) share their device location instead picks a city from
the directory, and the same nearby-venues map appears centered on that city, showing venues
within 75 km of it, with the results list showing the same in-range venues.

**Why this priority**: Preserves the feature for customers who decline geolocation or use an
unsupported/insecure context, without forcing location sharing. It builds on P1 but is
independently valuable.

**Independent Test**: Without granting location, select a directory city and verify the map
centers on that city and shows venues within 75 km of it with the same framing and
nearest-emphasis behavior as P1.

**Acceptance Scenarios**:

1. **Given** a customer has not granted location but has selected a directory city, **When** the
   map is shown, **Then** it is centered on the selected city and shows venues within 75 km of
   it.
2. **Given** a customer has granted location, **When** the map resolves its center, **Then** the
   device location takes precedence over any selected city.

---

### User Story 3 - No misleading map when there is nothing to center on (Priority: P3)

A customer who has neither granted location nor selected a city sees no map at all - not an empty
map, not a placeholder - so they are never shown an empty or "no venues found" map next to
results that actually exist elsewhere.

**Why this priority**: Directly removes the current confusion (device location resolving to a
hyper-local sub-locality and producing an empty result beside real venues). It is a guard
condition on top of P1/P2 rather than a standalone journey, hence lowest priority, but it is what
makes the feature trustworthy.

**Independent Test**: Deny location and select no city, then verify no map area, frame, or
placeholder is rendered anywhere on the search experience.

**Acceptance Scenarios**:

1. **Given** a customer has not granted location and has not selected a city, **When** they view
   the search experience, **Then** no map block is rendered at all.
2. **Given** a customer had a map showing (via location or city) and then clears the reference
   (revokes/loses location and deselects the city), **When** the view updates, **Then** the map
   block is removed rather than shown empty.

---

### Edge Cases

- Reference point exists (location or city) but no venue with a precise location is within 75 km:
  the map is shown centered on the reference with an explicit "no venues within 75 km" empty
  state - this is distinct from the no-reference case, where no map appears at all.
- All in-range venues are clustered very close together: framing must not zoom in so far that the
  surrounding area is unreadable - a sensible maximum zoom applies.
- A venue sits exactly at 75 km: it is included (radius is inclusive).
- The nearest venue is inside a cluster at the default framing: the "nearest" emphasis applies
  when that venue is shown as an individual marker (after the customer zooms in enough to separate
  it from its cluster); the results list still marks it as nearest regardless of clustering.
- The customer changes the reference (picks a different city, or location becomes available): the
  map re-centers and re-frames for the new reference point.
- Location permission is denied, unsupported, or the context is insecure: the feature degrades to
  the selected-city path (P2) or to no map (P3) with no blocking error.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST show, on the venue search experience, a map of venues that have a
  precise location and lie within a fixed 75 km radius of a reference point.
- **FR-002**: The system MUST resolve the reference point by precedence: (1) the customer's
  granted device location, else (2) an explicitly selected directory city, else (3) neither - in
  which case no map is shown.
- **FR-003**: The 75 km radius MUST be fixed and enforced by the system; a customer MUST NOT be
  able to widen it.
- **FR-004**: Only venues that have a precise, owner-set location MUST appear on the map; venues
  without one MUST NOT appear.
- **FR-005**: On first display for a given reference point, the map MUST automatically frame all
  in-range venues so they are visible on screen without the customer panning.
- **FR-006**: After the map is framed, the customer MUST be able to zoom and pan freely, and that
  manual navigation MUST NOT be undone by unrelated screen updates.
- **FR-007**: The venue nearest the reference point MUST be visually emphasized relative to the
  other in-range venues.
- **FR-008**: When there is no reference point, the system MUST NOT render a map area,
  placeholder, or empty frame - the map is absent entirely.
- **FR-009**: The system MUST NOT retain or log the customer's device coordinates after using
  them to center the map, and device coordinates MUST leave the device only at reduced (about
  1 km) precision.
- **FR-010**: User-provided venue text shown on the map MUST be rendered as plain text, never
  interpreted as markup or executable content.
- **FR-011**: The map capability MUST require a signed-in customer, consistent with the rest of
  the application; no anonymous exception is introduced.
- **FR-012**: Visible attribution for the map imagery provider and the city data source MUST
  remain present in the application.
- **FR-013**: The textual results list MUST reflect the same in-range venue set shown on the map
  (each listed venue corresponds to a map venue and vice versa); the list SHOULD be ordered by
  distance from the reference point, nearest first (ordering is a nice-to-have, not a hard
  requirement).
- **FR-014**: When multiple in-range venues are close together, the map MUST group them into a
  single marker showing the count, which separates into individual markers as the customer zooms
  in.
- **FR-015**: The map MUST obtain the customer's device location only through an explicit "near
  me" action the customer activates; it MUST NOT prompt for location automatically on page or map
  load. When location permission is already granted, the map MAY center automatically.

### Key Entities *(include if feature involves data)*

- **Venue**: Existing entity; its optional precise location (set by the owner) is what the radius
  map plots. Venues without a precise location are absent from the map.
- **City**: Existing directory entity; provides the fallback reference-point coordinates when the
  customer selects a city instead of sharing device location.
- **Reference point**: A transient center (device location or selected city coordinates) used
  only to compute the 75 km radius and frame the map; never persisted.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A customer who grants location sees the nearby-venues map framed to all in-range
  venues within 3 seconds of opening it.
- **SC-002**: In at least 95% of views, every venue within 75 km that has a precise location
  appears on the map and no venue beyond 75 km appears.
- **SC-003**: Customers correctly identify the nearest venue at a glance (the emphasized marker)
  in usability checks.
- **SC-004**: A customer who denies location and selects no city is never shown an empty map or a
  "no venues found" map - the map block is simply absent.
- **SC-005**: Customers who never open the map experience no measurable increase in initial page
  loading anywhere in the application.

## Assumptions

- Marker density (resolved 2026-07-19): venues that are close together are clustered into a count
  marker that expands on zoom-in - this is a deliberate product choice, not natural marker spread.
  This reintroduces a small map-clustering capability (a marker-clustering library on top of the
  existing map stack); its exact dependency and size are confirmed at /speckit-plan under the
  standing "new dependency needs sign-off with size" rule.
- Geolocation trigger (resolved 2026-07-19): the map obtains the device location only through an
  explicit "near me" action the customer activates - never a silent prompt on page or map load; if
  permission is already granted it may center automatically.
- Placement (resolved 2026-07-19): the radius map supersedes the current results-page map, and the
  search experience unifies around the reference point - both the map and the textual results list
  show the same in-range venue set (nearest first for the list, a nice-to-have). How sport
  filtering interacts with the in-range set (narrowing within the radius) is a /speckit-plan
  detail.
- Coverage and data are unchanged from the shipped city/geolocation feature: Ukrainian directory
  cities, venues carrying optional owner-set coordinates, the same trilingual UI and privacy
  posture.
- The confirmed design decisions, constraints, and consciously rejected alternatives for this
  feature are recorded in the consilium artifact
  `.specify/consilium/2026-07-19-venue-radius-map.md`, which planning consumes as input.
