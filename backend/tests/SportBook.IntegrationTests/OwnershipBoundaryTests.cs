using System.Net;
using System.Net.Http.Json;
using SportBook.Application.Dtos;
using SportBook.Domain.Enums;
using SportBook.IntegrationTests.TestInfrastructure;

namespace SportBook.IntegrationTests;

/// <summary>T041: cross-owner access to another owner's venue/court is rejected with 403.</summary>
[Collection(ApiCollection.Name)]
public class OwnershipBoundaryTests(ApiFixture fixture)
{
    [Fact]
    public async Task Cross_owner_cannot_update_or_delete_another_owners_venue()
    {
        var ownerClient = fixture.Factory.CreateClient();
        var owner = await ownerClient.RegisterAsync("Owner");
        ownerClient.UseBearer(owner.AccessToken);
        var venue = (await (await ownerClient.PostAsJsonAsync("/api/venues",
            new CreateVenueRequest("Venue", ApiClientExtensions.KyivCityId, "1 St", null))).Content.ReadFromJsonAsync<VenueDetailResponse>())!;

        var strangerClient = fixture.Factory.CreateClient();
        var stranger = await strangerClient.RegisterAsync("Stranger");
        strangerClient.UseBearer(stranger.AccessToken);

        var update = await strangerClient.PutAsJsonAsync($"/api/venues/{venue.Id}",
            new UpdateVenueRequest("Hijacked", ApiClientExtensions.LvivCityId, "2 St", null));
        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);

        var delete = await strangerClient.DeleteAsync($"/api/venues/{venue.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task Cross_owner_cannot_create_update_or_delete_a_court_on_another_owners_venue()
    {
        var ownerClient = fixture.Factory.CreateClient();
        var owner = await ownerClient.RegisterAsync("Owner");
        ownerClient.UseBearer(owner.AccessToken);
        var venue = (await (await ownerClient.PostAsJsonAsync("/api/venues",
            new CreateVenueRequest("Venue", ApiClientExtensions.KyivCityId, "1 St", null))).Content.ReadFromJsonAsync<VenueDetailResponse>())!;
        var court = (await (await ownerClient.PostAsJsonAsync($"/api/venues/{venue.Id}/courts",
            new CreateCourtRequest("Court", SportType.Tennis, 100m, new TimeOnly(8, 0), new TimeOnly(20, 0))))
            .Content.ReadFromJsonAsync<CourtResponse>())!;

        var strangerClient = fixture.Factory.CreateClient();
        var stranger = await strangerClient.RegisterAsync("Stranger");
        strangerClient.UseBearer(stranger.AccessToken);

        var create = await strangerClient.PostAsJsonAsync($"/api/venues/{venue.Id}/courts",
            new CreateCourtRequest("Intruder Court", SportType.Tennis, 100m, new TimeOnly(8, 0), new TimeOnly(20, 0)));
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);

        var update = await strangerClient.PutAsJsonAsync($"/api/courts/{court.Id}",
            new UpdateCourtRequest("Hijacked", SportType.Tennis, 100m, new TimeOnly(8, 0), new TimeOnly(20, 0), true));
        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);

        var delete = await strangerClient.DeleteAsync($"/api/courts/{court.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }
}
