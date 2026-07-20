using MediatR;
using Microsoft.EntityFrameworkCore;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Application.Services;
using SportBook.Domain.Entities;
using SportBook.Domain.Enums;
using SportBook.Infrastructure;

namespace SportBook.Application.Features.Venues.SearchVenues;

/// <summary>
/// Paginated venue search by city and/or sport type; a non-null <see cref="OwnerId"/> scopes
/// results to that owner's own venues (owner dashboard), server-derived from the caller's JWT,
/// never client-supplied. <see cref="IncludeNearby"/> only has an effect together with
/// <see cref="CityId"/> (spec US4) - it widens the city filter to the fixed 150km neighbor set,
/// which stays a server-side constant regardless of what the client requests.
/// </summary>
public sealed record SearchVenuesQuery(
    int? CityId, bool IncludeNearby, SportType? SportType, Guid? OwnerId, PageRequest Paging)
    : IRequest<PagedResponse<VenueSummaryResponse>>;

public sealed class SearchVenuesHandler(SportBookDbContext db, CityService cityService)
    : IRequestHandler<SearchVenuesQuery, PagedResponse<VenueSummaryResponse>>
{
    public async Task<PagedResponse<VenueSummaryResponse>> Handle(SearchVenuesQuery request, CancellationToken ct)
    {
        IQueryable<Venue> query = db.Venues.AsNoTracking().Include(v => v.City);

        if (request.OwnerId is not null)
        {
            query = query.Where(v => v.OwnerId == request.OwnerId);
        }

        if (request.CityId is not null)
        {
            if (request.IncludeNearby)
            {
                var neighborIds = await cityService.GetNeighborIdsAsync(request.CityId.Value, ct);
                var cityIds = new List<int>(neighborIds) { request.CityId.Value };
                query = query.Where(v => cityIds.Contains(v.CityId));
            }
            else
            {
                query = query.Where(v => v.CityId == request.CityId);
            }
        }

        if (request.SportType is not null)
        {
            query = query.Where(v => v.Courts.Any(c => c.SportType == request.SportType && c.IsActive));
        }

        var page = request.Paging;
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderBy(v => v.Name)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(ct);

        return new PagedResponse<VenueSummaryResponse>(
            items.Select(v => v.ToSummaryResponse()).ToList(), page.Page, page.PageSize, totalCount);
    }
}
