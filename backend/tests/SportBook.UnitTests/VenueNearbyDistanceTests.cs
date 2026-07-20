using SportBook.Application.Features.Venues.SearchNearbyVenues;
using SportBook.Domain.Entities;
using SportBook.Domain.Enums;
using SportBook.UnitTests.TestInfrastructure;

namespace SportBook.UnitTests;

/// <summary>
/// T010: the nearby distance/order/cap over materialized rows (Sqlite path) - proves
/// <see cref="SearchNearbyVenuesHandler.Handle"/> filters to
/// <see cref="SearchNearbyVenuesHandler.VenueRadiusKm"/>, orders nearest-first, and caps at 100,
/// entirely in C# over the materialized coordinate-bearing candidates (003 research.md "Distance
/// computation").
/// </summary>
public class VenueNearbyDistanceTests
{
    // Real GeoNames coordinates (same fixture set as CityNearestTests): Kyiv, Irpin (~19km away), Lviv (~470km away).
    private const decimal KyivLat = 50.45466m;
    private const decimal KyivLng = 30.5238m;
    private const decimal IrpinLat = 50.52201m;
    private const decimal IrpinLng = 30.24037m;
    private const decimal LvivLat = 49.83826m;
    private const decimal LvivLng = 24.02324m;

    [Fact]
    public async Task SearchNearbyAsync_filters_to_75km_and_orders_nearest_first()
    {
        using var db = new TestDb();
        var owner = SeedOwner(db);
        var city = TestDb.TestCity();
        db.Db.Cities.Add(city);
        var kyivVenue = SeedVenue(db, owner.Id, city.Id, KyivLat, KyivLng);
        var irpinVenue = SeedVenue(db, owner.Id, city.Id, IrpinLat, IrpinLng);
        var lvivVenue = SeedVenue(db, owner.Id, city.Id, LvivLat, LvivLng);
        db.Db.SaveChanges();

        var handler = new SearchNearbyVenuesHandler(db.Db);
        var result = await handler.Handle(new SearchNearbyVenuesQuery(KyivLat, KyivLng, null), CancellationToken.None);

        var ids = result.Select(v => v.Id).ToList();
        Assert.Contains(kyivVenue.Id, ids);
        Assert.Contains(irpinVenue.Id, ids);
        Assert.DoesNotContain(lvivVenue.Id, ids);
        Assert.True(ids.IndexOf(kyivVenue.Id) < ids.IndexOf(irpinVenue.Id), "expected the Kyiv venue (nearer) before the Irpin venue");
    }

    [Fact]
    public async Task SearchNearbyAsync_caps_at_100()
    {
        using var db = new TestDb();
        var owner = SeedOwner(db);
        var city = TestDb.TestCity();
        db.Db.Cities.Add(city);
        for (var i = 0; i < 105; i++)
        {
            SeedVenue(db, owner.Id, city.Id, KyivLat, KyivLng);
        }
        db.Db.SaveChanges();

        var handler = new SearchNearbyVenuesHandler(db.Db);
        var result = await handler.Handle(new SearchNearbyVenuesQuery(KyivLat, KyivLng, null), CancellationToken.None);

        Assert.Equal(100, result.Count);
    }

    private static User SeedOwner(TestDb db)
    {
        var owner = new User
        {
            Id = Guid.NewGuid(),
            Name = "owner@example.com",
            Email = "owner@example.com",
            PasswordHash = "not-a-real-hash",
            Role = Role.VenueOwner,
            CreatedAt = DateTime.UtcNow,
        };
        db.Db.Users.Add(owner);
        return owner;
    }

    private static Venue SeedVenue(TestDb db, Guid ownerId, int cityId, decimal lat, decimal lng)
    {
        var venue = new Venue
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = $"Venue {Guid.NewGuid():N}",
            CityId = cityId,
            Address = "1 Test St",
            Latitude = lat,
            Longitude = lng,
            CreatedAt = DateTime.UtcNow,
        };
        db.Db.Venues.Add(venue);
        return venue;
    }
}
