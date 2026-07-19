using System.Net.Http.Json;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Domain.Enums;
using SportBook.IntegrationTests.TestInfrastructure;

namespace SportBook.IntegrationTests;

/// <summary>
/// T012 (005): `GET /api/bookings?status=` returns only the selected group, default is All, and the
/// filter holds across pages (not just the first page).
/// </summary>
[Collection(ApiCollection.Name)]
public class BookingStatusFilterEndpointTests(ApiFixture fixture)
{
    [Fact]
    public async Task Status_filter_returns_only_its_group_and_default_is_all()
    {
        var client = fixture.Factory.CreateClient();
        var auth = await client.RegisterAsync();
        client.UseBearer(auth.AccessToken);
        var owner = await fixture.Factory.CreateClient().RegisterAsync("Owner");
        var court = await fixture.Factory.SeedCourtAsync(owner.User.Id);
        var now = DateTime.UtcNow;

        // Directly seed across statuses/times (bypasses overlap/past-start validation).
        var upcoming = (await fixture.Factory.SeedBookingAsync(court.Id, auth.User.Id,
            now.AddHours(24), now.AddHours(25), BookingStatus.Confirmed)).Id;
        var completed = (await fixture.Factory.SeedBookingAsync(court.Id, auth.User.Id,
            now.AddHours(-25), now.AddHours(-24), BookingStatus.Confirmed)).Id;
        var cancelled = (await fixture.Factory.SeedBookingAsync(court.Id, auth.User.Id,
            now.AddHours(48), now.AddHours(49), BookingStatus.Cancelled)).Id;

        var all = await client.GetFromJsonAsync<PagedResponse<BookingResponse>>("/api/bookings");
        Assert.NotNull(all);
        Assert.Equal(3, all.TotalCount);

        var upcomingOnly = await client.GetFromJsonAsync<PagedResponse<BookingResponse>>("/api/bookings?status=Upcoming");
        Assert.NotNull(upcomingOnly);
        Assert.Equal(new[] { upcoming }, upcomingOnly.Items.Select(b => b.Id).ToArray());

        var completedOnly = await client.GetFromJsonAsync<PagedResponse<BookingResponse>>("/api/bookings?status=Completed");
        Assert.NotNull(completedOnly);
        Assert.Equal(new[] { completed }, completedOnly.Items.Select(b => b.Id).ToArray());

        var cancelledOnly = await client.GetFromJsonAsync<PagedResponse<BookingResponse>>("/api/bookings?status=Cancelled");
        Assert.NotNull(cancelledOnly);
        Assert.Equal(new[] { cancelled }, cancelledOnly.Items.Select(b => b.Id).ToArray());
    }

    [Fact]
    public async Task Filter_holds_across_pages()
    {
        var client = fixture.Factory.CreateClient();
        var auth = await client.RegisterAsync();
        client.UseBearer(auth.AccessToken);
        var owner = await fixture.Factory.CreateClient().RegisterAsync("Owner");
        var court = await fixture.Factory.SeedCourtAsync(owner.User.Id);
        var now = DateTime.UtcNow;

        // Three cancelled + one upcoming; page the cancelled group at size 2 so page 2 exists.
        for (var i = 0; i < 3; i++)
        {
            await fixture.Factory.SeedBookingAsync(court.Id, auth.User.Id,
                now.AddHours(24 + i), now.AddHours(25 + i), BookingStatus.Cancelled);
        }
        await fixture.Factory.SeedBookingAsync(court.Id, auth.User.Id,
            now.AddHours(100), now.AddHours(101), BookingStatus.Confirmed);

        var page2 = await client.GetFromJsonAsync<PagedResponse<BookingResponse>>(
            "/api/bookings?status=Cancelled&pageSize=2&page=2");
        Assert.NotNull(page2);
        Assert.Equal(3, page2.TotalCount);
        Assert.Equal(2, page2.PageSize);
        Assert.Equal(2, page2.Page);
        var item = Assert.Single(page2.Items);
        Assert.Equal("Cancelled", item.Status);
    }
}
