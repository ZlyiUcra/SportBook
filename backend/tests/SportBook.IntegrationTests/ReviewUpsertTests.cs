using System.Net;
using System.Net.Http.Json;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Application.Features.Reviews.CreateOrReplaceReview;
using SportBook.Application.Features.Venues.CreateVenue;
using SportBook.Domain.Enums;
using SportBook.IntegrationTests.TestInfrastructure;

namespace SportBook.IntegrationTests;

/// <summary>
/// T054: a second review by the same user replaces the first rather than duplicating it
/// (data-model.md - one review per user per venue). Since 006, the reviewer needs a Confirmed,
/// past booking on the venue's court to be eligible.
/// </summary>
[Collection(ApiCollection.Name)]
public class ReviewUpsertTests(ApiFixture fixture)
{
    [Fact]
    public async Task Second_review_by_the_same_user_replaces_the_first()
    {
        var ownerClient = fixture.Factory.CreateClient();
        var owner = await ownerClient.RegisterAsync("Owner");
        ownerClient.UseBearer(owner.AccessToken);
        var venue = (await (await ownerClient.PostAsJsonAsync("/api/venues",
            new CreateVenueRequest("Venue", ApiClientExtensions.KyivCityId, "1 St", null))).Content.ReadFromJsonAsync<VenueDetailResponse>())!;
        var court = await fixture.Factory.SeedCourtAsync(owner.User.Id, venueId: venue.Id);

        var reviewerClient = fixture.Factory.CreateClient();
        var reviewer = await reviewerClient.RegisterAsync("Reviewer");
        reviewerClient.UseBearer(reviewer.AccessToken);
        var now = DateTime.UtcNow;
        await fixture.Factory.SeedBookingAsync(court.Id, reviewer.User.Id,
            now.AddHours(-25), now.AddHours(-24), BookingStatus.Confirmed);

        var firstResponse = await reviewerClient.PostAsJsonAsync(
            $"/api/venues/{venue.Id}/reviews", new CreateReviewRequest(3, "First take"));
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        var first = (await firstResponse.Content.ReadFromJsonAsync<ReviewResponse>())!;

        var secondResponse = await reviewerClient.PostAsJsonAsync(
            $"/api/venues/{venue.Id}/reviews", new CreateReviewRequest(5, "Changed my mind"));
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        var second = (await secondResponse.Content.ReadFromJsonAsync<ReviewResponse>())!;

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(5, second.Rating);
        Assert.Equal("Changed my mind", second.Comment);

        var list = await ownerClient.GetFromJsonAsync<PagedResponse<ReviewResponse>>($"/api/venues/{venue.Id}/reviews");
        Assert.NotNull(list);
        Assert.Equal(1, list.TotalCount);
        Assert.Equal(5, list.Items.Single().Rating);
    }
}
