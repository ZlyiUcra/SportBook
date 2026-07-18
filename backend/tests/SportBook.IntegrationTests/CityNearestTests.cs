using System.Net;
using System.Net.Http.Json;
using SportBook.Application.Dtos;
using SportBook.IntegrationTests.TestInfrastructure;

namespace SportBook.IntegrationTests;

/// <summary>T024: `GET /api/cities/nearest` resolves the nearest city and validates `lat`/`lng` ranges (spec Acceptance Scenarios, US3).</summary>
[Collection(ApiCollection.Name)]
public class CityNearestTests(ApiFixture fixture)
{
    [Fact]
    public async Task Nearest_resolves_kyiv_from_kyivs_own_coordinates()
    {
        var client = fixture.Factory.CreateClient();
        var auth = await client.RegisterAsync();
        client.UseBearer(auth.AccessToken);

        // Kyiv's own GeoNames coordinates, not just "a position inside Kyiv" - the directory
        // also includes finer-grained neighborhoods/sub-localities (e.g. "Stare Misto"), so an
        // arbitrary nearby point can legitimately resolve to one of those instead of the city
        // itself. Querying the exact coordinate guarantees a zero-distance, unambiguous match.
        var nearest = await client.GetFromJsonAsync<CityResponse>("/api/cities/nearest?lat=50.45466&lng=30.5238");
        Assert.NotNull(nearest);
        Assert.Equal(ApiClientExtensions.KyivCityId, nearest.Id);
    }

    [Theory]
    [InlineData(91, 30.52)]
    [InlineData(-91, 30.52)]
    [InlineData(50.45, 181)]
    [InlineData(50.45, -181)]
    public async Task Nearest_rejects_out_of_range_coordinates(double lat, double lng)
    {
        var client = fixture.Factory.CreateClient();
        var auth = await client.RegisterAsync();
        client.UseBearer(auth.AccessToken);

        var response = await client.GetAsync($"/api/cities/nearest?lat={lat}&lng={lng}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Nearest_requires_authentication()
    {
        var client = fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/cities/nearest?lat=50.45&lng=30.52");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
