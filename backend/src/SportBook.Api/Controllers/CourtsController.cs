using Microsoft.AspNetCore.Mvc;
using SportBook.Api.Extensions;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Application.Services;

namespace SportBook.Api.Controllers;

/// <summary>Court reads for US1 (list by venue) and owner-only court writes for US2.</summary>
[ApiController]
public class CourtsController(CourtService courtService) : ControllerBase
{
    /// <summary>Paginated list of a venue's courts.</summary>
    [HttpGet("api/venues/{venueId:guid}/courts")]
    public async Task<ActionResult<PagedResponse<CourtResponse>>> ListByVenue(
        Guid venueId, [FromQuery] PageRequest page, CancellationToken ct)
    {
        return Ok(await courtService.ListByVenueAsync(venueId, page, ct));
    }

    /// <summary>Creates a court under a venue; only the venue's owner may call this.</summary>
    [HttpPost("api/venues/{venueId:guid}/courts")]
    public async Task<ActionResult<CourtResponse>> Create(Guid venueId, CreateCourtRequest request, CancellationToken ct)
    {
        var result = await courtService.CreateAsync(User.GetUserId(), venueId, request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Updates a court; only the owner of its venue may call this.</summary>
    [HttpPut("api/courts/{id:guid}")]
    public async Task<ActionResult<CourtResponse>> Update(Guid id, UpdateCourtRequest request, CancellationToken ct)
    {
        return Ok(await courtService.UpdateAsync(User.GetUserId(), id, request, ct));
    }

    /// <summary>Deletes a court; only the owner of its venue may call this, and only while it has no upcoming, non-cancelled booking (FR-009).</summary>
    [HttpDelete("api/courts/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await courtService.DeleteAsync(User.GetUserId(), id, ct);
        return NoContent();
    }
}
