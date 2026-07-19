using Microsoft.EntityFrameworkCore;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Application.Services;
using SportBook.Domain.Enums;
using SportBook.UnitTests.TestInfrastructure;

namespace SportBook.UnitTests;

/// <summary>
/// T008/T009 (005): the `BookingStatusFilter` maps each choice to the right stored-status/time
/// combination over materialized rows (Sqlite path), and the filter + the court->venue->city
/// Include translate to SQL (no client evaluation).
/// </summary>
public class BookingStatusFilterTests
{
    private static readonly DateTime Now = new(2026, 7, 19, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Each_filter_returns_only_its_group_and_stale_pending_only_under_all()
    {
        using var db = new TestDb();
        var (customer, court) = db.SeedCustomerAndCourt();

        var upcomingConfirmed = Seed(db, court.Id, customer.Id, BookingStatus.Confirmed, Now.AddHours(24), Now.AddHours(25));
        var upcomingPending = Seed(db, court.Id, customer.Id, BookingStatus.Pending, Now.AddHours(48), Now.AddHours(49));
        var completed = Seed(db, court.Id, customer.Id, BookingStatus.Confirmed, Now.AddHours(-25), Now.AddHours(-24));
        var cancelled = Seed(db, court.Id, customer.Id, BookingStatus.Cancelled, Now.AddHours(24), Now.AddHours(25));
        var stalePending = Seed(db, court.Id, customer.Id, BookingStatus.Pending, Now.AddHours(-25), Now.AddHours(-24));

        var service = new BookingService(db.Db, new FixedTimeProvider(Now));

        Assert.Equal(
            new[] { upcomingConfirmed, upcomingPending, completed, cancelled, stalePending }.OrderBy(x => x).ToArray(),
            (await Ids(service, customer.Id, BookingStatusFilter.All)).OrderBy(x => x).ToArray());

        Assert.Equal(
            new[] { upcomingConfirmed, upcomingPending }.OrderBy(x => x).ToArray(),
            (await Ids(service, customer.Id, BookingStatusFilter.Upcoming)).OrderBy(x => x).ToArray());

        Assert.Equal(new[] { completed }, await Ids(service, customer.Id, BookingStatusFilter.Completed));
        Assert.Equal(new[] { cancelled }, await Ids(service, customer.Id, BookingStatusFilter.Cancelled));
    }

    [Fact]
    public void Filter_predicate_and_detail_include_translate_to_sql()
    {
        using var db = new TestDb();

        // Mirrors ApplyStatusFilter(Upcoming) + IncludeDetail - EF throws at query-string time if
        // any of it falls back to client evaluation, so a non-throwing SQL string is the proof.
        var sql = db.Db.Bookings
            .Where(b => b.Status != BookingStatus.Cancelled && b.EndTime > Now)
            .Include(b => b.Court!).ThenInclude(c => c.Venue!).ThenInclude(v => v.City)
            .ToQueryString();

        Assert.Contains("WHERE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("EndTime", sql);
    }

    private static async Task<Guid[]> Ids(BookingService service, Guid userId, BookingStatusFilter status)
    {
        var result = await service.ListMineAsync(userId, status, new PageRequest { PageSize = 100 }, CancellationToken.None);
        return result.Items.Select(b => b.Id).ToArray();
    }

    private static Guid Seed(TestDb db, Guid courtId, Guid userId, BookingStatus status, DateTime start, DateTime end) =>
        db.SeedBooking(courtId, userId, start, end, status).Id;
}
