# Contracts: Switch the backend's dispatch mechanism to MediatR

## No wire-contract changes

This feature MUST NOT change any HTTP contract (spec FR-003). Every route, HTTP verb, request
shape, response shape, status code, and auth requirement across all 26 actions is byte-identical
to what spec 009 already documented and shipped. This is a strictly internal dispatch-library
swap - `IMediator.Send(request, ct)` is called from every endpoint exactly as before, just against
a different implementation of `IMediator` underneath.

- **Endpoint code**: unchanged except for the `using Mediator;` -> `using MediatR;` line in each
  of the 8 `Endpoints/*.cs` files - no route, verb, or status-code logic touched.
- **Void-command endpoints** (`logout`, court/venue deletion): the trickiest case, since MediatR
  has no `Unit` return type - manually verified via a live-server `curl` check that `POST
  /api/auth/logout` still returns `204 No Content`, exactly as before.

## Full endpoint list

Unchanged from spec 009. See `specs/009-backend-slice-architecture/contracts/api.md` and each
earlier feature's `contracts/api.md` (001-008) for the authoritative, still-accurate contract of
every route this feature re-implements without modifying.
