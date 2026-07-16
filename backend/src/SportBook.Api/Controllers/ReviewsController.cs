using Microsoft.AspNetCore.Mvc;
using SportBook.Api.Extensions;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Application.Services;

namespace SportBook.Api.Controllers;

[ApiController]
public class ReviewsController(ReviewService reviewService) : ControllerBase
{
    [HttpGet("api/venues/{venueId:guid}/reviews")]
    public async Task<ActionResult<PagedResponse<ReviewResponse>>> ListByVenue(
        Guid venueId, [FromQuery] PageRequest page, CancellationToken ct)
    {
        return Ok(await reviewService.ListByVenueAsync(venueId, page, ct));
    }

    [HttpPost("api/venues/{venueId:guid}/reviews")]
    public async Task<ActionResult<ReviewResponse>> Create(Guid venueId, CreateReviewRequest request, CancellationToken ct)
    {
        var (response, created) = await reviewService.CreateOrReplaceAsync(User.GetUserId(), venueId, request, ct);
        return created ? StatusCode(StatusCodes.Status201Created, response) : Ok(response);
    }
}
