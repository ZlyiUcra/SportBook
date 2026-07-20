using Mediator;
using Microsoft.EntityFrameworkCore;
using SportBook.Application.Authorization;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Application.Exceptions;
using SportBook.Application.Services;
using SportBook.Infrastructure;

namespace SportBook.Application.Features.Bookings.ListVenueBookingsForOwner;

/// <summary>Paginated list of bookings against one of the caller's own venues. Only bookings for the caller's own venue (spec FR-010); venue ownership is checked directly, not per row.</summary>
public sealed record ListVenueBookingsForOwnerQuery(Guid OwnerId, Guid VenueId, PageRequest Paging) : IRequest<PagedResponse<BookingResponse>>;

public sealed class ListVenueBookingsForOwnerHandler(SportBookDbContext db, TimeProvider timeProvider)
    : IRequestHandler<ListVenueBookingsForOwnerQuery, PagedResponse<BookingResponse>>
{
    public async ValueTask<PagedResponse<BookingResponse>> Handle(ListVenueBookingsForOwnerQuery request, CancellationToken ct)
    {
        var venue = await db.Venues.AsNoTracking().SingleOrDefaultAsync(v => v.Id == request.VenueId, ct)
            ?? throw new ApiException(404, "VENUE_NOT_FOUND", "Venue not found.");
        OwnershipChecks.EnsureVenueOwner(venue, request.OwnerId);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var query = db.Bookings.AsNoTracking().Where(b => b.Court!.VenueId == request.VenueId);
        var page = request.Paging;
        var totalCount = await query.CountAsync(ct);
        var items = await BookingHelpers.IncludeDetail(query)
            .OrderByDescending(b => b.StartTime)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(ct);

        return new PagedResponse<BookingResponse>(
            items.Select(b => b.ToResponse(now)).ToList(), page.Page, page.PageSize, totalCount);
    }
}
