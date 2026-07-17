using Microsoft.AspNetCore.Mvc;
using SportBook.Application.Dtos;
using SportBook.Application.Services;

namespace SportBook.Api.Controllers;

/// <summary>Free-slot lookup for a single court and day, used by the booking form's time picker.</summary>
[ApiController]
public class AvailabilityController(AvailabilityService availabilityService) : ControllerBase
{
    /// <summary>Whole-hour free slots for a court on a given date, within its operating hours.</summary>
    [HttpGet("api/courts/{id:guid}/availability")]
    public async Task<ActionResult<AvailabilityResponse>> Get(
        Guid id, [FromQuery] DateOnly date, CancellationToken ct)
    {
        return Ok(await availabilityService.GetForDateAsync(id, date, ct));
    }
}
