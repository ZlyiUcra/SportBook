# Implementation Plan: Colocate single-use DTOs into their owning Features folders

**Branch**: `011-colocate-dtos` | **Date**: 2026-07-20 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/011-colocate-dtos/spec.md`

## Summary

Move each of the 10 single-use DTO records (see research.md's classification table) out of the
central, resource-grouped `backend/src/SportBook.Application/Dtos/` folder and into the
`Features/<Resource>/<UseCase>/` folder of the one action that uses it, colocated with that
action's Command/Query and Handler. The 7 genuinely shared DTOs (used by 2+ actions) stay in
`Dtos/` unchanged. No wire-contract or namespace-visible behavior change for any client.

## Technical Context

**Language/Version**: C# / .NET 10

**Primary Dependencies**: None new - this is a pure file/namespace reorganization within the
existing `SportBook.Application` project.

**Storage**: SQL Server via EF Core 10 (unchanged)

**Testing**: xUnit - `SportBook.UnitTests` and `SportBook.IntegrationTests`, both pre-existing;
no assertion changes expected, since no wire shape changes

**Target Platform**: ASP.NET Core web service (unchanged)

**Project Type**: Web service backend (unchanged; frontend untouched)

**Performance Goals**: None - compile-time-only change, zero runtime effect

**Constraints**: Zero wire-contract change for any endpoint (spec FR-003); shared DTOs must not be
duplicated or arbitrarily assigned to one action's folder (spec FR-002)

**Scale/Scope**: 10 single-use DTO records moving across 8 destination folders (Availability,
Bookings x2, Reviews, Venues x4, Courts x2); 7 shared DTO records staying in place; each moved
DTO's namespace changes from `SportBook.Application.Dtos` to its new
`SportBook.Application.Features.<Resource>.<UseCase>`, requiring a `using` update everywhere it is
referenced (its own Handler, and any `Endpoints/*.cs` file that binds it directly)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

`.specify/memory/constitution.md` is still the unfilled bootstrap template - no ratified
principles, so the gate trivially passes pre- and post-design (same status as 001-010).

## Project Structure

### Documentation (this feature)

```text
specs/011-colocate-dtos/
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
│   │   └── Endpoints/                        # `using` directives updated for any moved DTO
│   │                                          # bound directly as a Minimal API parameter -
│   │                                          # no route/verb/logic change
│   ├── SportBook.Application/
│   │   ├── Features/<Resource>/<UseCase>/     # Each of the 10 single-use DTOs moves into its
│   │   │                                      # owning action's file here, alongside the
│   │   │                                      # existing Command/Query + Handler
│   │   └── Dtos/                              # Slimmed to the 7 shared DTOs
│   │       ├── AuthDtos.cs                    #   UserResponse, AuthResponse
│   │       ├── BookingDtos.cs                 #   BookingResponse only (CreateBookingRequest,
│   │       │                                  #   FreeSlot, AvailabilityResponse,
│   │       │                                  #   BookingStatusFilter move out)
│   │       ├── CityDtos.cs                    #   CityResponse (unchanged - already single-DTO)
│   │       ├── ReviewDtos.cs                  #   ReviewResponse only (CreateReviewRequest
│   │       │                                  #   moves out)
│   │       ├── VenueDtos.cs                   #   VenueDetailResponse, CourtResponse only
│   │       │                                  #   (CreateVenueRequest, UpdateVenueRequest,
│   │       │                                  #   VenueSummaryResponse, NearbyVenueResponse,
│   │       │                                  #   CreateCourtRequest, UpdateCourtRequest move
│   │       │                                  #   out)
│   │       └── Mapping.cs                     #   Unchanged (research.md decision - out of scope)
│   ├── SportBook.Domain/                      # Unchanged
│   └── SportBook.Infrastructure/              # Unchanged
└── tests/                                      # Unchanged - no test asserts a DTO's namespace
```

**Structure Decision**: Each moved DTO joins its action's existing `<UseCase>.cs` file (the same
file already holding that action's Command/Query and Handler) rather than a new file in the same
folder - keeping the "one file per action, everything about it in one place" property spec 009
established, extended to cover DTOs too.

## Complexity Tracking

*No Constitution Check violations - table intentionally omitted.*
