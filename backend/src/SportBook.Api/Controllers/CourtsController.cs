using Microsoft.AspNetCore.Mvc;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Application.Services;

namespace SportBook.Api.Controllers;

[ApiController]
public class CourtsController(CourtService courtService) : ControllerBase
{
    [HttpGet("api/venues/{venueId:guid}/courts")]
    public async Task<ActionResult<PagedResponse<CourtResponse>>> ListByVenue(
        Guid venueId, [FromQuery] PageRequest page, CancellationToken ct)
    {
        return Ok(await courtService.ListByVenueAsync(venueId, page, ct));
    }
}
