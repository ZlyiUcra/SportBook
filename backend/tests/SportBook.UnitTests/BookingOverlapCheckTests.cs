using SportBook.Application.Dtos;
using SportBook.Application.Exceptions;
using SportBook.Application.Services;
using SportBook.Domain.Enums;
using SportBook.UnitTests.TestInfrastructure;

namespace SportBook.UnitTests;

/// <summary>
/// T025: overlap-check logic for [StartTime, EndTime) ranges on one court (spec FR-004,
/// data-model.md Booking validation). Cancelled bookings must not block a slot.
/// </summary>
public class BookingOverlapCheckTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTime Day = new(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);

    private static DateTime At(int hour) => Day.AddHours(hour);

    [Theory]
    [InlineData(10, 11, 10, 11)] // exact match
    [InlineData(10, 12, 11, 12)] // new range swallows existing tail
    [InlineData(10, 11, 9, 12)]  // new range fully covers existing
    [InlineData(10, 12, 11, 13)] // partial overlap at the end
    public async Task Create_rejects_overlap_with_pending_booking(
        int existingStart, int existingEnd, int newStart, int newEnd)
    {
        using var testDb = new TestDb();
        var (customer, court) = testDb.SeedCustomerAndCourt();
        testDb.SeedBooking(court.Id, customer.Id, At(existingStart), At(existingEnd), BookingStatus.Pending);
        var service = new BookingService(testDb.Db, new FixedTimeProvider(Now));

        var ex = await Assert.ThrowsAsync<ApiException>(() => service.CreateAsync(
            customer.Id, new CreateBookingRequest(court.Id, At(newStart), At(newEnd)), CancellationToken.None));

        Assert.Equal(409, ex.StatusCode);
        Assert.Equal("SLOT_TAKEN", ex.Code);
    }

    [Fact]
    public async Task Create_rejects_overlap_with_confirmed_booking()
    {
        using var testDb = new TestDb();
        var (customer, court) = testDb.SeedCustomerAndCourt();
        testDb.SeedBooking(court.Id, customer.Id, At(10), At(11), BookingStatus.Confirmed);
        var service = new BookingService(testDb.Db, new FixedTimeProvider(Now));

        var ex = await Assert.ThrowsAsync<ApiException>(() => service.CreateAsync(
            customer.Id, new CreateBookingRequest(court.Id, At(10), At(11)), CancellationToken.None));

        Assert.Equal("SLOT_TAKEN", ex.Code);
    }

    [Fact]
    public async Task Create_allows_adjacent_booking_without_gap()
    {
        using var testDb = new TestDb();
        var (customer, court) = testDb.SeedCustomerAndCourt();
        testDb.SeedBooking(court.Id, customer.Id, At(10), At(11), BookingStatus.Pending);
        var service = new BookingService(testDb.Db, new FixedTimeProvider(Now));

        var response = await service.CreateAsync(
            customer.Id, new CreateBookingRequest(court.Id, At(11), At(12)), CancellationToken.None);

        Assert.Equal(At(11), response.StartTime);
    }

    [Fact]
    public async Task Create_allows_slot_held_only_by_cancelled_booking()
    {
        using var testDb = new TestDb();
        var (customer, court) = testDb.SeedCustomerAndCourt();
        testDb.SeedBooking(court.Id, customer.Id, At(10), At(11), BookingStatus.Cancelled);
        var service = new BookingService(testDb.Db, new FixedTimeProvider(Now));

        var response = await service.CreateAsync(
            customer.Id, new CreateBookingRequest(court.Id, At(10), At(11)), CancellationToken.None);

        Assert.Equal("Pending", response.Status);
    }
}
