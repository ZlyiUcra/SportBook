# Research: Colocate single-use DTOs into their owning Features folders

## Decision: Per-DTO classification, by actual current usage

**Decision**: Classify each of the 17 DTO records in `backend/src/SportBook.Application/Dtos/`
(across `AuthDtos.cs`, `BookingDtos.cs`, `CityDtos.cs`, `ReviewDtos.cs`, `VenueDtos.cs`) as
single-use or shared by counting the distinct `Features/<Resource>/<UseCase>/` folders (and
`Endpoints/*.cs` body-binding sites) that reference it today - per spec FR-004, no guessing about
future reuse.

**Rationale**: `grep`-based usage counting across `Features/`, `Endpoints/`, and `Dtos/` itself
(to catch one DTO embedding another) gives an unambiguous, evidence-based split. Two DTOs
initially looked single-purpose but turned out shared once embedding was checked: `CourtResponse`
is returned directly by CreateCourt/UpdateCourt/ListCourtsByVenue AND embedded inside
`VenueDetailResponse`; `CityResponse` is returned directly by FindNearestCity/SuggestCities AND
embedded inside `VenueSummaryResponse`, `NearbyVenueResponse`, `VenueDetailResponse`, and
`BookingResponse` - spanning three different resource areas, the strongest possible signal that a
DTO is a genuinely shared value type, not one action's private shape.

**Alternatives considered**: Moving every DTO regardless of sharing, duplicating shared ones per
action (rejected - spec FR-002 explicitly rules this out, matching spec 009's FR-006 precedent for
non-DTO shared logic); leaving all DTOs in place and treating this as not worth doing (rejected -
this is the feature's whole premise, already agreed).

### Classification table

**Single-use (move into the owning action's folder)**:

| DTO | Owning action | Destination |
|---|---|---|
| `FreeSlot`, `AvailabilityResponse` | GetAvailability | `Features/Availability/GetAvailability/` |
| `CreateBookingRequest` | CreateBooking | `Features/Bookings/CreateBooking/` |
| `BookingStatusFilter` (enum) | ListMyBookings | `Features/Bookings/ListMyBookings/` |
| `CreateReviewRequest` | CreateOrReplaceReview | `Features/Reviews/CreateOrReplaceReview/` |
| `CreateVenueRequest` | CreateVenue | `Features/Venues/CreateVenue/` |
| `UpdateVenueRequest` | UpdateVenue | `Features/Venues/UpdateVenue/` |
| `VenueSummaryResponse` | SearchVenues | `Features/Venues/SearchVenues/` |
| `NearbyVenueResponse` | SearchNearbyVenues | `Features/Venues/SearchNearbyVenues/` |
| `CreateCourtRequest` | CreateCourt | `Features/Courts/CreateCourt/` |
| `UpdateCourtRequest` | UpdateCourt | `Features/Courts/UpdateCourt/` |

**Shared (stay in `Dtos/`, unchanged location)**:

| DTO | Used by (2+ actions) |
|---|---|
| `UserResponse` | GetMe (direct) + embedded in `AuthResponse` (Login/Register/Refresh) |
| `AuthResponse` | Login, Register, Refresh |
| `BookingResponse` | CreateBooking, CancelBooking, ConfirmBooking, GetBookingById, ListMyBookings, ListVenueBookingsForOwner |
| `CityResponse` | FindNearestCity, SuggestCities, + embedded in VenueSummaryResponse/NearbyVenueResponse/VenueDetailResponse/BookingResponse |
| `CourtResponse` | CreateCourt, UpdateCourt, ListCourtsByVenue, + embedded in VenueDetailResponse |
| `ReviewResponse` | CreateOrReplaceReview (via its own colocated `CreateOrReplaceReviewResult`), ListReviewsByVenue |
| `VenueDetailResponse` | GetVenueById, CreateVenue, UpdateVenue |

`CreateOrReplaceReviewResult` is not in this table - spec 009 already colocated it directly inside
`CreateOrReplaceReview.cs`, since it wraps `ReviewResponse` (shared) but is itself single-use; no
change needed there.

## Decision: `Dtos/Mapping.cs` stays in place, unmoved

**Decision**: The hand-written entity-to-DTO mapping extension methods in `Dtos/Mapping.cs` are
out of scope for this feature - they stay where they are.

**Rationale**: `Mapping.cs` is shared cross-cutting logic (called by many Handlers across many
resources), the same category as `Services/` - not itself a DTO record, and spec FR-001/FR-002
only govern DTO records. Moving mapping logic is a separate concern from moving DTO shapes.

**Gotcha to handle during implementation**: `Mapping.cs` references types from every DTO file
(`ToResponse(this User)` returns `UserResponse`, `ToResponse(this Court)` returns `CourtResponse`,
etc.) - none of the types it returns are moving (all are in the "shared" table above), so no
`using` change is needed there. Confirmed by cross-checking every `Mapping.cs` method's return
type against the classification table.

## Gotcha: endpoint files need new `using` directives for moved DTOs

Each `Endpoints/*.cs` file that binds a moved single-use Request DTO directly as a Minimal API
parameter (for example `VenuesEndpoints.cs` binding `CreateVenueRequest`) needs a `using` for that
DTO's new `Features/<Resource>/<UseCase>/` namespace, in addition to (or instead of, once nothing
else in that file needs `Dtos`) its existing `using SportBook.Application.Dtos;`.
