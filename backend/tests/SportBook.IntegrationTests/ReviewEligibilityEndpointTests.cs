using System.Net;
using System.Net.Http.Json;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Application.Features.Reviews.CreateOrReplaceReview;
using SportBook.Domain.Enums;
using SportBook.IntegrationTests.TestInfrastructure;

namespace SportBook.IntegrationTests;

/// <summary>
/// T002 (006): `POST /api/venues/{venueId}/reviews` accepts only a user with a Confirmed, past
/// booking on one of the venue's courts (data-model.md eligibility rule); the rating check and the
/// review list are unaffected.
/// </summary>
[Collection(ApiCollection.Name)]
public class ReviewEligibilityEndpointTests(ApiFixture fixture)
{
    [Fact]
    public async Task Eligible_customer_can_create_then_replace_their_review()
    {
        var ownerClient = fixture.Factory.CreateClient();
        var owner = await ownerClient.RegisterAsync("Owner");
        ownerClient.UseBearer(owner.AccessToken);
        var court = await fixture.Factory.SeedCourtAsync(owner.User.Id);

        var reviewerClient = fixture.Factory.CreateClient();
        var reviewer = await reviewerClient.RegisterAsync("Reviewer");
        reviewerClient.UseBearer(reviewer.AccessToken);
        var now = DateTime.UtcNow;
        await fixture.Factory.SeedBookingAsync(court.Id, reviewer.User.Id,
            now.AddHours(-25), now.AddHours(-24), BookingStatus.Confirmed);

        var createResponse = await reviewerClient.PostAsJsonAsync(
            $"/api/venues/{court.VenueId}/reviews", new CreateReviewRequest(4, "Great court"));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var replaceResponse = await reviewerClient.PostAsJsonAsync(
            $"/api/venues/{court.VenueId}/reviews", new CreateReviewRequest(5, "Even better"));
        Assert.Equal(HttpStatusCode.OK, replaceResponse.StatusCode);

        var list = await ownerClient.GetFromJsonAsync<PagedResponse<ReviewResponse>>($"/api/venues/{court.VenueId}/reviews");
        Assert.NotNull(list);
        Assert.Equal(1, list.TotalCount);
        Assert.Equal(5, list.Items.Single().Rating);
    }

    [Fact]
    public async Task Customer_with_no_completed_booking_is_rejected()
    {
        var owner = await fixture.Factory.CreateClient().RegisterAsync("Owner");
        var court = await fixture.Factory.SeedCourtAsync(owner.User.Id);

        var reviewerClient = fixture.Factory.CreateClient();
        var reviewer = await reviewerClient.RegisterAsync("Reviewer");
        reviewerClient.UseBearer(reviewer.AccessToken);

        var response = await reviewerClient.PostAsJsonAsync(
            $"/api/venues/{court.VenueId}/reviews", new CreateReviewRequest(4, "Never played"));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var list = await reviewerClient.GetFromJsonAsync<PagedResponse<ReviewResponse>>($"/api/venues/{court.VenueId}/reviews");
        Assert.NotNull(list);
        Assert.Equal(0, list.TotalCount);
    }

    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.Cancelled)]
    public async Task Customer_with_only_a_non_confirmed_past_booking_is_rejected(BookingStatus status)
    {
        var owner = await fixture.Factory.CreateClient().RegisterAsync("Owner");
        var court = await fixture.Factory.SeedCourtAsync(owner.User.Id);

        var reviewerClient = fixture.Factory.CreateClient();
        var reviewer = await reviewerClient.RegisterAsync("Reviewer");
        reviewerClient.UseBearer(reviewer.AccessToken);
        var now = DateTime.UtcNow;
        await fixture.Factory.SeedBookingAsync(court.Id, reviewer.User.Id, now.AddHours(-25), now.AddHours(-24), status);

        var response = await reviewerClient.PostAsJsonAsync(
            $"/api/venues/{court.VenueId}/reviews", new CreateReviewRequest(4, null));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Customer_with_only_an_upcoming_confirmed_booking_is_rejected()
    {
        var owner = await fixture.Factory.CreateClient().RegisterAsync("Owner");
        var court = await fixture.Factory.SeedCourtAsync(owner.User.Id);

        var reviewerClient = fixture.Factory.CreateClient();
        var reviewer = await reviewerClient.RegisterAsync("Reviewer");
        reviewerClient.UseBearer(reviewer.AccessToken);
        var now = DateTime.UtcNow;
        await fixture.Factory.SeedBookingAsync(court.Id, reviewer.User.Id,
            now.AddHours(24), now.AddHours(25), BookingStatus.Confirmed);

        var response = await reviewerClient.PostAsJsonAsync(
            $"/api/venues/{court.VenueId}/reviews", new CreateReviewRequest(4, null));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Rating_validation_still_rejects_an_eligible_customers_bad_rating()
    {
        var owner = await fixture.Factory.CreateClient().RegisterAsync("Owner");
        var court = await fixture.Factory.SeedCourtAsync(owner.User.Id);

        var reviewerClient = fixture.Factory.CreateClient();
        var reviewer = await reviewerClient.RegisterAsync("Reviewer");
        reviewerClient.UseBearer(reviewer.AccessToken);
        var now = DateTime.UtcNow;
        await fixture.Factory.SeedBookingAsync(court.Id, reviewer.User.Id,
            now.AddHours(-25), now.AddHours(-24), BookingStatus.Confirmed);

        var response = await reviewerClient.PostAsJsonAsync(
            $"/api/venues/{court.VenueId}/reviews", new CreateReviewRequest(0, null));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
