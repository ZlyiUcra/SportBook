using System.Net;
using System.Net.Http.Json;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.IntegrationTests.TestInfrastructure;

namespace SportBook.IntegrationTests;

/// <summary>T021: book an available slot end-to-end, price computed server-side (spec Acceptance Scenario 1).</summary>
[Collection(ApiCollection.Name)]
public class BookingCreationTests(ApiFixture fixture)
{
    [Fact]
    public async Task Booking_an_available_slot_returns_201_pending_with_server_computed_price()
    {
        var client = fixture.Factory.CreateClient();
        var auth = await client.RegisterAsync();
        client.UseBearer(auth.AccessToken);
        var owner = await fixture.Factory.CreateClient().RegisterAsync("Owner");
        var court = await fixture.Factory.SeedCourtAsync(owner.User.Id, pricePerHour: 150.50m);

        var start = ApiClientExtensions.TomorrowAt(10);
        var response = await client.PostAsJsonAsync("/api/bookings",
            new CreateBookingRequest(court.Id, start, start.AddHours(2)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var booking = await response.Content.ReadFromJsonAsync<BookingResponse>();
        Assert.NotNull(booking);
        Assert.Equal("Pending", booking.Status);
        Assert.Equal(301.00m, booking.TotalPrice);
        Assert.Equal(auth.User.Id, booking.UserId);

        var mine = await client.GetFromJsonAsync<PagedResponse<BookingResponse>>("/api/bookings");
        Assert.NotNull(mine);
        Assert.Contains(mine.Items, b => b.Id == booking.Id);
    }

    [Fact]
    public async Task Booking_outside_operating_hours_returns_409()
    {
        var client = fixture.Factory.CreateClient();
        var auth = await client.RegisterAsync();
        client.UseBearer(auth.AccessToken);
        var owner = await fixture.Factory.CreateClient().RegisterAsync("Owner");
        var court = await fixture.Factory.SeedCourtAsync(owner.User.Id,
            openingTime: new TimeOnly(8, 0), closingTime: new TimeOnly(10, 0));

        var start = ApiClientExtensions.TomorrowAt(12);
        var response = await client.PostAsJsonAsync("/api/bookings",
            new CreateBookingRequest(court.Id, start, start.AddHours(1)));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Booking_with_start_in_the_past_returns_400()
    {
        var client = fixture.Factory.CreateClient();
        var auth = await client.RegisterAsync();
        client.UseBearer(auth.AccessToken);
        var owner = await fixture.Factory.CreateClient().RegisterAsync("Owner");
        var court = await fixture.Factory.SeedCourtAsync(owner.User.Id);

        var start = DateTime.UtcNow.Date.AddDays(-1).AddHours(10);
        var response = await client.PostAsJsonAsync("/api/bookings",
            new CreateBookingRequest(court.Id, start, start.AddHours(1)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Booked_slot_disappears_from_availability()
    {
        var client = fixture.Factory.CreateClient();
        var auth = await client.RegisterAsync();
        client.UseBearer(auth.AccessToken);
        var owner = await fixture.Factory.CreateClient().RegisterAsync("Owner");
        var court = await fixture.Factory.SeedCourtAsync(owner.User.Id);

        var start = ApiClientExtensions.TomorrowAt(14);
        var date = DateOnly.FromDateTime(start);

        var before = await client.GetFromJsonAsync<AvailabilityResponse>(
            $"/api/courts/{court.Id}/availability?date={date:yyyy-MM-dd}");
        Assert.NotNull(before);
        Assert.Contains(before.FreeSlots, s => s.Start == start);

        var created = await client.PostAsJsonAsync("/api/bookings",
            new CreateBookingRequest(court.Id, start, start.AddHours(1)));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var after = await client.GetFromJsonAsync<AvailabilityResponse>(
            $"/api/courts/{court.Id}/availability?date={date:yyyy-MM-dd}");
        Assert.NotNull(after);
        Assert.DoesNotContain(after.FreeSlots, s => s.Start == start);
    }
}
