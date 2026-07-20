# Feature Specification: Switch the backend's dispatch mechanism to MediatR

**Feature Branch**: `010-switch-to-mediatr`

**Created**: 2026-07-20

**Status**: Draft

**Input**: User description: "Backend switch from martinothamar/Mediator to MediatR (latest).
Follow-up to spec 009 (backend rearchitecture to vertical slice architecture), which adopted
martinothamar/Mediator specifically because MediatR went commercial-licensed at v13.0.0.
Revisiting that call: MediatR's commercial license has a free Community tier for organizations
under $5,000,000 USD annual gross revenue and under $10,000,000 raised outside capital - this
project qualifies, so the earlier objection does not block using it. This change, already
implemented and verified, replaces martinothamar/Mediator with MediatR across the backend, with
no change to the Features/ folder structure, the Command/Query+Handler-per-use-case shape, or any
HTTP contract - routes, request/response shapes, and status codes are byte-identical to before.
Verified via a clean solution build, the full unit + integration test suite green, and a manual
check against a live server."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Use the ecosystem-standard dispatch library once the earlier objection is confirmed not to apply (Priority: P1)

Spec 009 chose a smaller, less widely-known dispatch library specifically to avoid a
commercially-licensed alternative, on the assumption that "commercially-licensed" meant "not
usable here." Since that alternative's paid tiers only apply to organizations above a
revenue/capital threshold this project has no realistic path to reaching, continuing to avoid it
protects against nothing - it only trades away the more widely-adopted library's broader
documentation, community familiarity, and long-term maintenance backing for no actual benefit.

**Why this priority**: This is the entire content of this feature - a single library swap
justified by new information (the free-tier threshold) that changes the conclusion spec 009
reached, without changing anything about the surrounding architecture that spec 009 established.

**Independent Test**: Confirm every command/query dispatch in the backend still resolves to the
same handler and produces the same result as before the swap, for every existing action, with no
observable difference from a caller's point of view.

**Acceptance Scenarios**:

1. **Given** an existing client of the backend (the web frontend, or an automated test), **When**
   any request that previously succeeded is repeated after the swap, **Then** it succeeds the same
   way - same status code, same response shape, same data.
2. **Given** a developer reads any one self-contained action (a request definition plus its
   handler), **When** they look for what changed, **Then** they find only the dispatch library's
   name and the mechanical adjustments its API requires - not a different structure, folder
   layout, or division of responsibility.

---

### Edge Cases

- What happens if the project's revenue or raised capital later crosses the free-tier threshold?
  Out of scope for this feature - it is a future licensing/budget decision for whoever operates
  the product at that point, not something this rework needs to plan for now.
- What happens to code that depended on the previous library's specific return-type conventions
  (for example, a lighter-weight task-like type used for performance in high-throughput
  scenarios)? It is adjusted to the new library's equivalent convention; no caller-visible
  behavior depends on which internal task-like type a handler happens to return.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The backend's command/query dispatch mechanism MUST be the library named in this
  feature's Input, replacing the one adopted under spec 009.
- **FR-002**: Every self-contained action (request definition plus handler) established under
  spec 009 MUST continue to exist as the same self-contained unit, in the same location, after
  this swap - this feature changes only the dispatch library, not the architecture built on top
  of it.
- **FR-003**: No HTTP endpoint's route, verb, response status code, request shape, or response
  shape MUST change as a result of this swap.
- **FR-004**: This feature explicitly supersedes spec 009's FR-005 ("the dispatch mechanism MUST
  NOT depend on a component whose continued free availability is a revenue-gated business
  decision") - the chosen library's free use IS conditional on the operating organization staying
  under a published revenue/capital threshold; this is accepted as a reasonable tradeoff given
  this project's realistic scale, not treated as a violation to work around.
- **FR-005**: Every automated check that exercised the backend before this swap MUST still pass
  afterward, asserting the same behavior as before.

### Key Entities

- No data entities are introduced, changed, or removed by this feature - it replaces an internal
  dispatch mechanism only.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of the backend's command/query dispatch calls use the library named in this
  feature's Input; zero remain on the library adopted under spec 009.
- **SC-002**: Every existing automated check (unit-level and end-to-end) that passed before this
  swap still passes afterward, with zero changes to what behavior those checks assert.
- **SC-003**: A person manually exercising the application's primary flows after this swap
  observes no difference from before it - same inputs produce the same outputs, same errors, same
  status codes.

## Assumptions

- This specification documents a swap that was already implemented and verified before this spec
  was written - it captures the agreed intent and observed outcome, the same documentation-after-
  the-fact pattern used for specs 006-009.
- "This project's realistic scale" (FR-004) is a judgment call made directly by the project's
  owner, not derived from a financial projection - it is not re-litigated by this spec.
- The specific revenue/capital threshold referenced in this feature's Input is the chosen
  library's own published licensing term at the time of this swap; if that vendor changes the
  term later, re-evaluating the choice is a future decision, out of scope here.
