using System.Net;
using System.Net.Http.Json;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Domain.Enums;
using SportBook.IntegrationTests.TestInfrastructure;

namespace SportBook.IntegrationTests;

/// <summary>T040: venue owner creates a venue and court, which becomes searchable/bookable (spec Acceptance Scenario 1, US2).</summary>
[Collection(ApiCollection.Name)]
public class VenueManagementTests(ApiFixture fixture)
{
    [Fact]
    public async Task Owner_creates_venue_and_court_which_becomes_searchable_and_bookable()
    {
        var client = fixture.Factory.CreateClient();
        var owner = await client.RegisterAsync("Owner");
        client.UseBearer(owner.AccessToken);
        var city = $"City-{Guid.NewGuid():N}";

        var venueResponse = await client.PostAsJsonAsync("/api/venues",
            new CreateVenueRequest("Test Venue", city, "1 Main St", "A nice venue"));
        Assert.Equal(HttpStatusCode.Created, venueResponse.StatusCode);
        var venue = (await venueResponse.Content.ReadFromJsonAsync<VenueDetailResponse>())!;

        var courtResponse = await client.PostAsJsonAsync($"/api/venues/{venue.Id}/courts",
            new CreateCourtRequest("Court 1", SportType.Tennis, 150m, new TimeOnly(8, 0), new TimeOnly(22, 0)));
        Assert.Equal(HttpStatusCode.Created, courtResponse.StatusCode);
        var court = (await courtResponse.Content.ReadFromJsonAsync<CourtResponse>())!;

        var searchResponse = await client.GetFromJsonAsync<PagedResponse<VenueSummaryResponse>>(
            $"/api/venues?city={city}");
        Assert.NotNull(searchResponse);
        Assert.Contains(searchResponse.Items, v => v.Id == venue.Id);

        var detail = await client.GetFromJsonAsync<VenueDetailResponse>($"/api/venues/{venue.Id}");
        Assert.NotNull(detail);
        Assert.Contains(detail.Courts, c => c.Id == court.Id);

        var start = ApiClientExtensions.TomorrowAt(10);
        var bookingClient = fixture.Factory.CreateClient();
        var customer = await bookingClient.RegisterAsync();
        bookingClient.UseBearer(customer.AccessToken);
        var bookingResponse = await bookingClient.PostAsJsonAsync("/api/bookings",
            new CreateBookingRequest(court.Id, start, start.AddHours(1)));
        Assert.Equal(HttpStatusCode.Created, bookingResponse.StatusCode);
    }

    [Fact]
    public async Task Owner_updates_venue_and_court()
    {
        var client = fixture.Factory.CreateClient();
        var owner = await client.RegisterAsync("Owner");
        client.UseBearer(owner.AccessToken);

        var venue = (await (await client.PostAsJsonAsync("/api/venues",
            new CreateVenueRequest("Old Name", "Kyiv", "1 St", null))).Content.ReadFromJsonAsync<VenueDetailResponse>())!;

        var updateResponse = await client.PutAsJsonAsync($"/api/venues/{venue.Id}",
            new UpdateVenueRequest("New Name", "Lviv", "2 St", "Updated"));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = (await updateResponse.Content.ReadFromJsonAsync<VenueDetailResponse>())!;
        Assert.Equal("New Name", updated.Name);
        Assert.Equal("Lviv", updated.City);

        var court = (await (await client.PostAsJsonAsync($"/api/venues/{venue.Id}/courts",
            new CreateCourtRequest("Court", SportType.Padel, 100m, new TimeOnly(8, 0), new TimeOnly(20, 0))))
            .Content.ReadFromJsonAsync<CourtResponse>())!;

        var courtUpdate = await client.PutAsJsonAsync($"/api/courts/{court.Id}",
            new UpdateCourtRequest("Court Renamed", SportType.Padel, 120m, new TimeOnly(9, 0), new TimeOnly(21, 0), false));
        Assert.Equal(HttpStatusCode.OK, courtUpdate.StatusCode);
        var updatedCourt = (await courtUpdate.Content.ReadFromJsonAsync<CourtResponse>())!;
        Assert.Equal("Court Renamed", updatedCourt.Name);
        Assert.False(updatedCourt.IsActive);
    }
}
