using System.Net.Http.Json;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Application.Features.Bookings.CreateBooking;
using SportBook.IntegrationTests.TestInfrastructure;

namespace SportBook.IntegrationTests;

/// <summary>
/// T004 (005): the booking response carries the venue/city/sport/court labels on both the customer
/// "My bookings" list and the owner "Venue bookings" list (shared shape). T005 (006): it also
/// carries `VenueId`, equal to its court's venue.
/// </summary>
[Collection(ApiCollection.Name)]
public class BookingDetailTests(ApiFixture fixture)
{
    [Fact]
    public async Task My_bookings_items_carry_venue_city_sport_and_court_detail()
    {
        var client = fixture.Factory.CreateClient();
        var auth = await client.RegisterAsync();
        client.UseBearer(auth.AccessToken);
        var owner = await fixture.Factory.CreateClient().RegisterAsync("Owner");
        var court = await fixture.Factory.SeedCourtAsync(owner.User.Id);

        var start = ApiClientExtensions.TomorrowAt(10);
        await client.PostAsJsonAsync("/api/bookings", new CreateBookingRequest(court.Id, start, start.AddHours(1)));

        var mine = await client.GetFromJsonAsync<PagedResponse<BookingResponse>>("/api/bookings");
        Assert.NotNull(mine);
        var booking = Assert.Single(mine.Items);
        Assert.False(string.IsNullOrWhiteSpace(booking.VenueName));
        Assert.Equal(ApiClientExtensions.KyivCityId, booking.City.Id);
        Assert.Equal("Tennis", booking.Sport);
        Assert.False(string.IsNullOrWhiteSpace(booking.CourtName));
        Assert.Equal(court.VenueId, booking.VenueId);
    }

    [Fact]
    public async Task Owner_venue_bookings_items_carry_the_same_detail_and_no_owner_id_leaks()
    {
        var customer = fixture.Factory.CreateClient();
        var customerAuth = await customer.RegisterAsync();
        customer.UseBearer(customerAuth.AccessToken);

        var ownerClient = fixture.Factory.CreateClient();
        var owner = await ownerClient.RegisterAsync("Owner");
        ownerClient.UseBearer(owner.AccessToken);
        var court = await fixture.Factory.SeedCourtAsync(owner.User.Id);

        var start = ApiClientExtensions.TomorrowAt(11);
        await customer.PostAsJsonAsync("/api/bookings", new CreateBookingRequest(court.Id, start, start.AddHours(1)));

        // The raw JSON must not expose an owner id - the response is only display labels + 001 fields.
        var raw = await ownerClient.GetStringAsync($"/api/venues/{court.VenueId}/bookings");
        Assert.DoesNotContain("ownerId", raw, StringComparison.OrdinalIgnoreCase);

        var list = await ownerClient.GetFromJsonAsync<PagedResponse<BookingResponse>>($"/api/venues/{court.VenueId}/bookings");
        Assert.NotNull(list);
        var booking = Assert.Single(list.Items);
        Assert.False(string.IsNullOrWhiteSpace(booking.VenueName));
        Assert.Equal(ApiClientExtensions.KyivCityId, booking.City.Id);
        Assert.Equal("Tennis", booking.Sport);
        Assert.False(string.IsNullOrWhiteSpace(booking.CourtName));
    }
}
