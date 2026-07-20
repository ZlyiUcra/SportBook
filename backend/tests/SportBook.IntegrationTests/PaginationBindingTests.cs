using System.Net.Http.Json;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Domain.Entities;
using SportBook.Domain.Enums;
using SportBook.IntegrationTests.TestInfrastructure;

namespace SportBook.IntegrationTests;

/// <summary>
/// Regression guard for the list endpoints that bind <see cref="PageRequest"/> from the query
/// string. A complex <c>[FromQuery]</c> parameter named <c>page</c> collides with the <c>page</c>
/// query key and the binder silently falls back to defaults, so paging never advances past page 1 -
/// these tests pin the param name (<c>paging</c>) by asserting page 2 returns a different slice
/// than page 1 and that the response echoes the bound page/pageSize.
/// </summary>
[Collection(ApiCollection.Name)]
public class PaginationBindingTests(ApiFixture fixture)
{
    [Fact]
    public async Task Venues_list_advances_past_page_one()
    {
        var client = fixture.Factory.CreateClient();
        var owner = await client.RegisterAsync("Owner");
        client.UseBearer(owner.AccessToken);

        for (var i = 0; i < 3; i++)
        {
            await client.PostAsJsonAsync("/api/venues",
                new CreateVenueRequest($"Venue {i}", ApiClientExtensions.KyivCityId, $"{i} St", null));
        }

        var page2 = await client.GetFromJsonAsync<PagedResponse<VenueSummaryResponse>>(
            "/api/venues?mine=true&pageSize=2&page=2");
        Assert.NotNull(page2);
        Assert.Equal(3, page2.TotalCount);
        Assert.Equal(2, page2.PageSize);
        Assert.Equal(2, page2.Page);
        Assert.Single(page2.Items);
    }

    [Fact]
    public async Task Courts_list_advances_past_page_one()
    {
        var client = fixture.Factory.CreateClient();
        var owner = await client.RegisterAsync("Owner");
        client.UseBearer(owner.AccessToken);

        // One venue holding three courts - SeedCourtAsync makes a venue per court, so seed directly.
        var venueId = Guid.NewGuid();
        await fixture.Factory.SeedAsync(db =>
        {
            db.Venues.Add(new Venue
            {
                Id = venueId,
                OwnerId = owner.User.Id,
                Name = "Venue",
                CityId = ApiClientExtensions.KyivCityId,
                Address = "1 St",
                CreatedAt = DateTime.UtcNow,
            });
            for (var i = 0; i < 3; i++)
            {
                db.Courts.Add(new Court
                {
                    Id = Guid.NewGuid(),
                    VenueId = venueId,
                    Name = $"Court {i}",
                    SportType = SportType.Tennis,
                    PricePerHour = 100m,
                    OpeningTime = new TimeOnly(0, 0),
                    ClosingTime = new TimeOnly(23, 0),
                    CreatedAt = DateTime.UtcNow,
                });
            }
            return db.SaveChangesAsync();
        });

        var page2 = await client.GetFromJsonAsync<PagedResponse<CourtResponse>>(
            $"/api/venues/{venueId}/courts?pageSize=2&page=2");
        Assert.NotNull(page2);
        Assert.Equal(3, page2.TotalCount);
        Assert.Equal(2, page2.PageSize);
        Assert.Equal(2, page2.Page);
        Assert.Single(page2.Items);
    }

    [Fact]
    public async Task Reviews_list_advances_past_page_one()
    {
        var client = fixture.Factory.CreateClient();
        var owner = await client.RegisterAsync("Owner");
        client.UseBearer(owner.AccessToken);

        var venue = (await (await client.PostAsJsonAsync("/api/venues",
            new CreateVenueRequest("Venue", ApiClientExtensions.KyivCityId, "1 St", null)))
            .Content.ReadFromJsonAsync<VenueDetailResponse>())!;
        var court = await fixture.Factory.SeedCourtAsync(owner.User.Id, venueId: venue.Id);
        var now = DateTime.UtcNow;

        // Three reviews from three distinct users, each with a completed booking (006 eligibility gate).
        for (var i = 0; i < 3; i++)
        {
            var reviewer = fixture.Factory.CreateClient();
            var reviewerAuth = await reviewer.RegisterAsync();
            reviewer.UseBearer(reviewerAuth.AccessToken);
            await fixture.Factory.SeedBookingAsync(court.Id, reviewerAuth.User.Id,
                now.AddHours(-25), now.AddHours(-24), BookingStatus.Confirmed);
            await reviewer.PostAsJsonAsync($"/api/venues/{venue.Id}/reviews",
                new CreateReviewRequest(5, null));
        }

        var page2 = await client.GetFromJsonAsync<PagedResponse<ReviewResponse>>(
            $"/api/venues/{venue.Id}/reviews?pageSize=2&page=2");
        Assert.NotNull(page2);
        Assert.Equal(3, page2.TotalCount);
        Assert.Equal(2, page2.PageSize);
        Assert.Equal(2, page2.Page);
        Assert.Single(page2.Items);
    }
}
