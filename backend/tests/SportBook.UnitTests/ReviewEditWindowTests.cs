using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Application.Exceptions;
using SportBook.Application.Services;
using SportBook.Domain.Enums;
using SportBook.UnitTests.TestInfrastructure;

namespace SportBook.UnitTests;

/// <summary>
/// T001 (007): replacing an existing review is only accepted within 24 hours of its original
/// CreatedAt - a prior replace never advances that window (data-model.md edit-window rule); a
/// first-time submission is never subject to this check.
/// </summary>
public class ReviewEditWindowTests
{
    private static readonly DateTime Now = new(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc);

    private static (Guid CustomerId, Guid VenueId) SeedEligibleReviewer(TestDb db)
    {
        var (customer, court) = db.SeedCustomerAndCourt();
        db.SeedBooking(court.Id, customer.Id, Now.AddHours(-48), Now.AddHours(-47), BookingStatus.Confirmed);
        return (customer.Id, court.VenueId);
    }

    [Fact]
    public async Task Replace_within_24h_of_original_creation_is_allowed()
    {
        using var db = new TestDb();
        var (customerId, venueId) = SeedEligibleReviewer(db);
        var service = new ReviewService(db.Db, new FixedTimeProvider(Now.AddHours(-23)));
        await service.CreateOrReplaceAsync(customerId, venueId, new CreateReviewRequest(3, "Original comment"), CancellationToken.None);

        var replaceService = new ReviewService(db.Db, new FixedTimeProvider(Now));
        var (response, created) = await replaceService.CreateOrReplaceAsync(
            customerId, venueId, new CreateReviewRequest(5, "Updated comment here"), CancellationToken.None);

        Assert.False(created);
        Assert.Equal(5, response.Rating);
        Assert.Equal("Updated comment here", response.Comment);
    }

    [Fact]
    public async Task Replace_after_24h_of_original_creation_is_rejected()
    {
        using var db = new TestDb();
        var (customerId, venueId) = SeedEligibleReviewer(db);
        var createService = new ReviewService(db.Db, new FixedTimeProvider(Now.AddHours(-25)));
        await createService.CreateOrReplaceAsync(customerId, venueId, new CreateReviewRequest(3, "Original comment"), CancellationToken.None);

        var replaceService = new ReviewService(db.Db, new FixedTimeProvider(Now));
        var ex = await Assert.ThrowsAsync<ApiException>(() => replaceService.CreateOrReplaceAsync(
            customerId, venueId, new CreateReviewRequest(5, "Updated comment here"), CancellationToken.None));

        Assert.Equal(409, ex.StatusCode);
        Assert.Equal("REVIEW_EDIT_WINDOW_CLOSED", ex.Code);
    }

    [Fact]
    public async Task A_replace_never_advances_the_window()
    {
        using var db = new TestDb();
        var (customerId, venueId) = SeedEligibleReviewer(db);
        var createTime = Now.AddHours(-23);
        var createService = new ReviewService(db.Db, new FixedTimeProvider(createTime));
        await createService.CreateOrReplaceAsync(customerId, venueId, new CreateReviewRequest(3, "Original comment"), CancellationToken.None);

        // First replace, still inside the window (1 hour after creation).
        var firstReplaceService = new ReviewService(db.Db, new FixedTimeProvider(createTime.AddHours(1)));
        await firstReplaceService.CreateOrReplaceAsync(customerId, venueId, new CreateReviewRequest(4, "First replace comment"), CancellationToken.None);

        // A second replace 25 hours after the ORIGINAL creation must be rejected, even though it is
        // only 24 hours after the first replace - the window is anchored to CreatedAt, not the edit.
        var secondReplaceService = new ReviewService(db.Db, new FixedTimeProvider(createTime.AddHours(25)));
        var ex = await Assert.ThrowsAsync<ApiException>(() => secondReplaceService.CreateOrReplaceAsync(
            customerId, venueId, new CreateReviewRequest(5, "Second replace comment"), CancellationToken.None));

        Assert.Equal("REVIEW_EDIT_WINDOW_CLOSED", ex.Code);
    }

    [Fact]
    public async Task First_time_submission_is_never_subject_to_the_edit_window()
    {
        using var db = new TestDb();
        var (customerId, venueId) = SeedEligibleReviewer(db);
        var service = new ReviewService(db.Db, new FixedTimeProvider(Now));
        var (response, created) = await service.CreateOrReplaceAsync(
            customerId, venueId, new CreateReviewRequest(4, "First ever review"), CancellationToken.None);

        Assert.True(created);
        Assert.Equal(4, response.Rating);
    }
}
