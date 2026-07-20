# Data Model: Colocate single-use DTOs into their owning Features folders

This feature moves code, not persisted data - no database schema, migration, or entity changes.
The "entities" below are the two categories established by spec.md's Key Entities section, mapped
onto their real, concrete membership (research.md's classification table).

## Single-use DTO (10 records - move)

**Represents**: A request or response record read/written by exactly one action.

**Real membership**: `FreeSlot`, `AvailabilityResponse`, `CreateBookingRequest`,
`BookingStatusFilter`, `CreateReviewRequest`, `CreateVenueRequest`, `UpdateVenueRequest`,
`VenueSummaryResponse`, `NearbyVenueResponse`, `CreateCourtRequest`, `UpdateCourtRequest`.

**New location**: Each joins its owning action's existing `Features/<Resource>/<UseCase>/
<UseCase>.cs` file, alongside that action's Command/Query and Handler (research.md's table gives
the exact destination for each).

## Shared DTO (7 records - stay in `Dtos/`)

**Represents**: A request or response record read/written by two or more actions, per spec
FR-002.

**Real membership**: `UserResponse`, `AuthResponse`, `BookingResponse`, `CityResponse`,
`CourtResponse`, `ReviewResponse`, `VenueDetailResponse`.

**Location**: Unchanged - `Dtos/AuthDtos.cs`, `Dtos/BookingDtos.cs`, `Dtos/CityDtos.cs`,
`Dtos/ReviewDtos.cs`, `Dtos/VenueDtos.cs`, each slimmed to only the shared records that remain.

**Invariant**: No field name, field type, or JSON structure changes for any DTO in either
category - this feature moves file locations and C# namespaces only.
