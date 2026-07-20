# Research: Backend rearchitecture to vertical slice architecture

This rework was carried out in two consilium-reviewed steps; this document records the decisions
actually made and the concrete implementation gotchas found while carrying them out; it is not a
forward-looking survey.

## Decision 1: MVC Controllers → Minimal API

**Decision**: Convert all 8 controllers to Minimal API endpoint-mapping extension methods
(`MapXxxEndpoints(this IEndpointRouteBuilder app)`), one file per resource, matching the prior
one-controller-per-resource granularity exactly.

**Rationale**: Consilium review (`.specify/consilium/2026-07-20-minimal-api-migration.md`) found
this a mechanical, low-risk, behavior-preserving conversion - all 5 review seats rated it DOES
NOT BLOCK, route `direct-verified`. It is also the officially documented path toward vertical
slices: Minimal API's `MapGroup` + one-extension-method-per-feature-area pattern is Microsoft's
own recommended shape for this scale of API.

**Alternatives considered**: Staying on Controllers and adding vertical slices around them was
rejected - Controllers carry MVC-specific binding/serialization conventions (documented below)
that would have had to be replicated by hand inside each slice instead of once, framework-level.

**Gotchas found during implementation** (not predicted by the consilium review, discovered by
running the real test suite):

- `ConfigureHttpJsonOptions` (Minimal API's JSON options) is a *different* options object from
  MVC's `AddJsonOptions` - the `JsonStringEnumConverter` registration had to be moved, not just
  left in place. The existing 71 integration tests could not have caught a missed move: the test
  client's own default `PostAsJsonAsync` serializes enums numerically (System.Text.Json's
  reflection-free default), the same shape a converter-less server already accepts - only the
  real frontend's string-valued enum bodies exercise the code path that needs the converter. A
  dedicated regression test (`VenueManagementTests.
  Creating_a_court_with_a_raw_string_valued_sportType_body_succeeds`) posts a raw JSON string
  body specifically to close this blind spot.
- Non-nullable value-type query parameters without a C# default (`bool includeNearby`,
  `bool mine`, `BookingStatusFilter status`) became *required* under Minimal API's binder, where
  MVC's `[FromQuery]` complex/simple binding silently defaulted a missing one. Fixed by adding
  explicit C# default values (`= false`, `= BookingStatusFilter.All`) at each call site.

## Decision 2: Dispatch mechanism - martinothamar/Mediator, not MediatR, not hand-rolled

**Decision**: `Mediator.Abstractions` + `Mediator.SourceGenerator` 3.0.2 (martinothamar/Mediator).

**Rationale**: A second consilium round (`.specify/consilium/2026-07-20-mediator-adoption.md`)
weighed three options - MediatR, this library, and a hand-rolled dispatcher - explicitly kept
blind to the requester's own stated preference until after independent review. All five seats
converged on rejecting MediatR specifically: it became a commercial product at v13.0.0
(2026-07-02), free only under a company-revenue-gated "Community edition" - a for-profit vendor's
policy, not a permanent license grant, for a dependency none of the project's other packages
carry that risk on. martinothamar/Mediator offers the identical `IRequest<T>`/
`IRequestHandler<TRequest,TResponse>`/`mediator.Send(...)` shape under a permanent MIT license,
resolved at compile time via source generation (no runtime reflection, unlike MediatR's DI
resolution). One seat (best-practices) held that no dispatch library was justified at all, absent
any planned pipeline-behavior use (validation/logging) - the requester's own stated ergonomic
preference (matching MediatR's shape) was the deciding tie-break, applied only after the board's
independent analysis, per the requester's explicit instruction not to let it bias the review
itself.

**Alternatives considered**: MediatR (rejected - license risk, detailed above); hand-rolled
`IHandler<TRequest,TResponse>` (viable, zero dependency, best-practices' pick - not chosen).

**Gotcha found during implementation**: `AddMediator()` defaults to `ServiceLifetime.Singleton`
for both `IMediator` and every registered handler. Every handler in this codebase depends on the
scoped `SportBookDbContext`, so the default produced a captive-dependency DI validation failure
at host startup (`Cannot consume scoped service 'SportBookDbContext' from singleton
'...Handler'`), caught immediately by the integration test suite's `WebApplicationFactory` boot.
Fixed with `AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped)`.

## Decision 3: One folder per action, shared logic stays a plain collaborator

**Decision**: `Features/<Resource>/<UseCase>/` holds exactly one Command or Query record plus its
Handler. Logic used by more than one action (ownership checks, token issuance, a shared
venue-detail read, a shared location validator, booking-specific query/validation helpers, the
city-neighbor lookup) stays in `Services/` as a plain constructor-injected class or static helper
- never wrapped in its own Command/Query, and never called via a nested `mediator.Send` from
inside another Handler.

**Rationale**: A Handler calling `mediator.Send` on another Handler is a documented mediator
anti-pattern (breaks pipeline-behavior assumptions, obscures the real call graph) - flagged during
consilium review before any code was written. The alternative of duplicating shared logic into
every action that needs it was rejected as the more expensive mistake.

**Alternatives considered**: Wrapping every shared helper in its own Query too (rejected - no
endpoint answers to `EnsureVenueOwner` or `IssueTokensAsync`, so FR-006's "genuinely shared"
carve-out applies); nested mediator dispatch between Handlers (rejected, anti-pattern above).

## Decision 4: Pagination binding needed a shape change, not just a binder swap

**Decision**: `PageRequest` changed from a property-only record with `init` accessors and private
backing-field defaults to a positional record with default constructor parameters
(`PageRequest(int Page = 1, int PageSize = DefaultPageSize)`), keeping the same clamping logic in
the property initializers.

**Rationale**: Minimal API's `[AsParameters]` binder inspects constructor parameter defaults to
decide whether a bound property is optional; a property-only record's `init` defaults are
invisible to it, so an omitted `page`/`pageSize` query parameter became a binding failure instead
of falling through to the default - caught by the full integration suite (18 tests failed on
first run after the endpoint conversion, all traced to this one root cause). The fix matches
Microsoft's own documented `[AsParameters]` pagination example.

**Alternatives considered**: Making every list endpoint's `page`/`pageSize` parameters individually
optional at the endpoint signature instead of via `[AsParameters]` (rejected - reintroduces the
per-endpoint duplication `PageRequest` existed to avoid).
