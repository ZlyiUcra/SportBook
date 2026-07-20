using System.Security.Claims;
using Mediator;
using SportBook.Api.Extensions;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Application.Features.Courts.CreateCourt;
using SportBook.Application.Features.Courts.DeleteCourt;
using SportBook.Application.Features.Courts.ListCourtsByVenue;
using SportBook.Application.Features.Courts.UpdateCourt;

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
            Guid venueId, [AsParameters] PageRequest paging, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ListCourtsByVenueQuery(venueId, paging), ct);
            return Results.Ok(result);
        });

        // <summary>Creates a court under a venue; only the venue's owner may call this.</summary>
        app.MapPost("api/venues/{venueId:guid}/courts", async (
            ClaimsPrincipal user, Guid venueId, CreateCourtRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new CreateCourtCommand(
                user.GetUserId(), venueId, request.Name, request.SportType, request.PricePerHour, request.OpeningTime, request.ClosingTime);
            var result = await mediator.Send(command, ct);
            return Results.Json(result, statusCode: StatusCodes.Status201Created);
        });

        // <summary>Updates a court; only the owner of its venue may call this.</summary>
        app.MapPut("api/courts/{id:guid}", async (
            ClaimsPrincipal user, Guid id, UpdateCourtRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new UpdateCourtCommand(
                user.GetUserId(), id, request.Name, request.SportType, request.PricePerHour,
                request.OpeningTime, request.ClosingTime, request.IsActive);
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        });

        // <summary>Deletes a court; only the owner of its venue may call this, and only while it has no upcoming, non-cancelled booking (FR-009).</summary>
        app.MapDelete("api/courts/{id:guid}", async (
            ClaimsPrincipal user, Guid id, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new DeleteCourtCommand(user.GetUserId(), id), ct);
            return Results.NoContent();
        });
    }
}
