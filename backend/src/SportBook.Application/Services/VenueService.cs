using Microsoft.EntityFrameworkCore;
using SportBook.Application.Authorization;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Application.Exceptions;
using SportBook.Domain.Entities;
using SportBook.Domain.Enums;
using SportBook.Infrastructure;

namespace SportBook.Application.Services;

/// <summary>Venue search/detail reads (US1, US4) and owner-only writes (US2).</summary>
public class VenueService(SportBookDbContext db, TimeProvider timeProvider, CityService cityService)
{
    /// <summary>
    /// <paramref name="ownerId"/> is server-derived from the caller's JWT when `mine=true`
    /// (VenuesController), never client-supplied - this is the read side of the owner dashboard
    /// (T051), added alongside it since a "manage my venues" page has no other way to list them.
    /// <paramref name="includeNearby"/> only has an effect together with <paramref name="cityId"/>
    /// (spec US4) - it widens the city filter to the fixed 150km neighbor set, which stays a
    /// server-side constant regardless of what the client requests.
    /// </summary>
    public async Task<PagedResponse<VenueSummaryResponse>> SearchAsync(
        int? cityId, bool includeNearby, SportType? sportType, Guid? ownerId, PageRequest page, CancellationToken ct)
    {
        IQueryable<Venue> query = db.Venues.AsNoTracking().Include(v => v.City);

        if (ownerId is not null)
        {
            query = query.Where(v => v.OwnerId == ownerId);
        }

        if (cityId is not null)
        {
            if (includeNearby)
            {
                var neighborIds = await cityService.GetNeighborIdsAsync(cityId.Value, ct);
                var cityIds = new List<int>(neighborIds) { cityId.Value };
                query = query.Where(v => cityIds.Contains(v.CityId));
            }
            else
            {
                query = query.Where(v => v.CityId == cityId);
            }
        }

        if (sportType is not null)
        {
            query = query.Where(v => v.Courts.Any(c => c.SportType == sportType && c.IsActive));
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderBy(v => v.Name)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(ct);

        return new PagedResponse<VenueSummaryResponse>(
            items.Select(v => v.ToSummaryResponse()).ToList(), page.Page, page.PageSize, totalCount);
    }

    public async Task<VenueDetailResponse> GetByIdAsync(Guid id, CancellationToken ct)
    {
        // Two separate queries instead of sibling collection Includes - closes the consilium
        // cartesian-explosion finding for GET /venues/{id} (research.md).
        var venue = await db.Venues.AsNoTracking()
            .Include(v => v.City)
            .Include(v => v.Courts)
            .SingleOrDefaultAsync(v => v.Id == id, ct)
            ?? throw new ApiException(404, "VENUE_NOT_FOUND", "Venue not found.");

        var ratings = await db.Reviews.AsNoTracking()
            .Where(r => r.VenueId == id)
            .GroupBy(r => r.VenueId)
            .Select(g => new { Average = g.Average(r => (double)r.Rating), Count = g.Count() })
            .SingleOrDefaultAsync(ct);

        return new VenueDetailResponse(
            venue.Id, venue.Name, venue.City!.ToResponse(), venue.Address, venue.Description,
            venue.Latitude, venue.Longitude, venue.OwnerId,
            venue.Courts.OrderBy(c => c.Name).Select(c => c.ToResponse()).ToList(),
            ratings?.Average, ratings?.Count ?? 0);
    }

    /// <summary>Owner is always the authenticated caller - there is no `ownerId` field on the request.</summary>
    public async Task<VenueDetailResponse> CreateAsync(Guid ownerId, CreateVenueRequest request, CancellationToken ct)
    {
        await ValidateLocationAsync(request.CityId, request.Latitude, request.Longitude, ct);

        var venue = new Venue
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = request.Name,
            CityId = request.CityId,
            Address = request.Address,
            Description = request.Description,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };

        db.Venues.Add(venue);
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(venue.Id, ct);
    }

    public async Task<VenueDetailResponse> UpdateAsync(Guid ownerId, Guid venueId, UpdateVenueRequest request, CancellationToken ct)
    {
        var venue = await db.Venues.SingleOrDefaultAsync(v => v.Id == venueId, ct)
            ?? throw new ApiException(404, "VENUE_NOT_FOUND", "Venue not found.");
        OwnershipChecks.EnsureVenueOwner(venue, ownerId);
        await ValidateLocationAsync(request.CityId, request.Latitude, request.Longitude, ct);

        venue.Name = request.Name;
        venue.CityId = request.CityId;
        venue.Address = request.Address;
        venue.Description = request.Description;
        venue.Latitude = request.Latitude;
        venue.Longitude = request.Longitude;
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(venueId, ct);
    }

    /// <summary>
    /// `cityId` must reference an existing city; `latitude`/`longitude` are both-or-neither and,
    /// when present, must be within legal ranges (contracts/api.md Venues section, spec FR-015).
    /// </summary>
    private async Task ValidateLocationAsync(int cityId, decimal? latitude, decimal? longitude, CancellationToken ct)
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

    /// <summary>
    /// Blocked while any court of this venue has an upcoming, non-cancelled booking (spec FR-009).
    /// Once allowed, the delete cascades to the venue's courts, their bookings, and its reviews -
    /// no separate cleanup step, since nothing upcoming survives the guard above.
    /// </summary>
    public async Task DeleteAsync(Guid ownerId, Guid venueId, CancellationToken ct)
    {
        var venue = await db.Venues.SingleOrDefaultAsync(v => v.Id == venueId, ct)
            ?? throw new ApiException(404, "VENUE_NOT_FOUND", "Venue not found.");
        OwnershipChecks.EnsureVenueOwner(venue, ownerId);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var hasUpcomingBookings = await db.Bookings.AnyAsync(b =>
            b.Court!.VenueId == venueId && b.Status != BookingStatus.Cancelled && b.StartTime > now, ct);
        if (hasUpcomingBookings)
        {
            throw new ApiException(409, "HAS_UPCOMING_BOOKINGS", "Cannot delete a venue with upcoming, non-cancelled bookings.");
        }

        db.Venues.Remove(venue);
        await db.SaveChangesAsync(ct);
    }
}
