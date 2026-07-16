using Microsoft.AspNetCore.Mvc;
using SportBook.Api.Extensions;
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

    [HttpPost("api/venues/{venueId:guid}/courts")]
    public async Task<ActionResult<CourtResponse>> Create(Guid venueId, CreateCourtRequest request, CancellationToken ct)
    {
        var result = await courtService.CreateAsync(User.GetUserId(), venueId, request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("api/courts/{id:guid}")]
    public async Task<ActionResult<CourtResponse>> Update(Guid id, UpdateCourtRequest request, CancellationToken ct)
    {
        return Ok(await courtService.UpdateAsync(User.GetUserId(), id, request, ct));
    }

    [HttpDelete("api/courts/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await courtService.DeleteAsync(User.GetUserId(), id, ct);
        return NoContent();
    }
}
