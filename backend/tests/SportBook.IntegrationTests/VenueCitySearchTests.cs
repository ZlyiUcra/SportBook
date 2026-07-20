using System.Net.Http.Json;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Application.Features.Venues.CreateVenue;
using SportBook.Application.Features.Venues.SearchVenues;
using SportBook.IntegrationTests.TestInfrastructure;

namespace SportBook.IntegrationTests;

/// <summary>T013: `GET /api/venues?cityId=` returns only venues of the selected city (spec Acceptance Scenario 2, US1).</summary>
[Collection(ApiCollection.Name)]
public class VenueCitySearchTests(ApiFixture fixture)
{
    [Fact]
    public async Task Search_by_cityId_returns_only_that_citys_venues()
    {
        var ownerClient = fixture.Factory.CreateClient();
        var owner = await ownerClient.RegisterAsync("Owner");
        ownerClient.UseBearer(owner.AccessToken);

        var kyivVenue = (await (await ownerClient.PostAsJsonAsync("/api/venues",
            new CreateVenueRequest("Kyiv Venue", ApiClientExtensions.KyivCityId, "1 St", null)))
            .Content.ReadFromJsonAsync<VenueDetailResponse>())!;
        var lvivVenue = (await (await ownerClient.PostAsJsonAsync("/api/venues",
            new CreateVenueRequest("Lviv Venue", ApiClientExtensions.LvivCityId, "2 St", null)))
            .Content.ReadFromJsonAsync<VenueDetailResponse>())!;

        var kyivResults = await ownerClient.GetFromJsonAsync<PagedResponse<VenueSummaryResponse>>(
            $"/api/venues?cityId={ApiClientExtensions.KyivCityId}");
        Assert.NotNull(kyivResults);
        Assert.Contains(kyivResults.Items, v => v.Id == kyivVenue.Id);
        Assert.DoesNotContain(kyivResults.Items, v => v.Id == lvivVenue.Id);
        Assert.All(kyivResults.Items, v => Assert.Equal(ApiClientExtensions.KyivCityId, v.City.Id));
    }
}
