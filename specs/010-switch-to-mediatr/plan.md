# Implementation Plan: Switch the backend's dispatch mechanism to MediatR

**Branch**: `010-switch-to-mediatr` | **Date**: 2026-07-20 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/010-switch-to-mediatr/spec.md`

**Note**: This plan documents a swap already implemented and verified (see spec.md Assumptions) -
it records the technical approach actually taken, not a forward-looking design to be estimated.

## Summary

Replace `martinothamar/Mediator` (adopted under spec 009) with `MediatR` 14.2.0 as the backend's
command/query dispatch library, across `SportBook.Api` and `SportBook.Application`. No change to
the `Features/<Resource>/<UseCase>/` folder structure, the Command/Query+Handler shape, or any
HTTP contract - this is a dispatch-library swap underneath an unchanged architecture.

## Technical Context

**Language/Version**: C# / .NET 10

**Primary Dependencies**: `MediatR` 14.2.0 (replaces `Mediator.Abstractions` +
`Mediator.SourceGenerator` 3.0.2) - licensed under RPL-1.5 or a paid commercial tier since
v13.0.0, with a free Community tier for organizations under $5M annual gross revenue and $10M
raised outside capital (spec FR-004, superseding spec 009's FR-005)

**Storage**: SQL Server via EF Core 10 (unchanged)

**Testing**: xUnit - `SportBook.UnitTests` (Sqlite in-memory) and `SportBook.IntegrationTests`
(`WebApplicationFactory` against the real SQL Server container), both pre-existing and unchanged
in what they assert

**Target Platform**: ASP.NET Core web service (unchanged deployment target)

**Project Type**: Web service backend (unchanged; frontend untouched)

**Performance Goals**: None new - MediatR's reflection-based handler resolution is slower than the
prior source-generated lookup, but this project has no throughput requirement anywhere near where
that difference would be observable, and it was not a factor in this decision.

**Constraints**: Zero wire-contract change for any endpoint (spec FR-003); every self-contained
action from spec 009 must remain in place unchanged (spec FR-002)

**Scale/Scope**: 26 client-reachable actions across 8 resource areas (Auth, Availability,
Bookings, Cities, Courts, Reviews, Users, Venues) - the same set spec 009 converted; every one of
their Handlers touched mechanically by this swap, none restructured

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

`.specify/memory/constitution.md` is still the unfilled bootstrap template - no ratified
principles, so the gate trivially passes pre- and post-design (same status as 001-009).

## Project Structure

### Documentation (this feature)

```text
specs/010-switch-to-mediatr/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md         # Phase 1 output
├── quickstart.md         # Phase 1 output
├── contracts/            # Phase 1 output
└── tasks.md              # Phase 2 output (/speckit-tasks - not created by this command)
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── SportBook.Api/
│   │   ├── Endpoints/                        # Unchanged structure; `using Mediator;` ->
│   │   │                                      # `using MediatR;` in all 8 files - no other edit
│   │   ├── Program.cs                         # AddMediator(...) -> AddMediatR(...)
│   │   └── SportBook.Api.csproj               # Mediator.Abstractions/SourceGenerator -> MediatR
│   ├── SportBook.Application/
│   │   ├── Features/<Resource>/<UseCase>/     # Unchanged folder structure (26 actions); each
│   │   │                                      # Handler's `Handle` return type adjusted from
│   │   │                                      # ValueTask<T> to Task<T> (or plain Task for the
│   │   │                                      # 3 void commands: Logout, DeleteCourt,
│   │   │                                      # DeleteVenue), matching MediatR's convention
│   │   └── SportBook.Application.csproj       # Mediator.Abstractions -> MediatR
│   ├── SportBook.Domain/                      # Unchanged
│   └── SportBook.Infrastructure/              # Unchanged
└── tests/
    ├── SportBook.UnitTests/                   # Unchanged assertions; dropped now-unnecessary
    │                                          # `.AsTask()` calls used to bridge the prior
    │                                          # library's ValueTask to xUnit's Task-based APIs
    └── SportBook.IntegrationTests/            # Unchanged - no edits needed
```

**Structure Decision**: No structural change from spec 009 - this feature deliberately touches
only the dispatch library and the mechanical adjustments its API requires (namespace, return
type, registration call). The `Features/` folder layout, the Api/Application project boundary,
and every Handler's internal logic are untouched.

## Complexity Tracking

*No Constitution Check violations - table intentionally omitted.*
