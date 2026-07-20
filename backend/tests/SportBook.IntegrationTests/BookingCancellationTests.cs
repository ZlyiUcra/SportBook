using System.Net;
using System.Net.Http.Json;
using SportBook.Application.Dtos;
using SportBook.Application.Features.Availability.GetAvailability;
using SportBook.Application.Features.Bookings.CreateBooking;
using SportBook.IntegrationTests.TestInfrastructure;

namespace SportBook.IntegrationTests;

/// <summary>
/// T023: cancellation is rejected inside the 2h cutoff and succeeds outside it (spec Acceptance
/// Scenario 3, FR-005). Near-cutoff bookings are seeded directly because the create API itself
/// would reject them only for other reasons at unlucky wall-clock times (operating hours).
/// </summary>
[Collection(ApiCollection.Name)]
public class BookingCancellationTests(ApiFixture fixture)
{
    [Fact]
    public async Task Cancel_inside_the_2h_cutoff_returns_409()
    {
        var client = fixture.Factory.CreateClient();
        var auth = await client.RegisterAsync();
        client.UseBearer(auth.AccessToken);
        var owner = await fixture.Factory.CreateClient().RegisterAsync("Owner");
        var court = await fixture.Factory.SeedCourtAsync(owner.User.Id);

        var start = DateTime.UtcNow.AddHours(1);
        var booking = await fixture.Factory.SeedBookingAsync(court.Id, auth.User.Id, start, start.AddHours(1));

        var response = await client.PutAsync($"/api/bookings/{booking.Id}/cancel", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_outside_the_cutoff_succeeds_and_frees_the_slot()
    {
        var client = fixture.Factory.CreateClient();
        var auth = await client.RegisterAsync();
        client.UseBearer(auth.AccessToken);
        var owner = await fixture.Factory.CreateClient().RegisterAsync("Owner");
        var court = await fixture.Factory.SeedCourtAsync(owner.User.Id);

        var start = ApiClientExtensions.TomorrowAt(12);
        var created = await client.PostAsJsonAsync("/api/bookings",
            new CreateBookingRequest(court.Id, start, start.AddHours(1)));
        var booking = (await created.Content.ReadFromJsonAsync<BookingResponse>())!;

        var cancel = await client.PutAsync($"/api/bookings/{booking.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        var cancelled = await cancel.Content.ReadFromJsonAsync<BookingResponse>();
        Assert.NotNull(cancelled);
        Assert.Equal("Cancelled", cancelled.Status);

        // The slot must reappear in availability (quickstart Scenario 4 step 5).
        var date = DateOnly.FromDateTime(start);
        var availability = await client.GetFromJsonAsync<AvailabilityResponse>(
            $"/api/courts/{court.Id}/availability?date={date:yyyy-MM-dd}");
        Assert.NotNull(availability);
        Assert.Contains(availability.FreeSlots, s => s.Start == start);
    }

    [Fact]
    public async Task Customer_cannot_view_or_cancel_another_customers_booking()
    {
        var ownerOfBookingClient = fixture.Factory.CreateClient();
        var ownerOfBookingAuth = await ownerOfBookingClient.RegisterAsync();
        ownerOfBookingClient.UseBearer(ownerOfBookingAuth.AccessToken);

        var strangerClient = fixture.Factory.CreateClient();
        var strangerAuth = await strangerClient.RegisterAsync();
        strangerClient.UseBearer(strangerAuth.AccessToken);

        var owner = await fixture.Factory.CreateClient().RegisterAsync("Owner");
        var court = await fixture.Factory.SeedCourtAsync(owner.User.Id);
        var start = ApiClientExtensions.TomorrowAt(9);
        var created = await ownerOfBookingClient.PostAsJsonAsync("/api/bookings",
            new CreateBookingRequest(court.Id, start, start.AddHours(1)));
        var booking = (await created.Content.ReadFromJsonAsync<BookingResponse>())!;

        var view = await strangerClient.GetAsync($"/api/bookings/{booking.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, view.StatusCode);

        var cancel = await strangerClient.PutAsync($"/api/bookings/{booking.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.Forbidden, cancel.StatusCode);
    }
}
