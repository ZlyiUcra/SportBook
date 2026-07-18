using System.Net;
using System.Net.Http.Json;
using SportBook.Application.Dtos;
using SportBook.IntegrationTests.TestInfrastructure;

namespace SportBook.IntegrationTests;

/// <summary>T020: venue create/update accepts `cityId` + optional both-or-neither `latitude`/`longitude`, rejects an unknown `cityId` and a partial coordinate pair (spec Acceptance Scenarios 1-3, US2).</summary>
[Collection(ApiCollection.Name)]
public class VenueLocationTests(ApiFixture fixture)
{
    [Fact]
    public async Task Create_with_valid_coordinates_stores_and_returns_them()
    {
        var client = fixture.Factory.CreateClient();
        var owner = await client.RegisterAsync("Owner");
        client.UseBearer(owner.AccessToken);

        var response = await client.PostAsJsonAsync("/api/venues",
            new CreateVenueRequest("Pinned Venue", ApiClientExtensions.KyivCityId, "1 St", null, 50.45m, 30.52m));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var venue = (await response.Content.ReadFromJsonAsync<VenueDetailResponse>())!;
        Assert.Equal(50.45m, venue.Latitude);
        Assert.Equal(30.52m, venue.Longitude);
    }

    [Fact]
    public async Task Create_without_coordinates_leaves_them_null()
    {
        var client = fixture.Factory.CreateClient();
        var owner = await client.RegisterAsync("Owner");
        client.UseBearer(owner.AccessToken);

        var response = await client.PostAsJsonAsync("/api/venues",
            new CreateVenueRequest("Unpinned Venue", ApiClientExtensions.KyivCityId, "1 St", null));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var venue = (await response.Content.ReadFromJsonAsync<VenueDetailResponse>())!;
        Assert.Null(venue.Latitude);
        Assert.Null(venue.Longitude);
    }

    [Fact]
    public async Task Create_rejects_an_unknown_cityId()
    {
        var client = fixture.Factory.CreateClient();
        var owner = await client.RegisterAsync("Owner");
        client.UseBearer(owner.AccessToken);

        var response = await client.PostAsJsonAsync("/api/venues",
            new CreateVenueRequest("Bad City Venue", 999_999_999, "1 St", null));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_rejects_a_partial_coordinate_pair()
    {
        var client = fixture.Factory.CreateClient();
        var owner = await client.RegisterAsync("Owner");
        client.UseBearer(owner.AccessToken);

        var response = await client.PostAsJsonAsync("/api/venues",
            new CreateVenueRequest("Half Pinned Venue", ApiClientExtensions.KyivCityId, "1 St", null, 50.45m, null));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_omitting_both_coordinates_clears_an_existing_pin()
    {
        var client = fixture.Factory.CreateClient();
        var owner = await client.RegisterAsync("Owner");
        client.UseBearer(owner.AccessToken);

        var venue = (await (await client.PostAsJsonAsync("/api/venues",
            new CreateVenueRequest("Venue With Pin", ApiClientExtensions.KyivCityId, "1 St", null, 50.45m, 30.52m)))
            .Content.ReadFromJsonAsync<VenueDetailResponse>())!;

        var updateResponse = await client.PutAsJsonAsync($"/api/venues/{venue.Id}",
            new UpdateVenueRequest("Venue With Pin", ApiClientExtensions.KyivCityId, "1 St", null));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = (await updateResponse.Content.ReadFromJsonAsync<VenueDetailResponse>())!;
        Assert.Null(updated.Latitude);
        Assert.Null(updated.Longitude);
    }
}
