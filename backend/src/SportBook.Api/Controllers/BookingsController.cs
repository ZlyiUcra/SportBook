using Microsoft.AspNetCore.Mvc;
using SportBook.Api.Extensions;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Application.Services;

namespace SportBook.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingsController(BookingService bookingService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<BookingResponse>> Create(CreateBookingRequest request, CancellationToken ct)
    {
        var result = await bookingService.CreateAsync(User.GetUserId(), request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<BookingResponse>>> ListMine(
        [FromQuery] PageRequest page, CancellationToken ct)
    {
        return Ok(await bookingService.ListMineAsync(User.GetUserId(), page, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookingResponse>> GetById(Guid id, CancellationToken ct)
    {
        return Ok(await bookingService.GetByIdAsync(User.GetUserId(), id, ct));
    }

    [HttpPut("{id:guid}/cancel")]
    public async Task<ActionResult<BookingResponse>> Cancel(Guid id, CancellationToken ct)
    {
        return Ok(await bookingService.CancelAsync(User.GetUserId(), id, ct));
    }
}
