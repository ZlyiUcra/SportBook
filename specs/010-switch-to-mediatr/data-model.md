# Data Model: Switch the backend's dispatch mechanism to MediatR

This feature changes an internal library dependency, not persisted data - no database schema,
migration, or entity changed, and no structural/code-organization unit from spec 009 is added,
removed, or renamed. The only "entity" worth recording here is the shape of the thing that
changed.

## Self-contained unit (action) - shape after this swap

**Represents**: The same one-Command/Query-plus-Handler-per-action unit spec 009 established;
unchanged in location, naming, and responsibility. What changed is the interface it implements and
its `Handle` method's return type.

**Real shape (26 of these, unchanged in count and location)**:

- Requests: `sealed record XxxCommand`/`XxxQuery` implementing `MediatR.IRequest<TResponse>` (was
  `Mediator.IRequest<TResponse>`) - identical generic shape, different namespace.
- Handlers with a response (23 of the 26): `sealed class XxxHandler : IRequestHandler<TRequest,
  TResponse>` with `public async Task<TResponse> Handle(TRequest request, CancellationToken ct)`
  (was `async ValueTask<TResponse>`).
- Handlers without a response (3 of the 26 - `Logout`, `DeleteCourt`, `DeleteVenue`): `sealed
  class XxxHandler : IRequestHandler<TCommand>` with `public async Task Handle(TCommand request,
  CancellationToken ct)` and no return statement (was `async ValueTask<Unit>` ending in `return
  Unit.Value;`).

**Invariant**: Every Handler's internal logic - the database queries, validation, and business
rules between its opening and closing brace - is byte-identical to before this swap. Only the
method signature and, for the 3 void commands, the removal of the `Unit.Value` return line,
changed.
