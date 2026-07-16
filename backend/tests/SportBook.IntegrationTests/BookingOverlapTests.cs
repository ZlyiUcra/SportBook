using System.Net;
using System.Net.Http.Json;
using SportBook.Application.Dtos;
using SportBook.IntegrationTests.TestInfrastructure;

namespace SportBook.IntegrationTests;

/// <summary>
/// T022: overlapping booking is rejected, including two concurrent requests where only one may
/// succeed (spec Acceptance Scenario 2, FR-004) - validated against the real SQL Server engine,
/// since the serializable-transaction semantics are exactly what Sqlite cannot reproduce.
/// </summary>
[Collection(ApiCollection.Name)]
public class BookingOverlapTests(ApiFixture fixture)
{
    [Fact]
    public async Task Second_overlapping_booking_by_another_customer_returns_409()
    {
        var firstClient = fixture.Factory.CreateClient();
        var firstAuth = await firstClient.RegisterAsync();
        firstClient.UseBearer(firstAuth.AccessToken);

        var secondClient = fixture.Factory.CreateClient();
        var secondAuth = await secondClient.RegisterAsync();
        secondClient.UseBearer(secondAuth.AccessToken);

        var owner = await fixture.Factory.CreateClient().RegisterAsync("Owner");
        var court = await fixture.Factory.SeedCourtAsync(owner.User.Id);
        var start = ApiClientExtensions.TomorrowAt(10);

        var first = await firstClient.PostAsJsonAsync("/api/bookings",
            new CreateBookingRequest(court.Id, start, start.AddHours(2)));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        // Overlaps the second hour of the existing booking.
        var second = await secondClient.PostAsJsonAsync("/api/bookings",
            new CreateBookingRequest(court.Id, start.AddHours(1), start.AddHours(3)));
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Concurrent_requests_for_the_same_slot_produce_exactly_one_booking()
    {
        var owner = await fixture.Factory.CreateClient().RegisterAsync("Owner");
        var court = await fixture.Factory.SeedCourtAsync(owner.User.Id);
        var start = ApiClientExtensions.TomorrowAt(18);

        var clients = new List<HttpClient>();
        for (var i = 0; i < 5; i++)
        {
            var client = fixture.Factory.CreateClient();
            var auth = await client.RegisterAsync();
            client.UseBearer(auth.AccessToken);
            clients.Add(client);
        }

        var responses = await Task.WhenAll(clients.Select(client =>
            client.PostAsJsonAsync("/api/bookings",
                new CreateBookingRequest(court.Id, start, start.AddHours(1)))));

        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(responses.Length - 1, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
    }
}
