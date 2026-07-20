using System.Security.Claims;
using SportBook.Api.Extensions;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Application.Services;
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
            ClaimsPrincipal user, VenueService venueService, [AsParameters] PageRequest paging, CancellationToken ct,
            int? cityId, SportType? sportType, bool includeNearby = false, bool mine = false) =>
        {
            var ownerId = mine ? user.GetUserId() : (Guid?)null;
            var result = await venueService.SearchAsync(cityId, includeNearby, sportType, ownerId, paging, ct);
            return Results.Ok(result);
        });

        // <summary>
        // Venues within the fixed <see cref="VenueService.VenueRadiusKm"/> of `(lat, lng)`, nearest
        // first (003 contracts/api.md). Contract MUSTs (consilium security verdict): `lat`/`lng` are
        // range-validated (VenueService, 400 on out-of-range) and never persisted or logged - this
        // action must not gain request logging without excluding its query string, same rule as
        // `GET /api/cities/nearest`.
        // </summary>
        group.MapGet("nearby", async (
            decimal lat, decimal lng, SportType? sportType, VenueService venueService, CancellationToken ct) =>
        {
            var result = await venueService.SearchNearbyAsync(lat, lng, sportType, ct);
            return Results.Ok(result);
        });

        // <summary>A single venue with its courts and aggregate review rating.</summary>
        group.MapGet("{id:guid}", async (Guid id, VenueService venueService, CancellationToken ct) =>
        {
            var result = await venueService.GetByIdAsync(id, ct);
            return Results.Ok(result);
        });

        // <summary>Creates a venue owned by the caller.</summary>
        group.MapPost("", async (
            ClaimsPrincipal user, CreateVenueRequest request, VenueService venueService, CancellationToken ct) =>
        {
            var result = await venueService.CreateAsync(user.GetUserId(), request, ct);
            return Results.Json(result, statusCode: StatusCodes.Status201Created);
        });

        // <summary>Updates a venue; only its owner may call this (403 otherwise).</summary>
        group.MapPut("{id:guid}", async (
            ClaimsPrincipal user, Guid id, UpdateVenueRequest request, VenueService venueService, CancellationToken ct) =>
        {
            var result = await venueService.UpdateAsync(user.GetUserId(), id, request, ct);
            return Results.Ok(result);
        });

        // <summary>Deletes a venue; only its owner may call this, and only while none of its courts have an upcoming, non-cancelled booking (FR-009).</summary>
        group.MapDelete("{id:guid}", async (
            ClaimsPrincipal user, Guid id, VenueService venueService, CancellationToken ct) =>
        {
            await venueService.DeleteAsync(user.GetUserId(), id, ct);
            return Results.NoContent();
        });
    }
}
