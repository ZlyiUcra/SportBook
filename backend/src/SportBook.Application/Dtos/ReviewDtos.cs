namespace SportBook.Application.Dtos;

public record ReviewResponse(Guid Id, Guid VenueId, Guid UserId, string UserName, int Rating, string? Comment, DateTime CreatedAt);
