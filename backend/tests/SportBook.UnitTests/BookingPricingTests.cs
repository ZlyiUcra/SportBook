using SportBook.Application.Dtos;
using SportBook.Application.Features.Bookings.CreateBooking;
using SportBook.UnitTests.TestInfrastructure;

namespace SportBook.UnitTests;

/// <summary>
/// T024: TotalPrice is computed server-side from Court.PricePerHour and duration (spec FR-003).
/// The request DTO carries no price field at all, so a client cannot even attempt to supply one -
/// that structural guarantee is asserted here too.
/// </summary>
public class BookingPricingTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 16, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(1, 150.50, 150.50)]
    [InlineData(2, 150.50, 301.00)]
    [InlineData(3, 100.00, 300.00)]
    public async Task Create_computes_total_price_from_hourly_rate_and_duration(
        int hours, decimal pricePerHour, decimal expectedTotal)
    {
        using var testDb = new TestDb();
        var (customer, court) = testDb.SeedCustomerAndCourt(pricePerHour);
        var handler = new CreateBookingHandler(testDb.Db, new FixedTimeProvider(Now));

        var start = new DateTime(2026, 7, 17, 10, 0, 0, DateTimeKind.Utc);
        var response = await handler.Handle(
            new CreateBookingCommand(customer.Id, court.Id, start, start.AddHours(hours)), CancellationToken.None);

        Assert.Equal(expectedTotal, response.TotalPrice);
        Assert.Equal("Pending", response.Status);
    }

    [Fact]
    public void CreateBookingRequest_has_no_price_or_user_field()
    {
        var propertyNames = typeof(CreateBookingRequest).GetProperties().Select(p => p.Name).ToArray();
        Assert.DoesNotContain("TotalPrice", propertyNames);
        Assert.DoesNotContain("UserId", propertyNames);
    }
}
