using System.Net;
using System.Net.Http.Json;
using SportBook.Application.Dtos;
using SportBook.IntegrationTests.TestInfrastructure;

namespace SportBook.IntegrationTests;

/// <summary>T012: `GET /api/cities` suggestion matches partial input in any of the three app languages, ranks larger settlements first, rejects queries under 2 characters (spec Acceptance Scenarios 1, 3, 4, US1).</summary>
[Collection(ApiCollection.Name)]
public class CitySuggestionTests(ApiFixture fixture)
{
    [Fact]
    public async Task Suggest_matches_partial_input_in_english_and_ukrainian()
    {
        var client = fixture.Factory.CreateClient();
        var auth = await client.RegisterAsync();
        client.UseBearer(auth.AccessToken);

        var byEnglish = await client.GetFromJsonAsync<List<CityResponse>>("/api/cities?query=Kyi");
        Assert.NotNull(byEnglish);
        Assert.Contains(byEnglish, c => c.Id == ApiClientExtensions.KyivCityId);

        var byUkrainian = await client.GetFromJsonAsync<List<CityResponse>>("/api/cities?query=Ки");
        Assert.NotNull(byUkrainian);
        Assert.Contains(byUkrainian, c => c.Id == ApiClientExtensions.KyivCityId);
    }

    [Fact]
    public async Task Suggest_ranks_larger_settlements_first_and_disambiguates_by_region()
    {
        var client = fixture.Factory.CreateClient();
        var auth = await client.RegisterAsync();
        client.UseBearer(auth.AccessToken);

        var results = await client.GetFromJsonAsync<List<CityResponse>>("/api/cities?query=Andriivka");
        Assert.NotNull(results);
        var sameNamed = results.Where(c => c.NameEn == "Andriivka").ToList();
        Assert.True(sameNamed.Count >= 2, "expected at least two same-named settlements in the fixture dataset");

        Assert.Contains(sameNamed, c => c.RegionEn == "Odesa");
        Assert.Contains(sameNamed, c => c.RegionEn == "Poltava");
        // The Odesa settlement (population 1203) outranks the Poltava ones (<= 590) in the fixture data.
        Assert.Equal("Odesa", sameNamed[0].RegionEn);
    }

    [Fact]
    public async Task Suggest_rejects_queries_under_two_characters()
    {
        var client = fixture.Factory.CreateClient();
        var auth = await client.RegisterAsync();
        client.UseBearer(auth.AccessToken);

        var response = await client.GetAsync("/api/cities?query=k");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Suggest_requires_authentication()
    {
        var client = fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/cities?query=Kyi");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
