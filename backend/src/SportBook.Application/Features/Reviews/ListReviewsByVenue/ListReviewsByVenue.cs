using Mediator;
using Microsoft.EntityFrameworkCore;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Application.Exceptions;
using SportBook.Infrastructure;

namespace SportBook.Application.Features.Reviews.ListReviewsByVenue;

/// <summary>Paginated list of a venue's reviews, newest first.</summary>
public sealed record ListReviewsByVenueQuery(Guid VenueId, PageRequest Paging) : IRequest<PagedResponse<ReviewResponse>>;

public sealed class ListReviewsByVenueHandler(SportBookDbContext db) : IRequestHandler<ListReviewsByVenueQuery, PagedResponse<ReviewResponse>>
{
    public async ValueTask<PagedResponse<ReviewResponse>> Handle(ListReviewsByVenueQuery request, CancellationToken ct)
    {
        if (!await db.Venues.AsNoTracking().AnyAsync(v => v.Id == request.VenueId, ct))
        {
            throw new ApiException(404, "VENUE_NOT_FOUND", "Venue not found.");
        }

        var page = request.Paging;
        var query = db.Reviews.AsNoTracking().Where(r => r.VenueId == request.VenueId);
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(r => new ReviewResponse(r.Id, r.VenueId, r.UserId, r.User!.Name, r.Rating, r.Comment, r.CreatedAt))
            .ToListAsync(ct);

        return new PagedResponse<ReviewResponse>(items, page.Page, page.PageSize, totalCount);
    }
}
