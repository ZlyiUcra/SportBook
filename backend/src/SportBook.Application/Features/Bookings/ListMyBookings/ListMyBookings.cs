using MediatR;
using Microsoft.EntityFrameworkCore;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Application.Services;
using SportBook.Infrastructure;

namespace SportBook.Application.Features.Bookings.ListMyBookings;

/// <summary>
/// Status groups a customer can filter "My bookings" by (005 data-model.md). `Completed` is not a
/// stored status - it is a Confirmed booking whose end time has passed (001 read-time derivation) -
/// so each value maps to a stored-status + time predicate in
/// <see cref="Services.BookingHelpers.ApplyStatusFilter"/>, applied before paging so it filters the
/// whole history. `All` (the default) applies no predicate.
/// </summary>
public enum BookingStatusFilter
{
    All,
    Upcoming,
    Completed,
    Cancelled,
}

/// <summary>
/// Paginated list of the caller's own bookings. `Status` (default All) filters by
/// All/Upcoming/Completed/Cancelled server-side, before paging, so it holds across pages (005
/// spec FR-006); the owner venue-bookings query does not take this filter.
/// </summary>
public sealed record ListMyBookingsQuery(Guid UserId, BookingStatusFilter Status, PageRequest Paging) : IRequest<PagedResponse<BookingResponse>>;

public sealed class ListMyBookingsHandler(SportBookDbContext db, TimeProvider timeProvider)
    : IRequestHandler<ListMyBookingsQuery, PagedResponse<BookingResponse>>
{
    public async Task<PagedResponse<BookingResponse>> Handle(ListMyBookingsQuery request, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var query = db.Bookings.AsNoTracking().Where(b => b.UserId == request.UserId);
        query = BookingHelpers.ApplyStatusFilter(query, request.Status, now);
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
