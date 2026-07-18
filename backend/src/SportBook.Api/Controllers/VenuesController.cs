using Microsoft.AspNetCore.Mvc;
using SportBook.Api.Extensions;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Application.Services;
using SportBook.Domain.Enums;

namespace SportBook.Api.Controllers;

/// <summary>Venue search/detail reads (US1) and owner-only venue writes (US2).</summary>
[ApiController]
[Route("api/venues")]
public class VenuesController(VenueService venueService) : ControllerBase
{
    /// <summary>
    /// Paginated venue search by city and/or sport type; `mine=true` scopes results to the
    /// caller's own venues (owner dashboard). `includeNearby` (default false) only applies
    /// together with `cityId` and widens results to cities within the fixed 150km radius (spec
    /// US4) - the radius itself is not client-configurable.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<VenueSummaryResponse>>> List(
        [FromQuery] int? cityId, [FromQuery] bool includeNearby, [FromQuery] SportType? sportType,
        [FromQuery] bool mine, [FromQuery] PageRequest page, CancellationToken ct)
    {
        var ownerId = mine ? User.GetUserId() : (Guid?)null;
        return Ok(await venueService.SearchAsync(cityId, includeNearby, sportType, ownerId, page, ct));
    }

    /// <summary>A single venue with its courts and aggregate review rating.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VenueDetailResponse>> GetById(Guid id, CancellationToken ct)
    {
        return Ok(await venueService.GetByIdAsync(id, ct));
    }

    /// <summary>Creates a venue owned by the caller.</summary>
    [HttpPost]
    public async Task<ActionResult<VenueDetailResponse>> Create(CreateVenueRequest request, CancellationToken ct)
    {
        var result = await venueService.CreateAsync(User.GetUserId(), request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Updates a venue; only its owner may call this (403 otherwise).</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<VenueDetailResponse>> Update(Guid id, UpdateVenueRequest request, CancellationToken ct)
    {
        return Ok(await venueService.UpdateAsync(User.GetUserId(), id, request, ct));
    }

    /// <summary>Deletes a venue; only its owner may call this, and only while none of its courts have an upcoming, non-cancelled booking (FR-009).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await venueService.DeleteAsync(User.GetUserId(), id, ct);
        return NoContent();
    }
}
