namespace SportBook.Application.Dtos;

public record CreateReviewRequest(int Rating, string? Comment);

public record ReviewResponse(Guid Id, Guid VenueId, Guid UserId, string UserName, int Rating, string? Comment, DateTime CreatedAt);
