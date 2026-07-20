using Mediator;
using Microsoft.EntityFrameworkCore;
using SportBook.Application.Dtos;
using SportBook.Application.Exceptions;
using SportBook.Domain.Entities;
using SportBook.Domain.Enums;
using SportBook.Infrastructure;

namespace SportBook.Application.Features.Reviews.CreateOrReplaceReview;

/// <summary>
/// Submits a review; a second submission by the same user for the same venue replaces the first
/// (201 vs 200 signals which - <see cref="Created"/> on the result).
/// </summary>
public sealed record CreateOrReplaceReviewCommand(Guid UserId, Guid VenueId, int Rating, string? Comment)
    : IRequest<CreateOrReplaceReviewResult>;

/// <summary>Returns the response plus whether a new review row was created (201) vs an existing one updated (200).</summary>
public sealed record CreateOrReplaceReviewResult(ReviewResponse Response, bool Created);

/// <summary>
/// Create-or-replace is gated (006 data-model.md): a review may only be created or replaced by a
/// user with a Confirmed, past booking on one of the venue's courts (a completed game) - checked
/// server-side so the client is never trusted to self-certify. Replacing an existing review is
/// further gated (007 data-model.md): only within 24 hours of its original CreatedAt (never reset
/// by a prior replace), and only with a comment of at least 10 characters - neither rule applies
/// to a first-time submission.
/// </summary>
public sealed class CreateOrReplaceReviewHandler(SportBookDbContext db, TimeProvider timeProvider)
    : IRequestHandler<CreateOrReplaceReviewCommand, CreateOrReplaceReviewResult>
{
    public async ValueTask<CreateOrReplaceReviewResult> Handle(CreateOrReplaceReviewCommand request, CancellationToken ct)
    {
        if (request.Rating is < 1 or > 5)
        {
            throw new ApiException(400, "INVALID_RATING", "Rating must be between 1 and 5.");
        }

        if (!await db.Venues.AnyAsync(v => v.Id == request.VenueId, ct))
        {
            throw new ApiException(404, "VENUE_NOT_FOUND", "Venue not found.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        var hasCompletedGame = await db.Bookings.AnyAsync(b =>
            b.UserId == request.UserId && b.Court!.VenueId == request.VenueId &&
            b.Status == BookingStatus.Confirmed && b.EndTime <= now, ct);
        if (!hasCompletedGame)
        {
            throw new ApiException(409, "REVIEW_NOT_ELIGIBLE",
                "You can only review a venue after completing a confirmed game there.");
        }

        var existing = await db.Reviews.SingleOrDefaultAsync(r => r.VenueId == request.VenueId && r.UserId == request.UserId, ct);

        Review review;
        bool created;
        if (existing is not null)
        {
            if (now > existing.CreatedAt.AddHours(24))
            {
                throw new ApiException(409, "REVIEW_EDIT_WINDOW_CLOSED",
                    "This review can no longer be edited - the 24-hour edit window has passed.");
            }

            var trimmedComment = request.Comment?.Trim();
            if (string.IsNullOrEmpty(trimmedComment) || trimmedComment.Length < 10)
            {
                throw new ApiException(400, "REVIEW_COMMENT_TOO_SHORT",
                    "Editing a review requires a comment of at least 10 characters.");
            }

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
                VenueId = request.VenueId,
                UserId = request.UserId,
                Rating = request.Rating,
                Comment = request.Comment,
                CreatedAt = now,
            };
            db.Reviews.Add(review);
            created = true;
        }

        await db.SaveChangesAsync(ct);

        var userName = await db.Users.AsNoTracking().Where(u => u.Id == request.UserId).Select(u => u.Name).SingleAsync(ct);
        return new CreateOrReplaceReviewResult(
            new ReviewResponse(review.Id, review.VenueId, review.UserId, userName, review.Rating, review.Comment, review.CreatedAt), created);
    }
}
