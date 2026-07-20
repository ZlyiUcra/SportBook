using Microsoft.EntityFrameworkCore;
using SportBook.Application.Exceptions;
using SportBook.Infrastructure;

namespace SportBook.Application.Services;

/// <summary>
/// Shared validation for CreateVenue/UpdateVenue - a plain injected collaborator, not its own
/// Command/Query, since it doesn't correspond to an endpoint (consilium 2026-07-20).
/// </summary>
public class VenueLocationValidator(SportBookDbContext db)
{
    /// <summary>
    /// `cityId` must reference an existing city; `latitude`/`longitude` are both-or-neither and,
    /// when present, must be within legal ranges (contracts/api.md Venues section, spec FR-015).
    /// </summary>
    public async Task ValidateAsync(int cityId, decimal? latitude, decimal? longitude, CancellationToken ct)
    {
        if (!await db.Cities.AnyAsync(c => c.Id == cityId, ct))
        {
            throw new ApiException(400, "UNKNOWN_CITY", "cityId does not reference an existing city.");
        }

        if (latitude.HasValue != longitude.HasValue)
        {
            throw new ApiException(400, "INCOMPLETE_COORDINATES", "latitude and longitude must be provided together.");
        }

        if (latitude is < -90 or > 90)
        {
            throw new ApiException(400, "INVALID_LATITUDE", "latitude must be between -90 and 90.");
        }

        if (longitude is < -180 or > 180)
        {
            throw new ApiException(400, "INVALID_LONGITUDE", "longitude must be between -180 and 180.");
        }
    }
}
