using System.Net.Http.Headers;
using System.Net.Http.Json;
using SportBook.Application.Dtos;
using SportBook.Domain.Entities;
using SportBook.Domain.Enums;

namespace SportBook.IntegrationTests.TestInfrastructure;

/// <summary>Shared register/seed helpers so each test reads as scenario steps, not plumbing.</summary>
public static class ApiClientExtensions
{
    /// <summary>Registers a fresh account (unique email per call) and returns the auth payload.</summary>
    public static async Task<AuthResponse> RegisterAsync(this HttpClient client, string? name = null)
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(name ?? "Test User", email, "Test1234!"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    public static void UseBearer(this HttpClient client, string accessToken) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    /// <summary>
    /// Seeds a venue + court directly through the DbContext (US2 owner endpoints do not exist
    /// yet - tasks.md US1 independent-test note explicitly allows direct seeding).
    /// </summary>
    public static async Task<Court> SeedCourtAsync(this SportBookApiFactory factory, Guid ownerUserId,
        decimal pricePerHour = 100m, TimeOnly? openingTime = null, TimeOnly? closingTime = null)
    {
        Court? court = null;
        await factory.SeedAsync(db =>
        {
            var venue = new Venue
            {
                Id = Guid.NewGuid(),
                OwnerId = ownerUserId,
                Name = $"Venue {Guid.NewGuid():N}",
                City = "Kyiv",
                Address = "1 Test St",
                CreatedAt = DateTime.UtcNow,
            };
            court = new Court
            {
                Id = Guid.NewGuid(),
                VenueId = venue.Id,
                Name = "Court 1",
                SportType = SportType.Tennis,
                PricePerHour = pricePerHour,
                OpeningTime = openingTime ?? new TimeOnly(0, 0),
                ClosingTime = closingTime ?? new TimeOnly(23, 0),
                CreatedAt = DateTime.UtcNow,
            };
            db.Venues.Add(venue);
            db.Courts.Add(court);
            return db.SaveChangesAsync();
        });
        return court!;
    }

    /// <summary>Seeds a booking row directly, bypassing API validation - for cutoff tests that need near-past start times.</summary>
    public static async Task<Booking> SeedBookingAsync(this SportBookApiFactory factory,
        Guid courtId, Guid userId, DateTime start, DateTime end, BookingStatus status = BookingStatus.Pending)
    {
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            CourtId = courtId,
            UserId = userId,
            StartTime = start,
            EndTime = end,
            Status = status,
            TotalPrice = 100m,
            CreatedAt = DateTime.UtcNow,
        };
        await factory.SeedAsync(db =>
        {
            db.Bookings.Add(booking);
            return db.SaveChangesAsync();
        });
        return booking;
    }

    /// <summary>Tomorrow at the given UTC hour - always in the future and inside 00:00-23:00 court hours.</summary>
    public static DateTime TomorrowAt(int hour) =>
        DateTime.UtcNow.Date.AddDays(1).AddHours(hour);
}
