using Microsoft.EntityFrameworkCore;
using SportBook.UnitTests.TestInfrastructure;

namespace SportBook.UnitTests;

/// <summary>T030: `ToQueryString()` proves the `CityId IN &lt;neighbor set&gt;` filter translates to SQL rather than client-evaluating (research.md Nearby-cities computation shape).</summary>
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
}
