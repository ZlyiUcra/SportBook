using SportBook.Application.Exceptions;

namespace SportBook.Application.Services;

/// <summary>
/// Internal collaborator used by SearchVenuesHandler's `includeNearby` filter (spec US4) -
/// not endpoint-facing itself (city suggestion/nearest-city reads live in
/// Features/Cities/SuggestCities and Features/Cities/FindNearestCity), so it stays a plain
/// injected service rather than a Command/Query (consilium 2026-07-20: inter-service calls that
/// don't correspond to an endpoint are not wrapped in the mediator). Backed by
/// <see cref="CityDirectoryCache"/> (a singleton) so the ~5k-row reference directory is loaded
/// once per process, not once per request.
/// </summary>
public class CityService(CityDirectoryCache cache)
{
    /// <summary>
    /// IDs of directory cities within <see cref="CityDistance.NearbyRadiusKm"/> of the given
    /// city, excluding the city itself - callers combine this with the selected city's own ID
    /// (SearchVenuesHandler's `includeNearby` filter, spec US4).
    /// </summary>
    public async Task<IReadOnlyList<int>> GetNeighborIdsAsync(int cityId, CancellationToken ct)
    {
        var cities = await cache.GetAllAsync(ct);
        var origin = cities.SingleOrDefault(c => c.Id == cityId)
            ?? throw new ApiException(400, "UNKNOWN_CITY", "cityId does not reference an existing city.");

        return cache.GetOrAddNeighborIds(cityId, _ => cities
            .Where(c => c.Id != origin.Id)
            .Where(c => CityDistance.DistanceKm((double)origin.Latitude, (double)origin.Longitude, (double)c.Latitude, (double)c.Longitude) <= CityDistance.NearbyRadiusKm)
            .Select(c => c.Id)
            .ToList());
    }
}
