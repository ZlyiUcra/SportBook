using Microsoft.EntityFrameworkCore;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Application.Exceptions;
using SportBook.Domain.Entities;
using SportBook.Infrastructure;

namespace SportBook.Application.Services;

/// <summary>
/// Venue reviews (US3): paginated list, and create-or-replace (data-model.md - at most one
/// review per user per venue, backed by a unique index on (VenueId, UserId)).
/// </summary>
public class ReviewService(SportBookDbContext db, TimeProvider timeProvider)
{
    public async Task<PagedResponse<ReviewResponse>> ListByVenueAsync(Guid venueId, PageRequest page, CancellationToken ct)
    {
        if (!await db.Venues.AsNoTracking().AnyAsync(v => v.Id == venueId, ct))
        {
            throw new ApiException(404, "VENUE_NOT_FOUND", "Venue not found.");
        }

        var query = db.Reviews.AsNoTracking().Where(r => r.VenueId == venueId);
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(r => new ReviewResponse(r.Id, r.VenueId, r.UserId, r.User!.Name, r.Rating, r.Comment, r.CreatedAt))
            .ToListAsync(ct);

        return new PagedResponse<ReviewResponse>(items, page.Page, page.PageSize, totalCount);
    }

    /// <summary>Returns the response plus whether a new review row was created (201) vs an existing one updated (200).</summary>
    public async Task<(ReviewResponse Response, bool Created)> CreateOrReplaceAsync(
        Guid userId, Guid venueId, CreateReviewRequest request, CancellationToken ct)
    {
        if (request.Rating is < 1 or > 5)
        {
            throw new ApiException(400, "INVALID_RATING", "Rating must be between 1 and 5.");
        }

        if (!await db.Venues.AnyAsync(v => v.Id == venueId, ct))
        {
            throw new ApiException(404, "VENUE_NOT_FOUND", "Venue not found.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var existing = await db.Reviews.SingleOrDefaultAsync(r => r.VenueId == venueId && r.UserId == userId, ct);

        Review review;
        bool created;
        if (existing is not null)
        {
            existing.Rating = request.Rating;
            existing.Comment = request.Comment;
            review = existing;
            created = false;
        }
        else
        {
            review = new Review
            {
                Id = Guid.NewGuid(),
                VenueId = venueId,
                UserId = userId,
                Rating = request.Rating,
                Comment = request.Comment,
                CreatedAt = now,
            };
            db.Reviews.Add(review);
            created = true;
        }

        await db.SaveChangesAsync(ct);

        var userName = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.Name).SingleAsync(ct);
        return (new ReviewResponse(review.Id, review.VenueId, review.UserId, userName, review.Rating, review.Comment, review.CreatedAt), created);
    }
}
