using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SportBook.Domain.Entities;
using SportBook.Domain.Enums;
using SportBook.Infrastructure;

namespace SportBook.UnitTests.TestInfrastructure;

/// <summary>
/// Sqlite in-memory database per test (plan.md testing decision) - the connection must stay open
/// for the database's lifetime, so this type owns and disposes it together with the context.
/// </summary>
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection;

    public SportBookDbContext Db { get; }

    public TestDb()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<SportBookDbContext>()
            .UseSqlite(_connection)
            .Options;
        Db = new SportBookDbContext(options);
        Db.Database.EnsureCreated();
    }

    /// <summary>Seeds the minimum graph a booking needs: one owner, one venue, one court.</summary>
    public (User Customer, Court Court) SeedCustomerAndCourt(
        decimal pricePerHour = 100m,
        TimeOnly? openingTime = null,
        TimeOnly? closingTime = null)
    {
        var owner = NewUser("owner@example.com", Role.VenueOwner);
        var customer = NewUser("customer@example.com", Role.Customer);
        var venue = new Venue
        {
            Id = Guid.NewGuid(),
            OwnerId = owner.Id,
            Name = "Test Venue",
            City = "Kyiv",
            Address = "1 Test St",
            CreatedAt = DateTime.UtcNow,
        };
        var court = new Court
        {
            Id = Guid.NewGuid(),
            VenueId = venue.Id,
            Name = "Court 1",
            SportType = SportType.Tennis,
            PricePerHour = pricePerHour,
            OpeningTime = openingTime ?? new TimeOnly(8, 0),
            ClosingTime = closingTime ?? new TimeOnly(22, 0),
            CreatedAt = DateTime.UtcNow,
        };

        Db.Users.AddRange(owner, customer);
        Db.Venues.Add(venue);
        Db.Courts.Add(court);
        Db.SaveChanges();
        return (customer, court);
    }

    public Booking SeedBooking(Guid courtId, Guid userId, DateTime start, DateTime end, BookingStatus status)
    {
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            CourtId = courtId,
            UserId = userId,
            StartTime = start,
            EndTime = end,
            Status = status,
            TotalPrice = 0m,
            CreatedAt = DateTime.UtcNow,
        };
        Db.Bookings.Add(booking);
        Db.SaveChanges();
        return booking;
    }

    private static User NewUser(string email, Role role) => new()
    {
        Id = Guid.NewGuid(),
        Name = email,
        Email = email,
        PasswordHash = "not-a-real-hash",
        Role = role,
        CreatedAt = DateTime.UtcNow,
    };

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}

/// <summary>Deterministic clock so cutoff and past-start rules can be tested exactly.</summary>
public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
