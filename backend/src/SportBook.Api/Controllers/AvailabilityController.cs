using Microsoft.AspNetCore.Mvc;
using SportBook.Application.Dtos;
using SportBook.Application.Services;

namespace SportBook.Api.Controllers;

[ApiController]
public class AvailabilityController(AvailabilityService availabilityService) : ControllerBase
{
    [HttpGet("api/courts/{id:guid}/availability")]
    public async Task<ActionResult<AvailabilityResponse>> Get(
        Guid id, [FromQuery] DateOnly date, CancellationToken ct)
    {
        return Ok(await availabilityService.GetForDateAsync(id, date, ct));
    }
}
