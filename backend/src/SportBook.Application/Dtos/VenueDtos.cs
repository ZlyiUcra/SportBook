namespace SportBook.Application.Dtos;

/// <summary>List-item shape for venue search; detail data (courts, rating) stays in <see cref="VenueDetailResponse"/>.</summary>
public record VenueSummaryResponse(Guid Id, string Name, string City, string Address, string? Description);

public record CourtResponse(
    Guid Id,
    Guid VenueId,
    string Name,
    string SportType,
    decimal PricePerHour,
    TimeOnly OpeningTime,
    TimeOnly ClosingTime,
    bool IsActive);

public record VenueDetailResponse(
    Guid Id,
    string Name,
    string City,
    string Address,
    string? Description,
    Guid OwnerId,
    IReadOnlyList<CourtResponse> Courts,
    double? AverageRating,
    int ReviewCount);
