using Microsoft.EntityFrameworkCore;
using SportBook.Application.Dtos;
using SportBook.Application.Exceptions;
using SportBook.Infrastructure;

namespace SportBook.Application.Services;

/// <summary>
/// Shared venue-detail read, used by the GetVenueById query handler AND (after a mutation) by
/// CreateVenue/UpdateVenue to build their response - a plain injected collaborator, not a nested
/// mediator dispatch, since a handler calling `mediator.Send` on another handler is an anti-
/// pattern that breaks pipeline-behavior assumptions (consilium 2026-07-20).
/// </summary>
public class VenueDetailReader(SportBookDbContext db)
{
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
}
