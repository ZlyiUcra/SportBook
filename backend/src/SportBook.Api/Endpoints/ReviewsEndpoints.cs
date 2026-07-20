using System.Security.Claims;
using SportBook.Api.Extensions;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Application.Services;

namespace SportBook.Api.Endpoints;

/// <summary>Venue reviews: paginated reads and a create-or-replace write (US3).</summary>
public static class ReviewsEndpoints
{
    /// <summary>Registers the venue review endpoints under `api/venues/{venueId}/reviews`.</summary>
    public static void MapReviewsEndpoints(this IEndpointRouteBuilder app)
    {
        // <summary>Paginated list of a venue's reviews, newest first.</summary>
        app.MapGet("api/venues/{venueId:guid}/reviews", async (
            Guid venueId, [AsParameters] PageRequest paging, ReviewService reviewService, CancellationToken ct) =>
        {
            var result = await reviewService.ListByVenueAsync(venueId, paging, ct);
            return Results.Ok(result);
        });

        // <summary>Submits a review; a second submission by the same user for the same venue replaces the first (201 vs 200 signals which).</summary>
        app.MapPost("api/venues/{venueId:guid}/reviews", async (
            ClaimsPrincipal user, Guid venueId, CreateReviewRequest request, ReviewService reviewService, CancellationToken ct) =>
        {
            var (response, created) = await reviewService.CreateOrReplaceAsync(user.GetUserId(), venueId, request, ct);
            return created
                ? Results.Json(response, statusCode: StatusCodes.Status201Created)
                : Results.Ok(response);
        });
    }
}
