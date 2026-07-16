using Microsoft.EntityFrameworkCore;
using SportBook.Application.Authorization;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Application.Exceptions;
using SportBook.Domain.Entities;
using SportBook.Domain.Enums;
using SportBook.Infrastructure;

namespace SportBook.Application.Services;

/// <summary>Venue search/detail reads (US1) and owner-only writes (US2).</summary>
public class VenueService(SportBookDbContext db, TimeProvider timeProvider)
{
    /// <summary>
    /// <paramref name="ownerId"/> is server-derived from the caller's JWT when `mine=true`
    /// (VenuesController), never client-supplied - this is the read side of the owner dashboard
    /// (T051), added alongside it since a "manage my venues" page has no other way to list them.
    /// </summary>
    public async Task<PagedResponse<VenueSummaryResponse>> SearchAsync(
        string? city, SportType? sportType, Guid? ownerId, PageRequest page, CancellationToken ct)
    {
        var query = db.Venues.AsNoTracking();

        if (ownerId is not null)
        {
            query = query.Where(v => v.OwnerId == ownerId);
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(v => v.City == city);
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
            .Select(v => new VenueSummaryResponse(v.Id, v.Name, v.City, v.Address, v.Description))
            .ToListAsync(ct);

        return new PagedResponse<VenueSummaryResponse>(items, page.Page, page.PageSize, totalCount);
    }

    public async Task<VenueDetailResponse> GetByIdAsync(Guid id, CancellationToken ct)
    {
        // Two separate queries instead of sibling collection Includes - closes the consilium
        // cartesian-explosion finding for GET /venues/{id} (research.md).
        var venue = await db.Venues.AsNoTracking()
            .Include(v => v.Courts)
            .SingleOrDefaultAsync(v => v.Id == id, ct)
            ?? throw new ApiException(404, "VENUE_NOT_FOUND", "Venue not found.");

        var ratings = await db.Reviews.AsNoTracking()
            .Where(r => r.VenueId == id)
            .GroupBy(r => r.VenueId)
            .Select(g => new { Average = g.Average(r => (double)r.Rating), Count = g.Count() })
            .SingleOrDefaultAsync(ct);

        return new VenueDetailResponse(
            venue.Id, venue.Name, venue.City, venue.Address, venue.Description, venue.OwnerId,
            venue.Courts.OrderBy(c => c.Name).Select(c => c.ToResponse()).ToList(),
            ratings?.Average, ratings?.Count ?? 0);
    }

    /// <summary>Owner is always the authenticated caller - there is no `ownerId` field on the request.</summary>
    public async Task<VenueDetailResponse> CreateAsync(Guid ownerId, CreateVenueRequest request, CancellationToken ct)
    {
        var venue = new Venue
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = request.Name,
            City = request.City,
            Address = request.Address,
            Description = request.Description,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };

        db.Venues.Add(venue);
        await db.SaveChangesAsync(ct);

        return new VenueDetailResponse(venue.Id, venue.Name, venue.City, venue.Address, venue.Description,
            venue.OwnerId, [], null, 0);
    }

    public async Task<VenueDetailResponse> UpdateAsync(Guid ownerId, Guid venueId, UpdateVenueRequest request, CancellationToken ct)
    {
        var venue = await db.Venues.SingleOrDefaultAsync(v => v.Id == venueId, ct)
            ?? throw new ApiException(404, "VENUE_NOT_FOUND", "Venue not found.");
        OwnershipChecks.EnsureVenueOwner(venue, ownerId);

        venue.Name = request.Name;
        venue.City = request.City;
        venue.Address = request.Address;
        venue.Description = request.Description;
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(venueId, ct);
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
