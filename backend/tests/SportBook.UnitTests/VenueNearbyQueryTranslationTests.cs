using Microsoft.EntityFrameworkCore;
using SportBook.Domain.Enums;
using SportBook.UnitTests.TestInfrastructure;

namespace SportBook.UnitTests;

/// <summary>
/// Query-translation guards for both nearby-search paths: T030 (002) proves
/// the city-neighbor `CityId IN &lt;set&gt;` filter translates; T011 (003) proves the
/// coordinate-radius `Latitude != null` (+ optional sport) filter translates and pushes no
/// trigonometry into SQL - distance itself is computed in C# over the materialized candidates
/// (003 research.md "Distance computation").
/// </summary>
public class VenueNearbyQueryTranslationTests
{
    [Fact]
    public void CityId_Contains_neighbor_set_filter_translates_to_sql()
    {
        using var db = new TestDb();
        var cityIds = new List<int> { 703448, 707565, 702550 };

        // EF throws at query-string/execution time if a Where clause cannot be translated
        // server-side and falls back to client evaluation for a filter like this - so a
        // non-throwing, non-empty SQL string is itself the proof this stays server-side.
        var sql = db.Db.Venues.Where(v => cityIds.Contains(v.CityId)).ToQueryString();

        Assert.Contains("WHERE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CityId", sql);
    }

    [Fact]
    public void Latitude_not_null_filter_with_optional_sport_translates_to_sql_with_no_trigonometry()
    {
        using var db = new TestDb();

        var sql = db.Db.Venues
            .Where(v => v.Latitude != null)
            .Where(v => v.Courts.Any(c => c.SportType == SportType.Tennis && c.IsActive))
            .ToQueryString();

        Assert.Contains("WHERE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Latitude", sql);
        Assert.DoesNotContain("SIN(", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("COS(", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ATN2(", sql, StringComparison.OrdinalIgnoreCase);
    }
}
