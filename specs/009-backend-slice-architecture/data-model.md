# Data Model: Backend rearchitecture to vertical slice architecture

This feature restructures code organization, not persisted data - no database schema, migration,
or entity changed. The "entities" below are the structural/code-organization units named in
spec.md's Key Entities section, mapped onto their real, shipped shape.

## Endpoint registration

**Represents**: The HTTP-facing declaration of one route.

**Real shape**: A `public static void MapXxxEndpoints(this IEndpointRouteBuilder app)` extension
method in `backend/src/SportBook.Api/Endpoints/XxxEndpoints.cs` - one file per resource (8
total: Auth, Availability, Bookings, Cities, Courts, Reviews, Users, Venues). Each individual
route inside it is one `app.MapGet/MapPost/MapPut/MapDelete(...)` call whose lambda body is at
most: extract the caller's id from `ClaimsPrincipal` (if needed), construct a Command/Query,
`await mediator.Send(...)`, and shape the `IResult`. No database access, no validation, no
business rule anywhere in this layer (spec FR-001, User Story 3).

**Fields/attributes**: route template (string), HTTP verb, auth requirement (`.AllowAnonymous()`
present or absent - absent means the global `RequireAuthenticatedUser()` fallback policy applies).

## Request/response pair

**Represents**: The wire shape flowing into and out of one action.

**Real shape**: For GET/query actions, the request is usually the Query record itself
(`SuggestCitiesQuery(string Query)`); for POST/PUT actions with a route id alongside a JSON body,
a small body-only DTO in `Dtos/` (e.g. `CreateCourtRequest`) is still bound from the body and the
endpoint combines it with the route id into the Command. Response shapes are the pre-existing
`Dtos/` response records (`BookingResponse`, `VenueDetailResponse`, etc.) - byte-identical to
before this rework (spec FR-002/FR-007).

**Invariant**: No field name, field type, or JSON structure changed for any existing endpoint.

## Self-contained unit (action)

**Represents**: One specific thing the backend can be asked to do.

**Real shape**: A `Features/<Resource>/<UseCase>/<UseCase>.cs` file holding exactly two types: a
`sealed record XxxCommand`/`XxxQuery` implementing `IRequest<TResponse>` (or plain `IRequest` for
a void action), and a `sealed class XxxHandler` implementing `IRequestHandler<TRequest,
TResponse>` with one `Handle` method. 26 of these exist, one per client-reachable action (spec
FR-003, SC-002):

| Resource | Actions |
|---|---|
| Auth | Register, Login, Refresh, Logout |
| Availability | GetAvailability |
| Bookings | CreateBooking, CancelBooking, GetBookingById, ListMyBookings, ListVenueBookingsForOwner, ConfirmBooking |
| Cities | SuggestCities, FindNearestCity |
| Courts | ListCourtsByVenue, CreateCourt, UpdateCourt, DeleteCourt |
| Reviews | ListReviewsByVenue, CreateOrReplaceReview |
| Users | GetMe |
| Venues | SearchVenues, SearchNearbyVenues, GetVenueById, CreateVenue, UpdateVenue, DeleteVenue |

**Dispatch**: Every endpoint reaches its unit through exactly one call shape -
`await mediator.Send(request, ct)` - never a directly-typed method call (spec FR-004).

**Testability**: Each Handler is independently constructible and callable in a unit test without
an HTTP round-trip (spec FR-008) - existing unit tests were updated to construct `XxxHandler`
directly rather than the pre-existing service classes they used to call.

## Shared component

**Represents**: Logic genuinely needed by more than one action, deliberately kept outside the
one-unit-per-action shape (spec FR-006).

**Real shape**: Plain classes/static classes in `backend/src/SportBook.Application/Services/`,
constructor-injected (or called as static methods) directly by the Handlers that need them -
never dispatched through the mediator:

| Component | Shared by |
|---|---|
| `OwnershipChecks` (pre-existing, unchanged) | Booking, Court, Venue Handlers (9 call sites) |
| `AuthTokenIssuer` | Register, Login, Refresh Handlers |
| `VenueDetailReader` | GetVenueById, CreateVenue, UpdateVenue Handlers |
| `VenueLocationValidator` | CreateVenue, UpdateVenue Handlers |
| `BookingHelpers` (static) | All 6 Booking Handlers |
| `CityService` (slimmed to `GetNeighborIdsAsync` only) | SearchVenues Handler |
| `CityDirectoryCache`, `CityDistance` (pre-existing, unchanged) | City- and Venue-related Handlers |

None of these answer an HTTP endpoint of their own - each is a plain dependency of one or more
Handlers, not a 27th slice.
