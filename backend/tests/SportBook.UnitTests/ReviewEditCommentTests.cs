using SportBook.Application.Exceptions;
using SportBook.Application.Features.Reviews.CreateOrReplaceReview;
using SportBook.Domain.Enums;
using SportBook.UnitTests.TestInfrastructure;

namespace SportBook.UnitTests;

/// <summary>
/// T006 (007): replacing an existing review requires a comment of at least 10 characters (after
/// trimming); a first-time submission is never subject to this check (data-model.md edit-comment
/// rule).
/// </summary>
public class ReviewEditCommentTests
{
    private static readonly DateTime Now = new(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc);

    private static (Guid CustomerId, Guid VenueId) SeedEligibleReviewer(TestDb db)
    {
        var (customer, court) = db.SeedCustomerAndCourt();
        db.SeedBooking(court.Id, customer.Id, Now.AddHours(-48), Now.AddHours(-47), BookingStatus.Confirmed);
        return (customer.Id, court.VenueId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Too short")]
    public async Task Replacing_with_a_missing_empty_or_under_length_comment_is_rejected(string? comment)
    {
        using var db = new TestDb();
        var (customerId, venueId) = SeedEligibleReviewer(db);
        var createHandler = new CreateOrReplaceReviewHandler(db.Db, new FixedTimeProvider(Now));
        await createHandler.Handle(new CreateOrReplaceReviewCommand(customerId, venueId, 3, "Original comment"), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<ApiException>(() => createHandler.Handle(
            new CreateOrReplaceReviewCommand(customerId, venueId, 4, comment), CancellationToken.None).AsTask());

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal("REVIEW_COMMENT_TOO_SHORT", ex.Code);
    }

    [Fact]
    public async Task Replacing_with_a_10_character_comment_is_accepted()
    {
        using var db = new TestDb();
        var (customerId, venueId) = SeedEligibleReviewer(db);
        var createHandler = new CreateOrReplaceReviewHandler(db.Db, new FixedTimeProvider(Now));
        await createHandler.Handle(new CreateOrReplaceReviewCommand(customerId, venueId, 3, "Original comment"), CancellationToken.None);

        var result = await createHandler.Handle(
            new CreateOrReplaceReviewCommand(customerId, venueId, 4, "1234567890"), CancellationToken.None);

        Assert.False(result.Created);
        Assert.Equal("1234567890", result.Response.Comment);
    }

    [Fact]
    public async Task First_time_submission_with_no_comment_is_accepted_regardless_of_length()
    {
        using var db = new TestDb();
        var (customerId, venueId) = SeedEligibleReviewer(db);
        var handler = new CreateOrReplaceReviewHandler(db.Db, new FixedTimeProvider(Now));

        var result = await handler.Handle(
            new CreateOrReplaceReviewCommand(customerId, venueId, 4, null), CancellationToken.None);

        Assert.True(result.Created);
        Assert.Null(result.Response.Comment);
    }
}
