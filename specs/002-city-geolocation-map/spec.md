# Feature Specification: City Selection, Geolocation and Venue Map

**Feature Branch**: `002-city-geolocation-map`

**Created**: 2026-07-18

**Status**: Draft

**Input**: User description: "Опис фічі - в артефакті консиліуму .specify/consilium/2026-07-18-city-geolocation-map.md: розділ \"Узгоджена пропозиція\" (WHAT/WHY), межі й обмеження - у відповідних розділах артефакту."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Search venues by a structured city (Priority: P1)

A customer searching for venues picks their city from a suggestion list instead of typing free text. Suggestions appear in the language the customer uses the app in, match partial input in any supported language, and rank larger settlements first. The search then shows venues of exactly that city.

**Why this priority**: Free-text city input is the root defect this feature removes - typos and language variants ("Kyiv"/"Київ") silently hide venues from customers. Every other story builds on the structured city directory this story introduces.

**Independent Test**: Can be fully tested by typing a partial city name in any supported language on the search page, picking a suggested city, and verifying that only venues assigned to that city appear - with no way to submit a city that is not in the directory.

**Acceptance Scenarios**:

1. **Given** the city directory contains the customer's city, **When** the customer types at least 2 characters of its name in any supported app language, **Then** the suggestion list includes that city, with larger settlements ranked above smaller ones.
2. **Given** a customer has selected a suggested city, **When** the search runs, **Then** the results contain only venues assigned to that city.
3. **Given** a customer types text that matches no directory city, **When** they attempt to search, **Then** no free-text search happens - a city must be picked from the suggestions.
4. **Given** two settlements share the same name, **When** both appear in suggestions, **Then** each suggestion carries enough context (such as its region) for the customer to tell them apart.

---

### User Story 2 - Owner assigns city and precise location to a venue (Priority: P2)

A venue owner creating or editing a venue picks its city from the same structured directory and can optionally mark the venue's exact spot by placing a pin on a map. The pin can be moved or removed later.

**Why this priority**: The city directory only keeps search honest if venue data enters through it too - otherwise newly created venues drift back into unsearchable free text. The precise pin is what makes the customer-facing map (Story 5) truthful.

**Independent Test**: Can be tested by creating a venue while choosing a city from suggestions, verifying the venue appears in search for that city, then placing, moving and removing a location pin and verifying the venue page reflects each state.

**Acceptance Scenarios**:

1. **Given** an owner is creating a venue, **When** they fill in the location, **Then** the city is chosen from the directory suggestions and free-text city input is not accepted.
2. **Given** an owner is creating or editing a venue, **When** they place a pin on the map, **Then** the venue stores that precise location and customers see it on the venue's page.
3. **Given** a venue has a location pin, **When** the owner removes it, **Then** the venue keeps its city, and the venue page no longer shows a location map.

---

### User Story 3 - Detect my city automatically (Priority: P3)

A customer taps "my city" and, after granting the browser's location permission, gets the nearest directory city pre-selected in the search. The customer can always override the detected city manually.

**Why this priority**: Convenience on top of Story 1 - it removes typing entirely for the most common case ("venues near me") but the search is fully usable without it.

**Independent Test**: Can be tested by granting location permission, tapping "my city", and verifying the pre-selected city is the nearest directory city to the device position; then denying permission and verifying manual selection still works with no blocking errors.

**Acceptance Scenarios**:

1. **Given** a customer grants location access, **When** they use "my city", **Then** the nearest directory city is pre-selected and the customer may change it manually.
2. **Given** a customer denies location access or the device cannot provide a position, **When** they use "my city", **Then** the app continues with manual city selection and shows no blocking error.
3. **Given** a customer uses "my city", **When** the device position leaves the device, **Then** its precision is reduced to roughly one kilometre beforehand, and the received position is not stored anywhere after the nearest city is determined.

---

### User Story 4 - Include venues from nearby cities (Priority: P4)

A customer searching in a chosen city can widen the search to also include venues from cities within 150 km - useful near city borders and in agglomerations where the next town's court is closer than the far side of one's own city.

**Why this priority**: Adds real discovery value for border and agglomeration users, but only makes sense once city-scoped search (Story 1) exists.

**Independent Test**: Can be tested by enabling the nearby option for a city that has a neighbouring city with venues inside 150 km, verifying those venues appear labelled with their own city, and verifying venues from cities beyond 150 km never appear.

**Acceptance Scenarios**:

1. **Given** a customer selected a city and enabled the nearby option, **When** the search runs, **Then** results include venues from cities within 150 km of the selected city, and each result shows which city it belongs to.
2. **Given** the nearby option is off (the default), **When** the search runs, **Then** results contain only venues of the selected city.
3. **Given** any search request, **When** a wider radius is requested by manipulating the request, **Then** the service still applies the fixed 150 km limit.

---

### User Story 5 - See venues on a map (Priority: P5)

A customer viewing search results can open a map showing pins for the venues on the current results page that have a precise location, and jump from a pin to the venue. A venue's own page shows a small map with its pin when the owner has set one.

**Why this priority**: The map is presentation on top of the structured search - valuable for spatial orientation, but dependent on Stories 1, 2 and 4 for its data.

**Independent Test**: Can be tested by running a search where some result venues have pins and some do not, opening the map, and verifying exactly the pinned venues of the current page appear; then opening a pinned venue's page and verifying its single-marker map, and a pinless venue's page and verifying the map block is absent.

**Acceptance Scenarios**:

1. **Given** search results where some venues have precise locations, **When** the customer opens the map, **Then** pins appear for exactly those venues of the current results page that have a location, and selecting a pin leads to that venue.
2. **Given** a venue without a precise location, **When** the customer views the search map, **Then** that venue is absent from the map while remaining in the results list.
3. **Given** a venue with a precise location, **When** a customer opens its page, **Then** a map with a single marker at the venue's location is shown; **Given** a venue without one, **Then** no map block is shown at all - no placeholder, no city-centre approximation.
4. **Given** a venue whose name or description contains markup-like text, **When** it is shown on the map, **Then** the text is displayed as plain text and never interpreted as markup or code.
5. **Given** a customer who never opens the map, **When** they browse any page of the app, **Then** they experience no additional loading cost from the map feature.

---

### Edge Cases

- Location permission denied, unsupported browser, or insecure context: "my city" degrades to manual selection without blocking errors (Story 3, scenario 2).
- Customer is far from any directory city (abroad, remote area): the nearest city is still offered but may be distant - manual override always available.
- Selected city has no venues: the search shows an empty state and suggests enabling the nearby option.
- City names containing apostrophes and language-specific characters must match in suggestions regardless of which supported language the customer types in.
- Same-named settlements must be distinguishable in suggestions by region context.
- Every venue existing before this feature ships must be assigned a directory city as part of the rollout - no venue may silently disappear from city-scoped search.
- Attempts to bypass the fixed 150 km radius or send out-of-range coordinates are rejected by the service.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Venue search MUST filter by a city selected from the city directory; free-text city input MUST NOT be accepted anywhere city is chosen.
- **FR-002**: The city directory MUST cover Ukrainian settlements with population of at least 500 and carry each settlement's name in all three app languages, its region context, its geographic position, and its population.
- **FR-003**: City suggestions MUST match partial input (2+ characters) against any localized name, return a short ranked list (larger settlements first), and respond while the customer is typing.
- **FR-004**: The system MUST offer "my city" detection that, with the customer's explicit browser-level consent, resolves the device position to the nearest directory city; manual selection MUST always remain available and detection failures MUST NOT block the search.
- **FR-005**: Device coordinates MUST have their precision reduced to roughly one kilometre before leaving the device, and the service MUST NOT store received coordinates after resolving the nearest city.
- **FR-006**: The search MUST offer an off-by-default option to include venues from cities within 150 km of the selected city; the radius is fixed and MUST be enforced by the service regardless of what a client requests.
- **FR-007**: Venue owners MUST choose their venue's city from the same directory when creating or editing a venue.
- **FR-008**: Venue owners MUST be able to optionally set, move, and remove a precise venue location by placing a pin on a map.
- **FR-009**: The search results page MUST offer a map showing pins for the venues of the current results page that have a precise location; venues without one simply do not appear on it.
- **FR-010**: A venue's page MUST show a location map only when the venue has a precise location; otherwise no map block is shown - no city-centre fallback.
- **FR-011**: Every venue existing before release MUST be assigned a directory city during rollout; the rollout MUST fail loudly rather than leave any venue without a valid city.
- **FR-012**: User-provided venue text displayed on any map MUST be rendered as plain text, never interpreted as markup or executable content.
- **FR-013**: Visible attribution for the city directory data source and the map imagery provider MUST be present in the application (About page).
- **FR-014**: All geo capabilities MUST require a signed-in user, matching the access rules of the rest of the application; no anonymous exceptions are introduced by this feature.
- **FR-015**: Coordinate inputs accepted by the service MUST be validated against the legal latitude/longitude ranges.

### Key Entities

- **City**: A settlement in the directory - names in the three app languages, country, region context, geographic position, population; the single source of truth for anything city-related.
- **Venue**: Existing entity, extended - belongs to exactly one City (replacing the former free-text city) and optionally carries a precise geographic location set by its owner.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A customer finds venues in their city within 30 seconds of opening the search, typing no more than a few characters of the city name.
- **SC-002**: With location consent granted, "my city" pre-selects a city within 5 seconds, and in populated areas the pre-selected city is the customer's actual settlement or one within 20 km in at least 90% of cases.
- **SC-003**: A nearby-enabled search never returns a venue from a city farther than 150 km from the selected city.
- **SC-004**: At release, 100% of existing venues are assigned a valid directory city and remain findable through city-scoped search.
- **SC-005**: Venue search continues to meet the existing load target (500 concurrent searches) after the feature ships, verified by re-running the established load scenario.
- **SC-006**: Customers who never open a map see no measurable increase in initial page loading anywhere in the app.
- **SC-007**: City suggestions appear within 1 second of a typing pause.

## Assumptions

- Coverage is Ukraine-only for this feature; the population >= 500 threshold is confirmed against actual dataset counts before seeding, and expansion waits for a second market.
- Localized city names come from a public geographic dataset licensed CC BY 4.0; the required attribution lives on the About page together with map imagery attribution.
- No separate country selection is needed while coverage is single-country; each city still records its country for future expansion.
- The 150 km nearby radius is a fixed product constant, not user-configurable.
- Drawing zones or areas on the map is explicitly out of scope for this feature (no defined semantics or stated need).
- Map imagery for development and demos comes from a community tile provider with visible attribution; choosing a keyed provider is a recorded open item with the deadline "before production release".
- The confirmed design decisions, constraints, and consciously rejected alternatives for this feature are recorded in the consilium artifact `.specify/consilium/2026-07-18-city-geolocation-map.md`, which planning consumes as input.
