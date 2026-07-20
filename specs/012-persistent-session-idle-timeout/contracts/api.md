# Contracts: Persistent session with idle-timeout auto-logout

## No new or changed HTTP contract

This feature adds no endpoint and changes no existing endpoint's route, verb, request shape,
response shape, status code, or auth requirement. It is a frontend consumer of two endpoints that
already existed before this feature (see `specs/001-sportbook-venue-booking/contracts/api.md` for
their original definition):

- `POST /api/auth/refresh` - previously defined but unused by the frontend; this feature is the
  first caller, invoked once on app mount when a stored session exists.
- `POST /api/auth/logout` - previously defined but unused by the frontend (the session was never
  persisted, so there was no refresh token worth revoking); this feature calls it from both the
  manual sign-out button and the idle-timeout auto-logout path.

## Client-side contract this feature does add

Not an HTTP contract, but worth naming since it is the actual interface this feature introduces:
the `localStorage` key `sportbook-session` (see data-model.md) is local to this feature and not a
contract any other part of the app should read directly - all access goes through
`entities/session/model/store.ts`'s exported functions.
