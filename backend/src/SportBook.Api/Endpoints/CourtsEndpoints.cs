using System.Security.Claims;
using SportBook.Api.Extensions;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Application.Services;

namespace SportBook.Api.Endpoints;

/// <summary>Court reads for US1 (list by venue) and owner-only court writes for US2.</summary>
public static class CourtsEndpoints
{
    /// <summary>
    /// Registers the court read/write endpoints - `api/venues/{venueId}/courts` (list/create) and
    /// `api/courts/{id}` (update/delete), two distinct prefixes preserved verbatim from the
    /// pre-Minimal-API routing, not regrouped under one `MapGroup` (consilium 2026-07-20).
    /// </summary>
    public static void MapCourtsEndpoints(this IEndpointRouteBuilder app)
    {
        // <summary>Paginated list of a venue's courts.</summary>
        app.MapGet("api/venues/{venueId:guid}/courts", async (
            Guid venueId, [AsParameters] PageRequest paging, CourtService courtService, CancellationToken ct) =>
        {
            var result = await courtService.ListByVenueAsync(venueId, paging, ct);
            return Results.Ok(result);
        });

        // <summary>Creates a court under a venue; only the venue's owner may call this.</summary>
        app.MapPost("api/venues/{venueId:guid}/courts", async (
            ClaimsPrincipal user, Guid venueId, CreateCourtRequest request, CourtService courtService, CancellationToken ct) =>
        {
            var result = await courtService.CreateAsync(user.GetUserId(), venueId, request, ct);
            return Results.Json(result, statusCode: StatusCodes.Status201Created);
        });

        // <summary>Updates a court; only the owner of its venue may call this.</summary>
        app.MapPut("api/courts/{id:guid}", async (
            ClaimsPrincipal user, Guid id, UpdateCourtRequest request, CourtService courtService, CancellationToken ct) =>
        {
            var result = await courtService.UpdateAsync(user.GetUserId(), id, request, ct);
            return Results.Ok(result);
        });

        // <summary>Deletes a court; only the owner of its venue may call this, and only while it has no upcoming, non-cancelled booking (FR-009).</summary>
        app.MapDelete("api/courts/{id:guid}", async (
            ClaimsPrincipal user, Guid id, CourtService courtService, CancellationToken ct) =>
        {
            await courtService.DeleteAsync(user.GetUserId(), id, ct);
            return Results.NoContent();
        });
    }
}
