using System.Security.Claims;
using SportBook.Api.Extensions;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Application.Services;

namespace SportBook.Api.Endpoints;

/// <summary>Booking lifecycle: create, cancel, list, and (for venue owners) list-by-venue and confirm.</summary>
public static class BookingsEndpoints
{
    /// <summary>
    /// Registers the booking lifecycle endpoints under `api/bookings`, plus the owner-facing
    /// `api/venues/{venueId}/bookings` list which lives outside that prefix (preserved verbatim
    /// from the pre-Minimal-API routing, not regrouped - consilium 2026-07-20).
    /// </summary>
    public static void MapBookingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/bookings");

        // <summary>Books a court for a whole-hour slot; price and overlap safety are computed server-side.</summary>
        group.MapPost("", async (
            ClaimsPrincipal user, CreateBookingRequest request, BookingService bookingService, CancellationToken ct) =>
        {
            var result = await bookingService.CreateAsync(user.GetUserId(), request, ct);
            return Results.Json(result, statusCode: StatusCodes.Status201Created);
        });

        // <summary>
        // Paginated list of the caller's own bookings. `status` (default All) filters by
        // All/Upcoming/Completed/Cancelled server-side, before paging, so it holds across pages (005
        // spec FR-006); the owner venue-bookings endpoint does not take this filter.
        // </summary>
        group.MapGet("", async (
            ClaimsPrincipal user, [AsParameters] PageRequest paging, BookingService bookingService,
            CancellationToken ct, BookingStatusFilter status = BookingStatusFilter.All) =>
        {
            var result = await bookingService.ListMineAsync(user.GetUserId(), status, paging, ct);
            return Results.Ok(result);
        });

        // <summary>A single booking; only the customer who made it may view it (403 otherwise).</summary>
        group.MapGet("{id:guid}", async (
            ClaimsPrincipal user, Guid id, BookingService bookingService, CancellationToken ct) =>
        {
            var result = await bookingService.GetByIdAsync(user.GetUserId(), id, ct);
            return Results.Ok(result);
        });

        // <summary>Cancels a booking; only its customer may call this, and only more than 2 hours before its start (FR-005).</summary>
        group.MapPut("{id:guid}/cancel", async (
            ClaimsPrincipal user, Guid id, BookingService bookingService, CancellationToken ct) =>
        {
            var result = await bookingService.CancelAsync(user.GetUserId(), id, ct);
            return Results.Ok(result);
        });

        // <summary>Paginated list of bookings against one of the caller's own venues.</summary>
        app.MapGet("api/venues/{venueId:guid}/bookings", async (
            ClaimsPrincipal user, Guid venueId, [AsParameters] PageRequest paging,
            BookingService bookingService, CancellationToken ct) =>
        {
            var result = await bookingService.ListByVenueForOwnerAsync(user.GetUserId(), venueId, paging, ct);
            return Results.Ok(result);
        });

        // <summary>Confirms a pending booking; only the owner of the booked court's venue may call this (FR-011).</summary>
        group.MapPut("{id:guid}/confirm", async (
            ClaimsPrincipal user, Guid id, BookingService bookingService, CancellationToken ct) =>
        {
            var result = await bookingService.ConfirmAsync(user.GetUserId(), id, ct);
            return Results.Ok(result);
        });
    }
}
