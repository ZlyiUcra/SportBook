# Contracts: Frontend resilience and continuity polish

## No HTTP contract changes

This feature adds no endpoint and changes no existing endpoint's route, verb, request shape,
response shape, status code, or auth requirement. All three pieces are entirely client-side.

## Client-side contract this feature adds

Not an HTTP contract, but worth naming since it is the actual interface this feature introduces:
the `localStorage` key `sportbook-venue-search` (see data-model.md) is local to this feature and
not a contract any other part of the app should read directly - all access goes through
`pages/venues/model/searchStore.ts`'s exported store and setters.
