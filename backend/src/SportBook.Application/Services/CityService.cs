using SportBook.Application.Dtos;
using SportBook.Application.Exceptions;
using SportBook.Domain.Entities;

namespace SportBook.Application.Services;

/// <summary>
/// City directory reads: suggestion search (US1), nearest-city resolution (US3), and
/// nearby-city expansion (US4). Backed by <see cref="CityDirectoryCache"/> (a singleton) so the
/// ~3-6k-row reference directory is loaded once per process, not once per request.
/// </summary>
public class CityService(CityDirectoryCache cache)
{
    /// <summary>Case-insensitive substring match against all three localized name columns - typing in any app language finds the city.</summary>
    private static bool MatchesQuery(City city, string query) =>
        city.NameEn.Contains(query, StringComparison.OrdinalIgnoreCase) ||
        city.NameUk.Contains(query, StringComparison.OrdinalIgnoreCase) ||
        city.NamePt.Contains(query, StringComparison.OrdinalIgnoreCase);

    /// <summary>Min 2 chars (else 400); matches any localized name column; TOP 10 ordered by population DESC (contracts/api.md Cities section).</summary>
    public async Task<IReadOnlyList<CityResponse>> SuggestAsync(string query, CancellationToken ct)
    {
        if (query.Length < 2)
        {
            throw new ApiException(400, "QUERY_TOO_SHORT", "query must be at least 2 characters.");
        }

        var cities = await cache.GetAllAsync(ct);
        return Rank(cities, query);
    }

    /// <summary>
    /// Pure ranking step of <see cref="SuggestAsync"/>, split out so suggestion ranking and
    /// localized-name matching are unit-testable without a database (T014).
    /// </summary>
    public static IReadOnlyList<CityResponse> Rank(IEnumerable<City> cities, string query, int limit = 10) =>
        cities
            .Where(c => MatchesQuery(c, query))
            .OrderByDescending(c => c.Population)
            .Take(limit)
            .Select(c => c.ToResponse())
            .ToList();

    /// <summary>Resolves the nearest directory city to a device position (US3); the server never persists or logs the received coordinates.</summary>
    public async Task<CityResponse> FindNearestAsync(decimal lat, decimal lng, CancellationToken ct)
    {
        if (lat is < -90 or > 90)
        {
            throw new ApiException(400, "INVALID_LATITUDE", "lat must be between -90 and 90.");
        }

        if (lng is < -180 or > 180)
        {
            throw new ApiException(400, "INVALID_LONGITUDE", "lng must be between -180 and 180.");
        }

        var cities = await cache.GetAllAsync(ct);
        var nearest = cities
            .Select(c => new { City = c, Distance = CityDistance.DistanceKm((double)lat, (double)lng, (double)c.Latitude, (double)c.Longitude) })
            .OrderBy(x => x.Distance)
            .First();

        return nearest.City.ToResponse();
    }

    /// <summary>
    /// IDs of directory cities within <see cref="CityDistance.NearbyRadiusKm"/> of the given
    /// city, excluding the city itself - callers combine this with the selected city's own ID
    /// (VenueService.Search's `includeNearby` filter, spec US4).
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
