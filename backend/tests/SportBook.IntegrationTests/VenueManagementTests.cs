using System.Net;
using System.Net.Http.Json;
using System.Text;
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

        var venueResponse = await client.PostAsJsonAsync("/api/venues",
            new CreateVenueRequest("Test Venue", ApiClientExtensions.KyivCityId, "1 Main St", "A nice venue", null, null));
        Assert.Equal(HttpStatusCode.Created, venueResponse.StatusCode);
        var venue = (await venueResponse.Content.ReadFromJsonAsync<VenueDetailResponse>())!;

        var courtResponse = await client.PostAsJsonAsync($"/api/venues/{venue.Id}/courts",
            new CreateCourtRequest("Court 1", SportType.Tennis, 150m, new TimeOnly(8, 0), new TimeOnly(22, 0)));
        Assert.Equal(HttpStatusCode.Created, courtResponse.StatusCode);
        var court = (await courtResponse.Content.ReadFromJsonAsync<CourtResponse>())!;

        var searchResponse = await client.GetFromJsonAsync<PagedResponse<VenueSummaryResponse>>(
            $"/api/venues?cityId={ApiClientExtensions.KyivCityId}");
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
            new CreateVenueRequest("Old Name", ApiClientExtensions.KyivCityId, "1 St", null, null, null)))
            .Content.ReadFromJsonAsync<VenueDetailResponse>())!;

        var updateResponse = await client.PutAsJsonAsync($"/api/venues/{venue.Id}",
            new UpdateVenueRequest("New Name", ApiClientExtensions.LvivCityId, "2 St", "Updated", null, null));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = (await updateResponse.Content.ReadFromJsonAsync<VenueDetailResponse>())!;
        Assert.Equal("New Name", updated.Name);
        Assert.Equal(ApiClientExtensions.LvivCityId, updated.City.Id);

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

    /// <summary>
    /// Regression test for the Minimal API migration (consilium 2026-07-20): every other test in
    /// this file posts `CreateCourtRequest`/`UpdateCourtRequest` as a typed C# record via
    /// `PostAsJsonAsync`, which - with no custom `JsonSerializerOptions` on the test client -
    /// serializes the `SportType` enum as a bare integer, the same shape .NET deserializes with
    /// no converter at all. That makes the whole suite structurally blind to whether the server's
    /// `JsonStringEnumConverter` (registered via `ConfigureHttpJsonOptions` in `Program.cs`) is
    /// actually wired up. The real frontend sends `sportType` as a JSON STRING (its TS enum is
    /// string-valued) - this test reproduces that exact wire shape with a raw string body,
    /// bypassing the test client's own enum serialization entirely.
    /// </summary>
    [Fact]
    public async Task Creating_a_court_with_a_raw_string_valued_sportType_body_succeeds()
    {
        var client = fixture.Factory.CreateClient();
        var owner = await client.RegisterAsync("Owner");
        client.UseBearer(owner.AccessToken);

        var venue = (await (await client.PostAsJsonAsync("/api/venues",
            new CreateVenueRequest("String Enum Venue", ApiClientExtensions.KyivCityId, "1 St", null, null, null)))
            .Content.ReadFromJsonAsync<VenueDetailResponse>())!;

        var rawJson = """{"name":"Court 1","sportType":"Tennis","pricePerHour":150,"openingTime":"08:00:00","closingTime":"22:00:00"}""";
        var response = await client.PostAsync(
            $"/api/venues/{venue.Id}/courts", new StringContent(rawJson, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var court = (await response.Content.ReadFromJsonAsync<CourtResponse>())!;
        Assert.Equal("Tennis", court.SportType);
    }
}
