using SportBook.Application.Services;

namespace SportBook.UnitTests;

/// <summary>T025: nearest-city resolution via the shared haversine function (CityDistance.DistanceKm).</summary>
public class CityNearestTests
{
    // Real GeoNames coordinates (research.md fixture set): Kyiv, Lviv, Irpin (near Kyiv).
    private const double KyivLat = 50.45466;
    private const double KyivLng = 30.5238;
    private const double LvivLat = 49.83826;
    private const double LvivLng = 24.02324;
    private const double IrpinLat = 50.52201;
    private const double IrpinLng = 30.24037;

    [Fact]
    public void DistanceKm_between_a_city_and_itself_is_zero()
    {
        Assert.Equal(0, CityDistance.DistanceKm(KyivLat, KyivLng, KyivLat, KyivLng), precision: 6);
    }

    [Fact]
    public void DistanceKm_is_symmetric()
    {
        var forward = CityDistance.DistanceKm(KyivLat, KyivLng, LvivLat, LvivLng);
        var backward = CityDistance.DistanceKm(LvivLat, LvivLng, KyivLat, KyivLng);
        Assert.Equal(forward, backward, precision: 9);
    }

    [Fact]
    public void Nearest_city_by_distance_picks_the_closer_settlement()
    {
        var cities = new[]
        {
            (Id: "Kyiv", Lat: KyivLat, Lng: KyivLng),
            (Id: "Lviv", Lat: LvivLat, Lng: LvivLng),
            (Id: "Irpin", Lat: IrpinLat, Lng: IrpinLng),
        };

        // A position right in central Kyiv should resolve to Kyiv, not the nearby-but-farther Irpin or the far Lviv.
        var nearest = cities
            .OrderBy(c => CityDistance.DistanceKm(KyivLat, KyivLng, c.Lat, c.Lng))
            .First();

        Assert.Equal("Kyiv", nearest.Id);
    }

    [Fact]
    public void Irpin_is_within_150km_of_kyiv_and_lviv_is_not()
    {
        var toIrpin = CityDistance.DistanceKm(KyivLat, KyivLng, IrpinLat, IrpinLng);
        var toLviv = CityDistance.DistanceKm(KyivLat, KyivLng, LvivLat, LvivLng);

        Assert.True(toIrpin <= CityDistance.NearbyRadiusKm, $"expected Irpin within {CityDistance.NearbyRadiusKm}km, was {toIrpin}km");
        Assert.True(toLviv > CityDistance.NearbyRadiusKm, $"expected Lviv beyond {CityDistance.NearbyRadiusKm}km, was {toLviv}km");
    }
}
