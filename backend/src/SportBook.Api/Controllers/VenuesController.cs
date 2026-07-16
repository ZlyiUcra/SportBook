using Microsoft.AspNetCore.Mvc;
using SportBook.Api.Extensions;
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

    [HttpPost]
    public async Task<ActionResult<VenueDetailResponse>> Create(CreateVenueRequest request, CancellationToken ct)
    {
        var result = await venueService.CreateAsync(User.GetUserId(), request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<VenueDetailResponse>> Update(Guid id, UpdateVenueRequest request, CancellationToken ct)
    {
        return Ok(await venueService.UpdateAsync(User.GetUserId(), id, request, ct));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await venueService.DeleteAsync(User.GetUserId(), id, ct);
        return NoContent();
    }
}
