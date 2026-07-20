using System.Net;
using System.Net.Http.Json;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Application.Features.Reviews.CreateOrReplaceReview;
using SportBook.Domain.Enums;
using SportBook.IntegrationTests.TestInfrastructure;

namespace SportBook.IntegrationTests;

/// <summary>
/// T002 (007): `POST /api/venues/{venueId}/reviews` accepts a replace only within 24 hours of the
/// existing review's original creation time (data-model.md edit-window rule); past that it is
/// rejected and the stored review is unchanged, while the review list is unaffected.
/// </summary>
[Collection(ApiCollection.Name)]
public class ReviewEditWindowEndpointTests(ApiFixture fixture)
{
    private async Task<(HttpClient ReviewerClient, Guid VenueId, Guid ReviewId)> SeedReviewerWithReview(string comment)
    {
        var owner = await fixture.Factory.CreateClient().RegisterAsync("Owner");
        var court = await fixture.Factory.SeedCourtAsync(owner.User.Id);

        var reviewerClient = fixture.Factory.CreateClient();
        var reviewer = await reviewerClient.RegisterAsync("Reviewer");
        reviewerClient.UseBearer(reviewer.AccessToken);
        var now = DateTime.UtcNow;
        await fixture.Factory.SeedBookingAsync(court.Id, reviewer.User.Id,
            now.AddHours(-48), now.AddHours(-47), BookingStatus.Confirmed);

        var createResponse = await reviewerClient.PostAsJsonAsync(
            $"/api/venues/{court.VenueId}/reviews", new CreateReviewRequest(3, comment));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = (await createResponse.Content.ReadFromJsonAsync<ReviewResponse>())!;

        return (reviewerClient, court.VenueId, created.Id);
    }

    [Fact]
    public async Task Replace_within_24h_of_creation_is_accepted()
    {
        var (reviewerClient, venueId, _) = await SeedReviewerWithReview("Original comment");

        var replaceResponse = await reviewerClient.PostAsJsonAsync(
            $"/api/venues/{venueId}/reviews", new CreateReviewRequest(5, "Updated comment here"));
        Assert.Equal(HttpStatusCode.OK, replaceResponse.StatusCode);
    }

    [Fact]
    public async Task Replace_after_the_window_closed_is_rejected_and_leaves_the_review_unchanged()
    {
        var (reviewerClient, venueId, reviewId) = await SeedReviewerWithReview("Original comment");

        await fixture.Factory.SeedAsync(db =>
        {
            var review = db.Reviews.Single(r => r.Id == reviewId);
            review.CreatedAt = DateTime.UtcNow.AddHours(-25);
            return db.SaveChangesAsync();
        });

        var replaceResponse = await reviewerClient.PostAsJsonAsync(
            $"/api/venues/{venueId}/reviews", new CreateReviewRequest(5, "Updated comment here"));
        Assert.Equal(HttpStatusCode.Conflict, replaceResponse.StatusCode);

        var list = await reviewerClient.GetFromJsonAsync<PagedResponse<ReviewResponse>>($"/api/venues/{venueId}/reviews");
        Assert.NotNull(list);
        var stored = list.Items.Single();
        Assert.Equal(3, stored.Rating);
        Assert.Equal("Original comment", stored.Comment);
    }

    [Fact]
    public async Task Review_past_its_window_still_lists_normally()
    {
        var (reviewerClient, venueId, reviewId) = await SeedReviewerWithReview("Original comment");

        await fixture.Factory.SeedAsync(db =>
        {
            var review = db.Reviews.Single(r => r.Id == reviewId);
            review.CreatedAt = DateTime.UtcNow.AddHours(-25);
            return db.SaveChangesAsync();
        });

        var list = await reviewerClient.GetFromJsonAsync<PagedResponse<ReviewResponse>>($"/api/venues/{venueId}/reviews");
        Assert.NotNull(list);
        Assert.Equal(1, list.TotalCount);
    }
}
