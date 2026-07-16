using Microsoft.AspNetCore.Mvc;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Application.Services;
using SportBook.Domain.Enums;

namespace SportBook.Api.Controllers;

[ApiController]
[Route("api/venues")]
public class VenuesController(VenueService venueService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<VenueSummaryResponse>>> List(
        [FromQuery] string? city, [FromQuery] SportType? sportType, [FromQuery] PageRequest page, CancellationToken ct)
    {
        return Ok(await venueService.SearchAsync(city, sportType, page, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VenueDetailResponse>> GetById(Guid id, CancellationToken ct)
    {
        return Ok(await venueService.GetByIdAsync(id, ct));
    }
}
