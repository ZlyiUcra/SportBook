using System.Net;
using System.Net.Http.Json;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.IntegrationTests.TestInfrastructure;

namespace SportBook.IntegrationTests;

/// <summary>T053: submitting a review appears in the venue's review list and updates its average rating (spec Acceptance Scenarios 1-2, US3).</summary>
[Collection(ApiCollection.Name)]
public class ReviewTests(ApiFixture fixture)
{
    [Fact]
    public async Task Submitting_a_review_appears_in_the_list_and_updates_the_average_rating()
    {
        var ownerClient = fixture.Factory.CreateClient();
        var owner = await ownerClient.RegisterAsync("Owner");
        ownerClient.UseBearer(owner.AccessToken);
        var venue = (await (await ownerClient.PostAsJsonAsync("/api/venues",
            new CreateVenueRequest("Reviewed Venue", ApiClientExtensions.KyivCityId, "1 St", null))).Content.ReadFromJsonAsync<VenueDetailResponse>())!;

        var firstReviewerClient = fixture.Factory.CreateClient();
        var firstReviewer = await firstReviewerClient.RegisterAsync("Reviewer1");
        firstReviewerClient.UseBearer(firstReviewer.AccessToken);
        var createResponse = await firstReviewerClient.PostAsJsonAsync(
            $"/api/venues/{venue.Id}/reviews", new CreateReviewRequest(4, "Good venue"));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var secondReviewerClient = fixture.Factory.CreateClient();
        var secondReviewer = await secondReviewerClient.RegisterAsync("Reviewer2");
        secondReviewerClient.UseBearer(secondReviewer.AccessToken);
        await secondReviewerClient.PostAsJsonAsync(
            $"/api/venues/{venue.Id}/reviews", new CreateReviewRequest(2, "Just OK"));

        var list = await ownerClient.GetFromJsonAsync<PagedResponse<ReviewResponse>>($"/api/venues/{venue.Id}/reviews");
        Assert.NotNull(list);
        Assert.Equal(2, list.TotalCount);
        Assert.Contains(list.Items, r => r.UserId == firstReviewer.User.Id && r.Rating == 4);
        Assert.Contains(list.Items, r => r.UserId == secondReviewer.User.Id && r.Rating == 2);

        var detail = await ownerClient.GetFromJsonAsync<VenueDetailResponse>($"/api/venues/{venue.Id}");
        Assert.NotNull(detail);
        Assert.Equal(2, detail.ReviewCount);
        Assert.Equal(3.0, detail.AverageRating);
    }
}
