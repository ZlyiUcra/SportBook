using System.Security.Claims;
using MediatR;
using SportBook.Api.Extensions;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Application.Features.Venues.CreateVenue;
using SportBook.Application.Features.Venues.DeleteVenue;
using SportBook.Application.Features.Venues.GetVenueById;
using SportBook.Application.Features.Venues.SearchNearbyVenues;
using SportBook.Application.Features.Venues.SearchVenues;
using SportBook.Application.Features.Venues.UpdateVenue;
using SportBook.Domain.Enums;

namespace SportBook.Api.Endpoints;

/// <summary>Venue search/detail reads (US1) and owner-only venue writes (US2).</summary>
public static class VenuesEndpoints
{
    /// <summary>Registers the venue search/detail/write endpoints under `api/venues`.</summary>
    public static void MapVenuesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/venues");

        // <summary>
        // Paginated venue search by city and/or sport type; `mine=true` scopes results to the
        // caller's own venues (owner dashboard). `includeNearby` (default false) only applies
        // together with `cityId` and widens results to cities within the fixed 150km radius (spec
        // US4) - the radius itself is not client-configurable.
        // </summary>
        group.MapGet("", async (
            ClaimsPrincipal user, IMediator mediator, [AsParameters] PageRequest paging, CancellationToken ct,
            int? cityId, SportType? sportType, bool includeNearby = false, bool mine = false) =>
        {
            var ownerId = mine ? user.GetUserId() : (Guid?)null;
            var query = new SearchVenuesQuery(cityId, includeNearby, sportType, ownerId, paging);
            var result = await mediator.Send(query, ct);
            return Results.Ok(result);
        });

        // <summary>
        // Venues within the fixed 75km radius of `(lat, lng)`, nearest first (003 contracts/api.md).
        // Contract MUSTs (consilium security verdict): `lat`/`lng` are range-validated (400 on
        // out-of-range) and never persisted or logged - this action must not gain request logging
        // without excluding its query string, same rule as `GET /api/cities/nearest`.
        // </summary>
        group.MapGet("nearby", async (
            decimal lat, decimal lng, SportType? sportType, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new SearchNearbyVenuesQuery(lat, lng, sportType), ct);
            return Results.Ok(result);
        });

        // <summary>A single venue with its courts and aggregate review rating.</summary>
        group.MapGet("{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetVenueByIdQuery(id), ct);
            return Results.Ok(result);
        });

        // <summary>Creates a venue owned by the caller.</summary>
        group.MapPost("", async (
            ClaimsPrincipal user, CreateVenueRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new CreateVenueCommand(
                user.GetUserId(), request.Name, request.CityId, request.Address, request.Description, request.Latitude, request.Longitude);
            var result = await mediator.Send(command, ct);
            return Results.Json(result, statusCode: StatusCodes.Status201Created);
        });

        // <summary>Updates a venue; only its owner may call this (403 otherwise).</summary>
        group.MapPut("{id:guid}", async (
            ClaimsPrincipal user, Guid id, UpdateVenueRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new UpdateVenueCommand(
                user.GetUserId(), id, request.Name, request.CityId, request.Address, request.Description, request.Latitude, request.Longitude);
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        });

        // <summary>Deletes a venue; only its owner may call this, and only while none of its courts have an upcoming, non-cancelled booking (FR-009).</summary>
        group.MapDelete("{id:guid}", async (
            ClaimsPrincipal user, Guid id, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new DeleteVenueCommand(user.GetUserId(), id), ct);
            return Results.NoContent();
        });
    }
}
