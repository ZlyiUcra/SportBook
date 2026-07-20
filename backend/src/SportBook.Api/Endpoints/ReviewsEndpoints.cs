using System.Security.Claims;
using MediatR;
using SportBook.Api.Extensions;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Application.Features.Reviews.CreateOrReplaceReview;
using SportBook.Application.Features.Reviews.ListReviewsByVenue;

namespace SportBook.Api.Endpoints;

/// <summary>Venue reviews: paginated reads and a create-or-replace write (US3).</summary>
public static class ReviewsEndpoints
{
    /// <summary>Registers the venue review endpoints under `api/venues/{venueId}/reviews`.</summary>
    public static void MapReviewsEndpoints(this IEndpointRouteBuilder app)
    {
        // <summary>Paginated list of a venue's reviews, newest first.</summary>
        app.MapGet("api/venues/{venueId:guid}/reviews", async (
            Guid venueId, [AsParameters] PageRequest paging, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ListReviewsByVenueQuery(venueId, paging), ct);
            return Results.Ok(result);
        });

        // <summary>Submits a review; a second submission by the same user for the same venue replaces the first (201 vs 200 signals which).</summary>
        app.MapPost("api/venues/{venueId:guid}/reviews", async (
            ClaimsPrincipal user, Guid venueId, CreateReviewRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new CreateOrReplaceReviewCommand(user.GetUserId(), venueId, request.Rating, request.Comment);
            var result = await mediator.Send(command, ct);
            return result.Created
                ? Results.Json(result.Response, statusCode: StatusCodes.Status201Created)
                : Results.Ok(result.Response);
        });
    }
}
