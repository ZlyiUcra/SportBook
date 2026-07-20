using MediatR;
using Microsoft.EntityFrameworkCore;
using SportBook.Application.Dtos;
using SportBook.Application.Exceptions;
using SportBook.Application.Services;
using SportBook.Domain.Entities;
using SportBook.Domain.Enums;
using SportBook.Infrastructure;

namespace SportBook.Application.Features.Venues.SearchNearbyVenues;

/// <summary>
/// `GET /api/venues/nearby` (003 data-model.md): venues within <see cref="SearchNearbyVenuesHandler.VenueRadiusKm"/>
/// of `(Lat, Lng)`, nearest first, capped at 100.
/// </summary>
public sealed record SearchNearbyVenuesQuery(decimal Lat, decimal Lng, SportType? SportType)
    : IRequest<IReadOnlyList<NearbyVenueResponse>>;

public sealed class SearchNearbyVenuesHandler(SportBookDbContext db) : IRequestHandler<SearchNearbyVenuesQuery, IReadOnlyList<NearbyVenueResponse>>
{
    /// <summary>
    /// Fixed radius for `GET /api/venues/nearby` (003 research.md "Fixed 75 km radius") - a
    /// server-side constant, never a request parameter. Kept beside its only consumer rather than
    /// on <see cref="CityDistance"/>: that class's 150km `NearbyRadiusKm` is a city-to-city
    /// neighbor radius, a different concept, and the two must stay visually un-confusable.
    /// </summary>
    public const decimal VenueRadiusKm = 75;

    /// <summary>
    /// The only server-side query work is the translatable `Latitude != null` (+ optional sport)
    /// filter (research.md "Distance computation") - distance itself is computed in C# over the
    /// materialized candidates via the existing pure <see cref="CityDistance.DistanceKm"/>, so no
    /// trigonometry is pushed into SQL and the logic stays unit-testable on the Sqlite provider.
    /// </summary>
    public async Task<IReadOnlyList<NearbyVenueResponse>> Handle(SearchNearbyVenuesQuery request, CancellationToken ct)
    {
        if (request.Lat is < -90 or > 90)
        {
            throw new ApiException(400, "INVALID_LATITUDE", "lat must be between -90 and 90.");
        }

        if (request.Lng is < -180 or > 180)
        {
            throw new ApiException(400, "INVALID_LONGITUDE", "lng must be between -180 and 180.");
        }

        IQueryable<Venue> query = db.Venues.AsNoTracking().Include(v => v.City)
            .Where(v => v.Latitude != null);

        if (request.SportType is not null)
        {
            query = query.Where(v => v.Courts.Any(c => c.SportType == request.SportType && c.IsActive));
        }

        var candidates = await query.ToListAsync(ct);

        return candidates
            .Select(v => new { Venue = v, Distance = (decimal)CityDistance.DistanceKm((double)request.Lat, (double)request.Lng, (double)v.Latitude!.Value, (double)v.Longitude!.Value) })
            .Where(x => x.Distance <= VenueRadiusKm)
            .OrderBy(x => x.Distance)
            .Take(100)
            .Select(x => x.Venue.ToNearbyResponse(x.Distance))
            .ToList();
    }
}
