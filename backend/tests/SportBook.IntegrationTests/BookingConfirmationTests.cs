using System.Net;
using System.Net.Http.Json;
using SportBook.Application.Dtos;
using SportBook.IntegrationTests.TestInfrastructure;

namespace SportBook.IntegrationTests;

/// <summary>T042: the venue owner confirms a pending booking; a non-owner confirm attempt is rejected (spec FR-011).</summary>
[Collection(ApiCollection.Name)]
public class BookingConfirmationTests(ApiFixture fixture)
{
    [Fact]
    public async Task Owner_confirms_a_pending_booking()
    {
        var ownerClient = fixture.Factory.CreateClient();
        var owner = await ownerClient.RegisterAsync("Owner");
        ownerClient.UseBearer(owner.AccessToken);
        var court = await fixture.Factory.SeedCourtAsync(owner.User.Id);

        var customerClient = fixture.Factory.CreateClient();
        var customer = await customerClient.RegisterAsync();
        var booking = await fixture.Factory.SeedBookingAsync(
            court.Id, customer.User.Id, ApiClientExtensions.TomorrowAt(9), ApiClientExtensions.TomorrowAt(10));

        var confirm = await ownerClient.PutAsync($"/api/bookings/{booking.Id}/confirm", null);
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
        var confirmed = await confirm.Content.ReadFromJsonAsync<BookingResponse>();
        Assert.NotNull(confirmed);
        Assert.Equal("Confirmed", confirmed.Status);
    }

    [Fact]
    public async Task Non_owner_confirm_attempt_is_rejected()
    {
        var ownerClient = fixture.Factory.CreateClient();
        var owner = await ownerClient.RegisterAsync("Owner");
        ownerClient.UseBearer(owner.AccessToken);
        var court = await fixture.Factory.SeedCourtAsync(owner.User.Id);

        var customerClient = fixture.Factory.CreateClient();
        var customer = await customerClient.RegisterAsync();
        customerClient.UseBearer(customer.AccessToken);
        var booking = await fixture.Factory.SeedBookingAsync(
            court.Id, customer.User.Id, ApiClientExtensions.TomorrowAt(9), ApiClientExtensions.TomorrowAt(10));

        // The booking's own customer is not the venue owner, so they cannot confirm it either.
        var byCustomer = await customerClient.PutAsync($"/api/bookings/{booking.Id}/confirm", null);
        Assert.Equal(HttpStatusCode.Forbidden, byCustomer.StatusCode);

        var strangerClient = fixture.Factory.CreateClient();
        var stranger = await strangerClient.RegisterAsync("StrangerOwner");
        strangerClient.UseBearer(stranger.AccessToken);
        var byStranger = await strangerClient.PutAsync($"/api/bookings/{booking.Id}/confirm", null);
        Assert.Equal(HttpStatusCode.Forbidden, byStranger.StatusCode);
    }
}
