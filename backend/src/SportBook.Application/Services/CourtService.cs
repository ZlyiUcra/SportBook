using Microsoft.EntityFrameworkCore;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Application.Exceptions;
using SportBook.Infrastructure;

namespace SportBook.Application.Services;

/// <summary>Court reads for US1 (list by venue). Owner-side writes arrive with US2.</summary>
public class CourtService(SportBookDbContext db)
{
    public async Task<PagedResponse<CourtResponse>> ListByVenueAsync(Guid venueId, PageRequest page, CancellationToken ct)
    {
        if (!await db.Venues.AnyAsync(v => v.Id == venueId, ct))
        {
            throw new ApiException(404, "VENUE_NOT_FOUND", "Venue not found.");
        }

        var query = db.Courts.AsNoTracking().Where(c => c.VenueId == venueId);
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderBy(c => c.Name)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(c => new CourtResponse(c.Id, c.VenueId, c.Name, c.SportType.ToString(),
                c.PricePerHour, c.OpeningTime, c.ClosingTime, c.IsActive))
            .ToListAsync(ct);

        return new PagedResponse<CourtResponse>(items, page.Page, page.PageSize, totalCount);
    }
}
