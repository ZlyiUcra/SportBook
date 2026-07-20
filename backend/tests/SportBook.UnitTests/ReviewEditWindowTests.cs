using SportBook.Application.Exceptions;
using SportBook.Application.Features.Reviews.CreateOrReplaceReview;
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
        var handler = new CreateOrReplaceReviewHandler(db.Db, new FixedTimeProvider(Now.AddHours(-23)));
        await handler.Handle(new CreateOrReplaceReviewCommand(customerId, venueId, 3, "Original comment"), CancellationToken.None);

        var replaceHandler = new CreateOrReplaceReviewHandler(db.Db, new FixedTimeProvider(Now));
        var result = await replaceHandler.Handle(
            new CreateOrReplaceReviewCommand(customerId, venueId, 5, "Updated comment here"), CancellationToken.None);

        Assert.False(result.Created);
        Assert.Equal(5, result.Response.Rating);
        Assert.Equal("Updated comment here", result.Response.Comment);
    }

    [Fact]
    public async Task Replace_after_24h_of_original_creation_is_rejected()
    {
        using var db = new TestDb();
        var (customerId, venueId) = SeedEligibleReviewer(db);
        var createHandler = new CreateOrReplaceReviewHandler(db.Db, new FixedTimeProvider(Now.AddHours(-25)));
        await createHandler.Handle(new CreateOrReplaceReviewCommand(customerId, venueId, 3, "Original comment"), CancellationToken.None);

        var replaceHandler = new CreateOrReplaceReviewHandler(db.Db, new FixedTimeProvider(Now));
        var ex = await Assert.ThrowsAsync<ApiException>(() => replaceHandler.Handle(
            new CreateOrReplaceReviewCommand(customerId, venueId, 5, "Updated comment here"), CancellationToken.None).AsTask());

        Assert.Equal(409, ex.StatusCode);
        Assert.Equal("REVIEW_EDIT_WINDOW_CLOSED", ex.Code);
    }

    [Fact]
    public async Task A_replace_never_advances_the_window()
    {
        using var db = new TestDb();
        var (customerId, venueId) = SeedEligibleReviewer(db);
        var createTime = Now.AddHours(-23);
        var createHandler = new CreateOrReplaceReviewHandler(db.Db, new FixedTimeProvider(createTime));
        await createHandler.Handle(new CreateOrReplaceReviewCommand(customerId, venueId, 3, "Original comment"), CancellationToken.None);

        // First replace, still inside the window (1 hour after creation).
        var firstReplaceHandler = new CreateOrReplaceReviewHandler(db.Db, new FixedTimeProvider(createTime.AddHours(1)));
        await firstReplaceHandler.Handle(new CreateOrReplaceReviewCommand(customerId, venueId, 4, "First replace comment"), CancellationToken.None);

        // A second replace 25 hours after the ORIGINAL creation must be rejected, even though it is
        // only 24 hours after the first replace - the window is anchored to CreatedAt, not the edit.
        var secondReplaceHandler = new CreateOrReplaceReviewHandler(db.Db, new FixedTimeProvider(createTime.AddHours(25)));
        var ex = await Assert.ThrowsAsync<ApiException>(() => secondReplaceHandler.Handle(
            new CreateOrReplaceReviewCommand(customerId, venueId, 5, "Second replace comment"), CancellationToken.None).AsTask());

        Assert.Equal("REVIEW_EDIT_WINDOW_CLOSED", ex.Code);
    }

    [Fact]
    public async Task First_time_submission_is_never_subject_to_the_edit_window()
    {
        using var db = new TestDb();
        var (customerId, venueId) = SeedEligibleReviewer(db);
        var handler = new CreateOrReplaceReviewHandler(db.Db, new FixedTimeProvider(Now));
        var result = await handler.Handle(
            new CreateOrReplaceReviewCommand(customerId, venueId, 4, "First ever review"), CancellationToken.None);

        Assert.True(result.Created);
        Assert.Equal(4, result.Response.Rating);
    }
}
