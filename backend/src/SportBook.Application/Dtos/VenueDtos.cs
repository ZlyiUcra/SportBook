namespace SportBook.Application.Dtos;

public record CourtResponse(
    Guid Id,
    Guid VenueId,
    string Name,
    string SportType,
    decimal PricePerHour,
    TimeOnly OpeningTime,
    TimeOnly ClosingTime,
    bool IsActive);

/// <summary>`Latitude`/`Longitude` are null when the owner has not set a pin - no city-centre fallback (spec FR-010).</summary>
public record VenueDetailResponse(
    Guid Id,
    string Name,
    CityResponse City,
    string Address,
    string? Description,
    decimal? Latitude,
    decimal? Longitude,
    Guid OwnerId,
    IReadOnlyList<CourtResponse> Courts,
    double? AverageRating,
    int ReviewCount);
