namespace SportBook.Application.Services;

/// <summary>
/// Pure haversine distance and neighbor-set computation over the in-memory city list
/// (research.md "Coordinate modeling" and "Nearby-cities computation shape"). No spatial types,
/// no database round-trip - the whole workload is "which of ~3-6k reference cities are within
/// 150 km", cheap enough to run in-process and unit-test on the Sqlite provider.
/// </summary>
public static class CityDistance
{
    /// <summary>The fixed nearby-search radius (spec FR-006) - a server-side constant, never a client parameter.</summary>
    public const double NearbyRadiusKm = 150;

    private const double EarthRadiusKm = 6371.0;

    /// <summary>Great-circle distance between two coordinates in kilometers.</summary>
    public static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusKm * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}
