using Microsoft.AspNetCore.Mvc;
using SportBook.Api.Extensions;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Application.Services;

namespace SportBook.Api.Controllers;

/// <summary>Venue reviews: paginated reads and a create-or-replace write (US3).</summary>
[ApiController]
public class ReviewsController(ReviewService reviewService) : ControllerBase
{
    /// <summary>Paginated list of a venue's reviews, newest first.</summary>
    [HttpGet("api/venues/{venueId:guid}/reviews")]
    public async Task<ActionResult<PagedResponse<ReviewResponse>>> ListByVenue(
        Guid venueId, [FromQuery] PageRequest paging, CancellationToken ct)
    {
        return Ok(await reviewService.ListByVenueAsync(venueId, paging, ct));
    }

    /// <summary>Submits a review; a second submission by the same user for the same venue replaces the first (201 vs 200 signals which).</summary>
    [HttpPost("api/venues/{venueId:guid}/reviews")]
    public async Task<ActionResult<ReviewResponse>> Create(Guid venueId, CreateReviewRequest request, CancellationToken ct)
    {
        var (response, created) = await reviewService.CreateOrReplaceAsync(User.GetUserId(), venueId, request, ct);
        return created ? StatusCode(StatusCodes.Status201Created, response) : Ok(response);
    }
}
