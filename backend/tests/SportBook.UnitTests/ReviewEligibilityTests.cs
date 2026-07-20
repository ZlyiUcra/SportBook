using SportBook.Application.Exceptions;
using SportBook.Application.Features.Reviews.CreateOrReplaceReview;
using SportBook.Domain.Entities;
using SportBook.Domain.Enums;
using SportBook.UnitTests.TestInfrastructure;

namespace SportBook.UnitTests;

/// <summary>
/// T001 (006): a review may be created or replaced only by a user with a Confirmed, past booking
/// on a court of the target venue (data-model.md eligibility rule) - every other booking state is
/// rejected, and the rating check still fires independently of eligibility.
/// </summary>
public class ReviewEligibilityTests
{
    private static readonly DateTime Now = new(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Confirmed_past_booking_on_the_venue_makes_the_user_eligible()
    {
        using var db = new TestDb();
        var (customer, court) = db.SeedCustomerAndCourt();
        db.SeedBooking(court.Id, customer.Id, Now.AddHours(-25), Now.AddHours(-24), BookingStatus.Confirmed);

        var handler = new CreateOrReplaceReviewHandler(db.Db, new FixedTimeProvider(Now));
        var result = await handler.Handle(
            new CreateOrReplaceReviewCommand(customer.Id, court.VenueId, 4, "Great court"), CancellationToken.None);

        Assert.True(result.Created);
        Assert.Equal(4, result.Response.Rating);
    }

    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.Cancelled)]
    public async Task Non_confirmed_past_booking_is_not_eligible(BookingStatus status)
    {
        using var db = new TestDb();
        var (customer, court) = db.SeedCustomerAndCourt();
        db.SeedBooking(court.Id, customer.Id, Now.AddHours(-25), Now.AddHours(-24), status);

        var handler = new CreateOrReplaceReviewHandler(db.Db, new FixedTimeProvider(Now));
        var ex = await Assert.ThrowsAsync<ApiException>(() => handler.Handle(
            new CreateOrReplaceReviewCommand(customer.Id, court.VenueId, 4, null), CancellationToken.None));

        Assert.Equal(409, ex.StatusCode);
        Assert.Equal("REVIEW_NOT_ELIGIBLE", ex.Code);
    }

    [Fact]
    public async Task Confirmed_future_booking_is_not_eligible()
    {
        using var db = new TestDb();
        var (customer, court) = db.SeedCustomerAndCourt();
        db.SeedBooking(court.Id, customer.Id, Now.AddHours(24), Now.AddHours(25), BookingStatus.Confirmed);

        var handler = new CreateOrReplaceReviewHandler(db.Db, new FixedTimeProvider(Now));
        var ex = await Assert.ThrowsAsync<ApiException>(() => handler.Handle(
            new CreateOrReplaceReviewCommand(customer.Id, court.VenueId, 4, null), CancellationToken.None));

        Assert.Equal("REVIEW_NOT_ELIGIBLE", ex.Code);
    }

    [Fact]
    public async Task No_booking_at_all_is_not_eligible()
    {
        using var db = new TestDb();
        var (customer, court) = db.SeedCustomerAndCourt();

        var handler = new CreateOrReplaceReviewHandler(db.Db, new FixedTimeProvider(Now));
        var ex = await Assert.ThrowsAsync<ApiException>(() => handler.Handle(
            new CreateOrReplaceReviewCommand(customer.Id, court.VenueId, 4, null), CancellationToken.None));

        Assert.Equal("REVIEW_NOT_ELIGIBLE", ex.Code);
    }

    [Fact]
    public async Task Completed_game_at_a_different_venue_does_not_confer_eligibility()
    {
        using var db = new TestDb();
        var (customer, court) = db.SeedCustomerAndCourt();
        db.SeedBooking(court.Id, customer.Id, Now.AddHours(-25), Now.AddHours(-24), BookingStatus.Confirmed);

        var otherVenue = new Venue
        {
            Id = Guid.NewGuid(),
            OwnerId = customer.Id,
            Name = "Other Venue",
            CityId = TestDb.TestCity().Id,
            Address = "2 Test St",
            CreatedAt = DateTime.UtcNow,
        };
        db.Db.Venues.Add(otherVenue);
        db.Db.SaveChanges();

        var handler = new CreateOrReplaceReviewHandler(db.Db, new FixedTimeProvider(Now));
        var ex = await Assert.ThrowsAsync<ApiException>(() => handler.Handle(
            new CreateOrReplaceReviewCommand(customer.Id, otherVenue.Id, 4, null), CancellationToken.None));

        Assert.Equal("REVIEW_NOT_ELIGIBLE", ex.Code);
    }

    [Fact]
    public async Task Bad_rating_is_rejected_regardless_of_eligibility()
    {
        using var db = new TestDb();
        var (customer, court) = db.SeedCustomerAndCourt();
        db.SeedBooking(court.Id, customer.Id, Now.AddHours(-25), Now.AddHours(-24), BookingStatus.Confirmed);

        var handler = new CreateOrReplaceReviewHandler(db.Db, new FixedTimeProvider(Now));
        var ex = await Assert.ThrowsAsync<ApiException>(() => handler.Handle(
            new CreateOrReplaceReviewCommand(customer.Id, court.VenueId, 6, null), CancellationToken.None));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal("INVALID_RATING", ex.Code);
    }
}
