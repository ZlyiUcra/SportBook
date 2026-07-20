using System.Net;
using System.Net.Http.Json;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Domain.Enums;
using SportBook.IntegrationTests.TestInfrastructure;

namespace SportBook.IntegrationTests;

/// <summary>
/// T007 (007): `POST /api/venues/{venueId}/reviews` replace requires a comment of at least 10
/// characters (data-model.md edit-comment rule); a first-time submission is unaffected.
/// </summary>
[Collection(ApiCollection.Name)]
public class ReviewEditCommentEndpointTests(ApiFixture fixture)
{
    private async Task<(HttpClient ReviewerClient, Guid VenueId)> SeedReviewerWithReview(string comment)
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

        return (reviewerClient, court.VenueId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Too short")]
    public async Task Replace_with_a_missing_empty_or_short_comment_is_rejected(string? comment)
    {
        var (reviewerClient, venueId) = await SeedReviewerWithReview("Original comment");

        var replaceResponse = await reviewerClient.PostAsJsonAsync(
            $"/api/venues/{venueId}/reviews", new CreateReviewRequest(5, comment));
        Assert.Equal(HttpStatusCode.BadRequest, replaceResponse.StatusCode);

        var list = await reviewerClient.GetFromJsonAsync<PagedResponse<ReviewResponse>>($"/api/venues/{venueId}/reviews");
        Assert.NotNull(list);
        var stored = list.Items.Single();
        Assert.Equal(3, stored.Rating);
        Assert.Equal("Original comment", stored.Comment);
    }

    [Fact]
    public async Task Replace_with_a_10_character_comment_succeeds()
    {
        var (reviewerClient, venueId) = await SeedReviewerWithReview("Original comment");

        var replaceResponse = await reviewerClient.PostAsJsonAsync(
            $"/api/venues/{venueId}/reviews", new CreateReviewRequest(5, "1234567890"));
        Assert.Equal(HttpStatusCode.OK, replaceResponse.StatusCode);
    }

    [Fact]
    public async Task First_time_submission_with_no_comment_still_returns_201()
    {
        var owner = await fixture.Factory.CreateClient().RegisterAsync("Owner");
        var court = await fixture.Factory.SeedCourtAsync(owner.User.Id);

        var reviewerClient = fixture.Factory.CreateClient();
        var reviewer = await reviewerClient.RegisterAsync("Reviewer");
        reviewerClient.UseBearer(reviewer.AccessToken);
        var now = DateTime.UtcNow;
        await fixture.Factory.SeedBookingAsync(court.Id, reviewer.User.Id,
            now.AddHours(-48), now.AddHours(-47), BookingStatus.Confirmed);

        var response = await reviewerClient.PostAsJsonAsync(
            $"/api/venues/{court.VenueId}/reviews", new CreateReviewRequest(4, null));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
