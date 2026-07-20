# Contracts: Backend rearchitecture to vertical slice architecture

## No wire-contract changes

This feature MUST NOT change any HTTP contract (spec FR-002, FR-007). Every route, HTTP verb,
request shape, response shape, status code, and auth requirement across all 26 actions
(enumerated in [data-model.md](../data-model.md)'s Self-contained unit table) is byte-identical
to what specs 001-008 already documented and shipped. This document exists to state that
explicitly and to name the two places that behavior could have silently drifted during the
rework, and how each was closed:

- **Enum wire format** (`sportType` on Court create/update): confirmed unchanged - see
  research.md Decision 1's gotcha and the dedicated regression test
  (`VenueManagementTests.Creating_a_court_with_a_raw_string_valued_sportType_body_succeeds`),
  which posts a raw string-valued JSON body and asserts 201.
- **Pagination defaults** (`page`/`pageSize` omitted from a list endpoint's query string):
  confirmed unchanged - `page` defaults to 1, `pageSize` to 20, capped at 100, exactly as before
  (research.md Decision 4). All existing `PaginationBindingTests.cs` cases pass unmodified.

## Auth posture, unchanged

The global fallback policy (`RequireAuthenticatedUser()`) and the three explicit
`.AllowAnonymous()` endpoints (`POST /api/auth/register`, `/login`, `/refresh`) are unchanged.
`POST /api/auth/logout` deliberately stays behind the fallback policy, as before - verified by a
manual `curl` check (no token → 401) each time this feature's endpoints were touched, since no
automated test previously covered it (a pre-existing gap this feature did not introduce and does
not claim to close).

## Full endpoint list

Unchanged from prior specs. See `specs/001-sportbook-venue-booking/contracts/api.md` and each
subsequent feature's `contracts/api.md` (002-008) for the authoritative, still-accurate contract
of every route this feature re-implements without modifying.
