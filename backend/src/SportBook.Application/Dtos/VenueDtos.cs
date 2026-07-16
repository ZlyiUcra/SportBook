using SportBook.Domain.Enums;

namespace SportBook.Application.Dtos;

/// <summary>No `ownerId` field - the owner is always the authenticated caller (research.md Authorization checklist).</summary>
public record CreateVenueRequest(string Name, string City, string Address, string? Description);

public record UpdateVenueRequest(string Name, string City, string Address, string? Description);

public record CreateCourtRequest(string Name, SportType SportType, decimal PricePerHour, TimeOnly OpeningTime, TimeOnly ClosingTime);

public record UpdateCourtRequest(string Name, SportType SportType, decimal PricePerHour, TimeOnly OpeningTime, TimeOnly ClosingTime, bool IsActive);

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
