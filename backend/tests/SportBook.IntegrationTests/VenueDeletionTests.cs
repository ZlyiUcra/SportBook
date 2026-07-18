using System.Net;
using System.Net.Http.Json;
using SportBook.Application.Dtos;
using SportBook.Domain.Enums;
using SportBook.IntegrationTests.TestInfrastructure;

namespace SportBook.IntegrationTests;

/// <summary>T043: deleting a venue/court with an upcoming, non-cancelled booking is rejected (spec FR-009).</summary>
[Collection(ApiCollection.Name)]
public class VenueDeletionTests(ApiFixture fixture)
{
    [Fact]
    public async Task Deleting_a_venue_with_an_upcoming_booking_is_rejected()
    {
        var ownerClient = fixture.Factory.CreateClient();
        var owner = await ownerClient.RegisterAsync("Owner");
        ownerClient.UseBearer(owner.AccessToken);
        var court = await fixture.Factory.SeedCourtAsync(owner.User.Id);

        var customer = await fixture.Factory.CreateClient().RegisterAsync();
        await fixture.Factory.SeedBookingAsync(
            court.Id, customer.User.Id, ApiClientExtensions.TomorrowAt(9), ApiClientExtensions.TomorrowAt(10));

        var delete = await ownerClient.DeleteAsync($"/api/venues/{court.VenueId}");
        Assert.Equal(HttpStatusCode.Conflict, delete.StatusCode);
    }

    [Fact]
    public async Task Deleting_a_court_with_an_upcoming_booking_is_rejected()
    {
        var ownerClient = fixture.Factory.CreateClient();
        var owner = await ownerClient.RegisterAsync("Owner");
        ownerClient.UseBearer(owner.AccessToken);
        var court = await fixture.Factory.SeedCourtAsync(owner.User.Id);

        var customer = await fixture.Factory.CreateClient().RegisterAsync();
        await fixture.Factory.SeedBookingAsync(
            court.Id, customer.User.Id, ApiClientExtensions.TomorrowAt(9), ApiClientExtensions.TomorrowAt(10));

        var delete = await ownerClient.DeleteAsync($"/api/courts/{court.Id}");
        Assert.Equal(HttpStatusCode.Conflict, delete.StatusCode);
    }

    [Fact]
    public async Task Deleting_a_venue_and_court_without_upcoming_bookings_succeeds()
    {
        var ownerClient = fixture.Factory.CreateClient();
        var owner = await ownerClient.RegisterAsync("Owner");
        ownerClient.UseBearer(owner.AccessToken);
        var venue = (await (await ownerClient.PostAsJsonAsync("/api/venues",
            new CreateVenueRequest("Disposable Venue", ApiClientExtensions.KyivCityId, "1 St", null))).Content.ReadFromJsonAsync<VenueDetailResponse>())!;
        var court = (await (await ownerClient.PostAsJsonAsync($"/api/venues/{venue.Id}/courts",
            new CreateCourtRequest("Disposable Court", SportType.Tennis, 100m, new TimeOnly(8, 0), new TimeOnly(20, 0))))
            .Content.ReadFromJsonAsync<CourtResponse>())!;

        var deleteCourt = await ownerClient.DeleteAsync($"/api/courts/{court.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteCourt.StatusCode);

        var deleteVenue = await ownerClient.DeleteAsync($"/api/venues/{venue.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteVenue.StatusCode);
    }
}
