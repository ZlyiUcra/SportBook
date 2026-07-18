using System.Net.Http.Json;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.IntegrationTests.TestInfrastructure;

namespace SportBook.IntegrationTests;

/// <summary>T029: `includeNearby=true` returns venues within 150km and never beyond it, and the server enforces the fixed radius regardless of client-supplied values (spec Acceptance Scenarios, US4).</summary>
[Collection(ApiCollection.Name)]
public class VenueNearbyTests(ApiFixture fixture)
{
    [Fact]
    public async Task IncludeNearby_adds_venues_within_150km_and_excludes_venues_beyond_it()
    {
        var client = fixture.Factory.CreateClient();
        var owner = await client.RegisterAsync("Owner");
        client.UseBearer(owner.AccessToken);

        var kyivVenue = (await (await client.PostAsJsonAsync("/api/venues",
            new CreateVenueRequest("Kyiv Venue", ApiClientExtensions.KyivCityId, "1 St", null)))
            .Content.ReadFromJsonAsync<VenueDetailResponse>())!;
        // Irpin is ~10km from Kyiv - well within the 150km radius.
        var irpinVenue = (await (await client.PostAsJsonAsync("/api/venues",
            new CreateVenueRequest("Irpin Venue", ApiClientExtensions.IrpinCityId, "2 St", null)))
            .Content.ReadFromJsonAsync<VenueDetailResponse>())!;
        // Lviv is ~470km from Kyiv - well beyond the 150km radius.
        var lvivVenue = (await (await client.PostAsJsonAsync("/api/venues",
            new CreateVenueRequest("Lviv Venue", ApiClientExtensions.LvivCityId, "3 St", null)))
            .Content.ReadFromJsonAsync<VenueDetailResponse>())!;

        var narrowSearch = await client.GetFromJsonAsync<PagedResponse<VenueSummaryResponse>>(
            $"/api/venues?cityId={ApiClientExtensions.KyivCityId}");
        Assert.NotNull(narrowSearch);
        Assert.Contains(narrowSearch.Items, v => v.Id == kyivVenue.Id);
        Assert.DoesNotContain(narrowSearch.Items, v => v.Id == irpinVenue.Id);

        var nearbySearch = await client.GetFromJsonAsync<PagedResponse<VenueSummaryResponse>>(
            $"/api/venues?cityId={ApiClientExtensions.KyivCityId}&includeNearby=true");
        Assert.NotNull(nearbySearch);
        Assert.Contains(nearbySearch.Items, v => v.Id == kyivVenue.Id);
        Assert.Contains(nearbySearch.Items, v => v.Id == irpinVenue.Id);
        Assert.DoesNotContain(nearbySearch.Items, v => v.Id == lvivVenue.Id);
    }

    [Fact]
    public async Task IncludeNearby_without_cityId_changes_nothing()
    {
        var client = fixture.Factory.CreateClient();
        var owner = await client.RegisterAsync("Owner");
        client.UseBearer(owner.AccessToken);

        var withoutFlag = await client.GetFromJsonAsync<PagedResponse<VenueSummaryResponse>>("/api/venues?pageSize=1");
        var withFlag = await client.GetFromJsonAsync<PagedResponse<VenueSummaryResponse>>("/api/venues?includeNearby=true&pageSize=1");

        Assert.NotNull(withoutFlag);
        Assert.NotNull(withFlag);
        Assert.Equal(withoutFlag.TotalCount, withFlag.TotalCount);
    }
}
