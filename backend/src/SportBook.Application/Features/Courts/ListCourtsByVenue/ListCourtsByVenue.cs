using MediatR;
using Microsoft.EntityFrameworkCore;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Application.Exceptions;
using SportBook.Infrastructure;

namespace SportBook.Application.Features.Courts.ListCourtsByVenue;

/// <summary>Paginated list of a venue's courts.</summary>
public sealed record ListCourtsByVenueQuery(Guid VenueId, PageRequest Paging) : IRequest<PagedResponse<CourtResponse>>;

public sealed class ListCourtsByVenueHandler(SportBookDbContext db) : IRequestHandler<ListCourtsByVenueQuery, PagedResponse<CourtResponse>>
{
    public async Task<PagedResponse<CourtResponse>> Handle(ListCourtsByVenueQuery request, CancellationToken ct)
    {
        if (!await db.Venues.AnyAsync(v => v.Id == request.VenueId, ct))
        {
            throw new ApiException(404, "VENUE_NOT_FOUND", "Venue not found.");
        }

        var page = request.Paging;
        var query = db.Courts.AsNoTracking().Where(c => c.VenueId == request.VenueId);
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
