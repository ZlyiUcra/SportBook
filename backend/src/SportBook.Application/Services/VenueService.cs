using Microsoft.EntityFrameworkCore;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Application.Exceptions;
using SportBook.Domain.Enums;
using SportBook.Infrastructure;

namespace SportBook.Application.Services;

/// <summary>Venue search and detail reads (US1). Owner-side writes arrive with US2.</summary>
public class VenueService(SportBookDbContext db)
{
    public async Task<PagedResponse<VenueSummaryResponse>> SearchAsync(
        string? city, SportType? sportType, PageRequest page, CancellationToken ct)
    {
        var query = db.Venues.AsNoTracking();

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
}
