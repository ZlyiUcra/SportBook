# Research: Switch the backend's dispatch mechanism to MediatR

This document records the decision actually made and the concrete implementation gotchas found
while carrying it out; it is not a forward-looking survey.

## Decision: MediatR 14.2.0, revisiting spec 009's rejection

**Decision**: Replace `martinothamar/Mediator` with `MediatR` 14.2.0 as the command/query dispatch
library, keeping every other part of spec 009's architecture unchanged.

**Rationale**: Spec 009's mediator-adoption consilium rejected MediatR specifically because it
became commercially licensed at v13.0.0, treating "revenue-gated" as equivalent to "not usable
here" without checking the actual threshold. MediatR's commercial terms include a free Community
tier for organizations under $5,000,000 USD annual gross revenue and under $10,000,000 raised
outside capital - this project has no realistic path to crossing either figure, so the free tier
applies unconditionally in practice. Once that threshold is known, the balance of considerations
from spec 009's review (MediatR's much larger install base, more StackOverflow/documentation
coverage, and broader hiring familiarity vs. martinothamar/Mediator's compile-time source
generation and permanent MIT license) tips toward the ecosystem-standard library, since the
license risk it was traded away to avoid does not actually apply.

**Alternatives considered**: Staying on `martinothamar/Mediator` (rejected - no longer any reason
to prefer it once the license objection is confirmed not to bind); a hand-rolled dispatcher (not
reconsidered here - already evaluated and not chosen during spec 009's review, and nothing about
this decision changes that evaluation).

**Gotchas found during implementation**:

- MediatR's `IRequestHandler<TRequest,TResponse>.Handle` returns `Task<TResponse>`, not
  martinothamar's `ValueTask<TResponse>`. Every one of the 26 Handlers' `Handle` method signatures
  needed this mechanical change (`async ValueTask<T>` -> `async Task<T>`).
- MediatR has no equivalent of martinothamar's `IRequestHandler<TCommand>` (no response) returning
  `ValueTask<Unit>` with an explicit `return Unit.Value;` - its non-generic `IRequestHandler<TCommand>`
  returns plain `Task`, with no `Unit` type at all. The three void-command Handlers (`Logout`,
  `DeleteCourt`, `DeleteVenue`) had their `Handle` signature changed to `async Task` and their
  trailing `return Unit.Value;` removed.
- Two unit test files called `.AsTask()` on a Handler's `Handle(...)` result specifically to
  convert martinothamar's `ValueTask<T>` into the `Task` shape xUnit's `Assert.ThrowsAsync`
  expects. `Task<T>` already satisfies that signature, so the now-invalid `.AsTask()` calls
  (`Task<T>` has no such method) were dropped rather than replaced.
- MediatR's handler registration (`AddMediatR(cfg => cfg.RegisterServicesFromAssembly(...))`)
  defaults handlers to `ServiceLifetime.Transient`, unlike martinothamar's `Singleton` default that
  spec 009 had to explicitly override to `Scoped` to avoid a captive-dependency DI failure against
  `SportBookDbContext`. Transient has no such conflict with a scoped dependency, so no lifetime
  override was needed this time - confirmed by the same `WebApplicationFactory`-boot DI validation
  that would have caught it if it existed.
