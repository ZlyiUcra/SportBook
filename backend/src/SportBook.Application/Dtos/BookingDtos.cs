namespace SportBook.Application.Dtos;

/// <summary>
/// The 001 booking fields plus (005) the human-readable venue/city/sport/court labels so a booking
/// is legible without a second lookup (005 data-model.md), plus (006) the venue id. Only display
/// labels and one navigation id are added - no owner id or other internal field (005 spec FR-011).
/// `VenueId` lets a completed booking's review action target the right venue (006 data-model.md) -
/// exposed for the same reason `CourtId` already is (actions/links, not display). `VenueName`/
/// `City`/`Sport`/`CourtName`/`VenueId` require the `Court -> Venue -> City` chain to be loaded
/// before mapping (enforced by the mapping's callers).
/// </summary>
public record BookingResponse(
    Guid Id,
    Guid CourtId,
    Guid UserId,
    DateTime StartTime,
    DateTime EndTime,
    string Status,
    decimal TotalPrice,
    DateTime CreatedAt,
    string VenueName,
    CityResponse City,
    string Sport,
    string CourtName,
    Guid VenueId);
