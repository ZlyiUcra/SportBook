using Microsoft.AspNetCore.Mvc;
using SportBook.Api.Extensions;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Application.Services;

namespace SportBook.Api.Controllers;

/// <summary>Booking lifecycle: create, cancel, list, and (for venue owners) list-by-venue and confirm.</summary>
[ApiController]
[Route("api/bookings")]
public class BookingsController(BookingService bookingService) : ControllerBase
{
    /// <summary>Books a court for a whole-hour slot; price and overlap safety are computed server-side.</summary>
    [HttpPost]
    public async Task<ActionResult<BookingResponse>> Create(CreateBookingRequest request, CancellationToken ct)
    {
        var result = await bookingService.CreateAsync(User.GetUserId(), request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Paginated list of the caller's own bookings. `status` (default All) filters by
    /// All/Upcoming/Completed/Cancelled server-side, before paging, so it holds across pages (005
    /// spec FR-006); the owner venue-bookings endpoint does not take this filter.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<BookingResponse>>> ListMine(
        [FromQuery] BookingStatusFilter status, [FromQuery] PageRequest paging, CancellationToken ct)
    {
        return Ok(await bookingService.ListMineAsync(User.GetUserId(), status, paging, ct));
    }

    /// <summary>A single booking; only the customer who made it may view it (403 otherwise).</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookingResponse>> GetById(Guid id, CancellationToken ct)
    {
        return Ok(await bookingService.GetByIdAsync(User.GetUserId(), id, ct));
    }

    /// <summary>Cancels a booking; only its customer may call this, and only more than 2 hours before its start (FR-005).</summary>
    [HttpPut("{id:guid}/cancel")]
    public async Task<ActionResult<BookingResponse>> Cancel(Guid id, CancellationToken ct)
    {
        return Ok(await bookingService.CancelAsync(User.GetUserId(), id, ct));
    }

    /// <summary>Paginated list of bookings against one of the caller's own venues.</summary>
    [HttpGet("/api/venues/{venueId:guid}/bookings")]
    public async Task<ActionResult<PagedResponse<BookingResponse>>> ListByVenue(
        Guid venueId, [FromQuery] PageRequest paging, CancellationToken ct)
    {
        return Ok(await bookingService.ListByVenueForOwnerAsync(User.GetUserId(), venueId, paging, ct));
    }

    /// <summary>Confirms a pending booking; only the owner of the booked court's venue may call this (FR-011).</summary>
    [HttpPut("{id:guid}/confirm")]
    public async Task<ActionResult<BookingResponse>> Confirm(Guid id, CancellationToken ct)
    {
        return Ok(await bookingService.ConfirmAsync(User.GetUserId(), id, ct));
    }
}
