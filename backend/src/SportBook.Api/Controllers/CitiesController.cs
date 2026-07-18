using Microsoft.AspNetCore.Mvc;
using SportBook.Application.Dtos;
using SportBook.Application.Services;

namespace SportBook.Api.Controllers;

/// <summary>City directory reads: suggestion search (US1) and nearest-city resolution (US3).</summary>
[ApiController]
[Route("api/cities")]
public class CitiesController(CityService cityService) : ControllerBase
{
    /// <summary>Suggests up to 10 directory cities matching `query` (min 2 chars) in any app language, ranked by population.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CityResponse>>> Suggest([FromQuery] string query, CancellationToken ct)
    {
        return Ok(await cityService.SuggestAsync(query, ct));
    }

    /// <summary>
    /// Resolves the nearest directory city to a device position. Contract MUSTs (research.md
    /// Geolocation privacy posture): `lat`/`lng` are range-validated and never persisted or
    /// logged - this action must not gain request logging without excluding its query string.
    /// </summary>
    [HttpGet("nearest")]
    public async Task<ActionResult<CityResponse>> Nearest([FromQuery] decimal lat, [FromQuery] decimal lng, CancellationToken ct)
    {
        return Ok(await cityService.FindNearestAsync(lat, lng, ct));
    }
}
