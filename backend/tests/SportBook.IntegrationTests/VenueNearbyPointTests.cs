using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using SportBook.Application.Dtos;
using SportBook.Application.Features.Courts.CreateCourt;
using SportBook.Application.Features.Venues.CreateVenue;
using SportBook.Application.Features.Venues.SearchNearbyVenues;
using SportBook.Domain.Enums;
using SportBook.IntegrationTests.TestInfrastructure;

namespace SportBook.IntegrationTests;

/// <summary>
/// T009: `GET /api/venues/nearby` returns venues within 75km ordered nearest-first with
/// `distanceKm`, excludes venues beyond 75km, rejects out-of-range `lat`/`lng`, honors
/// `sportType`, and requires auth (spec Acceptance Scenarios, US1).
/// </summary>
[Collection(ApiCollection.Name)]
public class VenueNearbyPointTests(ApiFixture fixture)
{
    // Real GeoNames coordinates (same fixture set as CityNearestTests): Kyiv, Lviv (~470km away), Irpin (~19km away).
    private const decimal KyivLat = 50.45466m;
    private const decimal KyivLng = 30.5238m;
    private const decimal IrpinLat = 50.52201m;
    private const decimal IrpinLng = 30.24037m;
    private const decimal LvivLat = 49.83826m;
    private const decimal LvivLng = 24.02324m;

    // The query string must not depend on the test runner's current culture (decimal.ToString()
    // uses it by default, e.g. a comma decimal separator would corrupt the URL).
    private static string Invariant(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    [Fact]
    public async Task Nearby_returns_venues_within_75km_ordered_nearest_first_and_excludes_beyond()
    {
        var client = fixture.Factory.CreateClient();
        var owner = await client.RegisterAsync("Owner");
        client.UseBearer(owner.AccessToken);

        var kyivVenue = (await (await client.PostAsJsonAsync("/api/venues",
            new CreateVenueRequest("Kyiv Venue", ApiClientExtensions.KyivCityId, "1 St", null, KyivLat, KyivLng)))
            .Content.ReadFromJsonAsync<VenueDetailResponse>())!;
        var irpinVenue = (await (await client.PostAsJsonAsync("/api/venues",
            new CreateVenueRequest("Irpin Venue", ApiClientExtensions.KyivCityId, "2 St", null, IrpinLat, IrpinLng)))
            .Content.ReadFromJsonAsync<VenueDetailResponse>())!;
        var lvivVenue = (await (await client.PostAsJsonAsync("/api/venues",
            new CreateVenueRequest("Lviv Venue", ApiClientExtensions.LvivCityId, "3 St", null, LvivLat, LvivLng)))
            .Content.ReadFromJsonAsync<VenueDetailResponse>())!;

        var results = await client.GetFromJsonAsync<List<NearbyVenueResponse>>(
            $"/api/venues/nearby?lat={Invariant(KyivLat)}&lng={Invariant(KyivLng)}");

        Assert.NotNull(results);
        Assert.DoesNotContain(results, v => v.Id == lvivVenue.Id);
        var kyivResult = Assert.Single(results, v => v.Id == kyivVenue.Id);
        var irpinResult = Assert.Single(results, v => v.Id == irpinVenue.Id);
        Assert.True(kyivResult.DistanceKm < 1m, $"expected near-zero distance, was {kyivResult.DistanceKm}km");
        Assert.True(irpinResult.DistanceKm <= 75m, $"expected Irpin within 75km, was {irpinResult.DistanceKm}km");

        var order = results.Select(v => v.Id).ToList();
        Assert.True(order.IndexOf(kyivVenue.Id) < order.IndexOf(irpinVenue.Id), "expected the Kyiv venue (nearer) before the Irpin venue");
    }

    [Theory]
    [InlineData(91, 30.52)]
    [InlineData(-91, 30.52)]
    [InlineData(50.45, 181)]
    [InlineData(50.45, -181)]
    public async Task Nearby_rejects_out_of_range_lat_or_lng(decimal lat, decimal lng)
    {
        var client = fixture.Factory.CreateClient();
        var owner = await client.RegisterAsync("Owner");
        client.UseBearer(owner.AccessToken);

        var response = await client.GetAsync($"/api/venues/nearby?lat={Invariant(lat)}&lng={Invariant(lng)}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Nearby_honors_sportType()
    {
        var client = fixture.Factory.CreateClient();
        var owner = await client.RegisterAsync("Owner");
        client.UseBearer(owner.AccessToken);

        var tennisVenue = (await (await client.PostAsJsonAsync("/api/venues",
            new CreateVenueRequest("Tennis Venue", ApiClientExtensions.KyivCityId, "1 St", null, KyivLat, KyivLng)))
            .Content.ReadFromJsonAsync<VenueDetailResponse>())!;
        await client.PostAsJsonAsync($"/api/venues/{tennisVenue.Id}/courts",
            new CreateCourtRequest("Court 1", SportType.Tennis, 100m, new TimeOnly(0, 0), new TimeOnly(23, 0)));

        var footballVenue = (await (await client.PostAsJsonAsync("/api/venues",
            new CreateVenueRequest("Football Venue", ApiClientExtensions.KyivCityId, "2 St", null, IrpinLat, IrpinLng)))
            .Content.ReadFromJsonAsync<VenueDetailResponse>())!;
        await client.PostAsJsonAsync($"/api/venues/{footballVenue.Id}/courts",
            new CreateCourtRequest("Court 1", SportType.Football, 100m, new TimeOnly(0, 0), new TimeOnly(23, 0)));

        var results = await client.GetFromJsonAsync<List<NearbyVenueResponse>>(
            $"/api/venues/nearby?lat={Invariant(KyivLat)}&lng={Invariant(KyivLng)}&sportType=Tennis");

        Assert.NotNull(results);
        Assert.Contains(results, v => v.Id == tennisVenue.Id);
        Assert.DoesNotContain(results, v => v.Id == footballVenue.Id);
    }

    [Fact]
    public async Task Nearby_requires_auth()
    {
        var client = fixture.Factory.CreateClient();

        var response = await client.GetAsync($"/api/venues/nearby?lat={Invariant(KyivLat)}&lng={Invariant(KyivLng)}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
