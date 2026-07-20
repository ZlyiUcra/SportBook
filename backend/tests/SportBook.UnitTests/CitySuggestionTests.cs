using SportBook.Application.Services;
using SportBook.Domain.Entities;

namespace SportBook.UnitTests;

/// <summary>T014: city suggestion ranking and localized-name matching (CityService.Rank).</summary>
public class CitySuggestionTests
{
    private static City MakeCity(int id, string nameEn, string nameUk, string namePt, int population, string region = "Region") => new()
    {
        Id = id,
        NameEn = nameEn,
        NameUk = nameUk,
        NamePt = namePt,
        NameEs = nameEn,
        CountryCode = "UA",
        RegionEn = region,
        RegionUk = region,
        RegionPt = region,
        RegionEs = region,
        Latitude = 0,
        Longitude = 0,
        Population = population,
    };

    [Fact]
    public void Rank_matches_query_against_any_localized_name_column()
    {
        var cities = new[]
        {
            MakeCity(1, "Kyiv", "Київ", "Kyiv", 2_952_301),
            MakeCity(2, "Lviv", "Львів", "Lviv", 717_273),
        };

        Assert.Single(CityService.Rank(cities, "Kyi"), c => c.Id == 1);
        Assert.Single(CityService.Rank(cities, "Ки"), c => c.Id == 1);
        Assert.Empty(CityService.Rank(cities, "Odesa"));
    }

    [Fact]
    public void Rank_matching_is_case_insensitive()
    {
        var cities = new[] { MakeCity(1, "Kyiv", "Київ", "Kyiv", 2_952_301) };

        Assert.Single(CityService.Rank(cities, "kYI"), c => c.Id == 1);
    }

    [Fact]
    public void Rank_orders_matches_by_population_descending_and_respects_the_limit()
    {
        var cities = new[]
        {
            MakeCity(1, "Andriivka", "Андріївка", "Andriivka", 590, "Poltava"),
            MakeCity(2, "Andriivka", "Андріївка", "Andriivka", 1203, "Odesa"),
            MakeCity(3, "Andriivka", "Андріївка", "Andriivka", 578, "Poltava"),
        };

        var ranked = CityService.Rank(cities, "Andriivka", limit: 2);

        Assert.Equal(2, ranked.Count);
        Assert.Equal(2, ranked[0].Id);
        Assert.Equal(1, ranked[1].Id);
    }
}
