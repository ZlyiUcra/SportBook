# Feature Specification: Backend rearchitecture to vertical slice architecture

**Feature Branch**: `009-backend-slice-architecture`

**Created**: 2026-07-20

**Status**: Draft

**Input**: User description: "Backend rearchitecture to Vertical Slice Architecture. Three-step
rework of the SportBook backend, already implemented and consilium-reviewed: (1) converted all 8
ASP.NET Core MVC Controllers to Minimal API endpoint-mapping files, preserving every
route/verb/status-code/auth-requirement exactly; (2) adopted martinothamar/Mediator (MIT-licensed,
source-generator based - not MediatR, which went commercial-licensed at v13.0.0) as the
command/query dispatch mechanism; (3) rewrote all 26 endpoint-facing Application-layer service
methods into Command/Query + Handler pairs organized one folder per use case, replacing the prior
flat Services/ layer. No behavior change for any client - routes, request/response shapes, and
status codes are byte-identical to before."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Find and change one use case without touching unrelated code (Priority: P1)

A developer needs to change the behavior of one specific action (for example, how a booking is
cancelled) without having to read or risk breaking a shared service class that also handles
several unrelated actions. Today, before this rework, changing one action meant opening a service
file that mixed together every action for that resource (search, create, update, delete, and
more), each with its own validation and query logic side by side.

**Why this priority**: This is the core value of the rework - it is the reason the other two steps
exist. Everything else is groundwork for this outcome.

**Independent Test**: Pick any one action (e.g. "cancel a booking"), locate its request shape, its
handling logic, and its endpoint registration; confirm all three are reachable from a single,
narrowly-scoped location without opening a file that also defines unrelated actions.

**Acceptance Scenarios**:

1. **Given** a developer wants to change the validation rule for booking cancellation, **When**
   they locate the cancellation logic, **Then** they find it in a single self-contained location
   that defines nothing else, with no unrelated actions (create, confirm, list) in the same file.
2. **Given** a developer is unfamiliar with the codebase, **When** they need to find where an
   HTTP endpoint's request is transformed into a database change, **Then** the request shape, the
   handling logic, and the endpoint registration follow one consistent, predictable pattern across
   every action in the backend.

---

### User Story 2 - Every backend action follows one uniform shape (Priority: P2)

A developer adding a brand-new action to the backend needs a single, predictable pattern to follow
- a request shape and a handler - rather than having to decide case-by-case whether to add a
method to an existing multi-purpose class, and rather than an endpoint file directly containing
business logic, database queries, and authorization checks all at once.

**Why this priority**: This establishes the uniform shape that User Story 1's per-action
separation depends on; it matters independently because it is also what makes cross-cutting
concerns (validation, logging) addable in one place later, if ever needed.

**Independent Test**: Pick any two unrelated actions from different resources (e.g. "register a
new account" and "delete a court") and confirm both are expressed as the same two-part shape
(a request definition plus a handler that processes it), dispatched the same way from their
respective HTTP endpoints.

**Acceptance Scenarios**:

1. **Given** two actions belonging to entirely different resources, **When** a developer compares
   how each is structured, **Then** both follow the identical request-plus-handler shape, with no
   resource-specific exception to the pattern.
2. **Given** an HTTP endpoint receives a request, **When** it hands the request off for
   processing, **Then** it does so through one uniform dispatch call, not a direct method call
   whose signature varies action to action.

---

### User Story 3 - The web framework layer stays a thin, swappable shell (Priority: P3)

The team wants the HTTP-handling layer (which endpoints exist, which HTTP verbs and routes they
answer to) to be simple enough that it could be swapped for a different web framework someday
without having to touch or re-test any business logic, since the business logic does not live in
that layer at all.

**Why this priority**: Lowest priority because it is a durability property, not something anyone
needs today - but it is the concrete, verifiable proof that the separation from User Story 1 and
2 actually holds, not just in spirit.

**Independent Test**: Confirm that every one of the backend's HTTP endpoint definitions contains
no business logic, database access, or validation of its own - only route/verb registration and a
single hand-off call - for every endpoint in the backend, without exception.

**Acceptance Scenarios**:

1. **Given** any HTTP endpoint definition in the backend, **When** a developer reads it,
   **Then** it contains only routing information and a single call handing the request off
   elsewhere for processing - never a database query or a business rule.
2. **Given** the full set of existing callers of the backend (the web frontend, automated tests),
   **When** this rework is complete, **Then** none of them observe any difference in behavior -
   every route, request shape, response shape, and status code stays exactly as it was before.

---

### Edge Cases

- What happens to logic that is genuinely shared by several actions (for example, an
  authorization rule reused by three different actions on the same resource)? It remains a
  plain, directly-called shared component rather than being forced into the one-action-per-unit
  pattern, and rather than being duplicated across every action that needs it.
- What happens to an action that previously had no dedicated business-logic component at all
  (its handling lived directly in the endpoint)? It is given a proper, standalone unit the same
  as every other action, so no action is an undocumented exception to the uniform shape.
- What happens if a single client-visible behavior spans two steps of internal processing (for
  example, one action's result also determines a second piece of related information, like
  whether a brand-new record was created versus an existing one updated)? The result of that
  distinction is preserved exactly as it was before, expressed through the single unit's own
  result rather than lost or flattened.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The backend MUST expose every HTTP endpoint through a lightweight routing
  declaration (route, verb, and a single hand-off call), not through the heavier
  multi-responsibility controller-class style used before this rework.
- **FR-002**: Every endpoint's route, HTTP verb, response status code, and authentication
  requirement MUST remain byte-identical to what it was before this rework - no client-visible
  behavior may change as a side effect of the internal restructuring.
- **FR-003**: Every action reachable through an HTTP endpoint MUST be expressed as a single,
  self-contained unit (a request definition plus its handling logic) rather than as one method
  among several unrelated ones inside a shared, multi-purpose class.
- **FR-004**: Each self-contained unit from FR-003 MUST be dispatched from its HTTP endpoint
  through one uniform mechanism, not a directly-typed method call whose shape varies by action.
- **FR-005**: The dispatch mechanism from FR-004 MUST NOT depend on a component whose continued
  free availability is a revenue-gated business decision by a for-profit vendor; it MUST be
  available under a permanent, unconditionally free license.
- **FR-006**: Logic genuinely shared across more than one action (for example, an authorization
  check reused by several actions on the same resource) MUST remain a plain, directly-invoked
  shared component, not be force-fit into the one-unit-per-action shape and not be duplicated.
  Two of this rework's own actions being unable to call each other's self-contained units
  directly is itself a symptom that the shared logic between them belongs in such a component.
- **FR-007**: The rework MUST NOT change any request or response wire shape (field names, field
  types, or JSON structure) for any existing endpoint.
- **FR-008**: Every self-contained unit from FR-003 MUST be independently verifiable (automated
  test coverage) without requiring the whole backend to be exercised end to end for that one
  action's logic to be checked.

### Key Entities

- **Endpoint registration**: The HTTP-facing declaration of one route (path, verb, and auth
  requirement) that hands an incoming request off to exactly one self-contained unit and returns
  its result - contains no business logic itself.
- **Request/response pair**: The shape of data going into one self-contained unit and the shape
  of data it returns - both stay wire-identical to what the corresponding endpoint sent/received
  before this rework, even though how they get produced internally has changed.
- **Self-contained unit (action)**: One specific thing the backend can be asked to do (e.g.
  "cancel a booking"), holding its own validation and processing logic, reachable from exactly
  one endpoint registration, and independently testable.
- **Shared component**: Logic genuinely needed by more than one self-contained unit (an
  authorization rule, a token-issuing routine, a validation routine) - explicitly not itself a
  self-contained unit, since it answers to no endpoint of its own.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of the backend's HTTP endpoints are expressed in the lightweight routing style
  described in FR-001; zero remain in the previous heavier controller style.
- **SC-002**: 100% of the backend's client-reachable actions (every one that answers an HTTP
  endpoint) are expressed as a self-contained, independently-testable unit; zero remain as one
  method among several unrelated ones inside a shared, multi-purpose class.
- **SC-003**: Every existing automated check that exercised the backend before this rework
  (unit-level and full end-to-end) still passes afterward, with zero changes to what behavior
  those checks assert - only how the tested code is reached internally may differ.
- **SC-004**: A person manually exercising the application's primary flows (signing up, searching,
  booking, reviewing, managing a venue) after this rework observes no difference from before it -
  same inputs produce the same outputs, same errors, same status codes.
- **SC-005**: A developer picking any single action at random can name, within one minute, the one
  location that defines its request shape and the one location that defines its handling logic -
  without first having to rule out which of several unrelated methods in a shared class it might
  be.

## Assumptions

- This specification documents a rework that was already designed (via a multi-perspective
  review process), implemented, and verified before this spec was written - it captures the
  agreed intent and observed outcome, rather than proposing new work to be estimated and planned.
- "Client" throughout this spec means any existing caller of the backend's HTTP API - the web
  frontend and the backend's own automated test suites - none of which are themselves being
  changed by this rework.
- The specific dispatch mechanism chosen to satisfy FR-004/FR-005 was selected after comparing it
  against a hand-written alternative and against a well-known commercially-licensed alternative;
  the comparison and the reasoning are recorded outside this spec (architecture review records)
  and are not repeated here, since this document describes outcomes, not the decision process.
- A small number of components are shared across several actions on the same resource (for
  example, ownership-authorization checks, and token issuance shared across account actions) -
  per FR-006, these intentionally remain outside the one-unit-per-action structure described in
  FR-003.
