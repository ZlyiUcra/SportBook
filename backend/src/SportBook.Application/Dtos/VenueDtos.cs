using SportBook.Domain.Enums;

namespace SportBook.Application.Dtos;

/// <summary>
/// No `ownerId` field - the owner is always the authenticated caller (research.md Authorization
/// checklist). `Latitude`/`Longitude` are both-or-neither (contracts/api.md Venues section) -
/// enforced in VenueService, not by the record shape, since "both or neither" is not expressible
/// as a type constraint here without over-complicating the DTO.
/// </summary>
public record CreateVenueRequest(string Name, int CityId, string Address, string? Description, decimal? Latitude = null, decimal? Longitude = null);

public record UpdateVenueRequest(string Name, int CityId, string Address, string? Description, decimal? Latitude = null, decimal? Longitude = null);

public record CreateCourtRequest(string Name, SportType SportType, decimal PricePerHour, TimeOnly OpeningTime, TimeOnly ClosingTime);

public record UpdateCourtRequest(string Name, SportType SportType, decimal PricePerHour, TimeOnly OpeningTime, TimeOnly ClosingTime, bool IsActive);

/// <summary>
/// List-item shape for venue search; detail data (courts, rating) stays in
/// <see cref="VenueDetailResponse"/>. `Latitude`/`Longitude` are null when the owner has not set
/// a pin - consumers must not substitute city coordinates (spec FR-009/FR-010).
/// </summary>
public record VenueSummaryResponse(Guid Id, string Name, CityResponse City, string Address, string? Description, decimal? Latitude, decimal? Longitude);

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
