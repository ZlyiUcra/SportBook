# Contracts: Colocate single-use DTOs into their owning Features folders

## No wire-contract changes

This feature MUST NOT change any HTTP contract (spec FR-003). Every route, HTTP verb, request
shape, response shape, status code, and auth requirement is byte-identical to before - this is a
C#-namespace-only move of DTO record definitions, with no effect on the JSON any client sends or
receives. See `specs/009-backend-slice-architecture/contracts/api.md` and
`specs/010-switch-to-mediatr/contracts/api.md` for the still-accurate contract of every route.
