using Mediator;
using SportBook.Application.Dtos;
using SportBook.Application.Exceptions;
using SportBook.Application.Services;

namespace SportBook.Application.Features.Cities.FindNearestCity;

/// <summary>Resolves the nearest directory city to a device position (US3).</summary>
public sealed record FindNearestCityQuery(decimal Lat, decimal Lng) : IRequest<CityResponse>;

/// <summary>The server never persists or logs the received coordinates (contract MUST).</summary>
public sealed class FindNearestCityHandler(CityDirectoryCache cache) : IRequestHandler<FindNearestCityQuery, CityResponse>
{
    public async ValueTask<CityResponse> Handle(FindNearestCityQuery request, CancellationToken ct)
    {
        if (request.Lat is < -90 or > 90)
        {
            throw new ApiException(400, "INVALID_LATITUDE", "lat must be between -90 and 90.");
        }

        if (request.Lng is < -180 or > 180)
        {
            throw new ApiException(400, "INVALID_LONGITUDE", "lng must be between -180 and 180.");
        }

        var cities = await cache.GetAllAsync(ct);
        var nearest = cities
            .Select(c => new { City = c, Distance = CityDistance.DistanceKm((double)request.Lat, (double)request.Lng, (double)c.Latitude, (double)c.Longitude) })
            .OrderBy(x => x.Distance)
            .First();

        return nearest.City.ToResponse();
    }
}
