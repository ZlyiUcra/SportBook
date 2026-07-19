namespace SportBook.Application.Dtos;

/// <summary>
/// Status groups a customer can filter "My bookings" by (005 data-model.md). `Completed` is not a
/// stored status - it is a Confirmed booking whose end time has passed (001 read-time derivation) -
/// so each value maps to a stored-status + time predicate in
/// <see cref="Services.BookingService.ListMineAsync"/>, applied before paging so it filters the
/// whole history. `All` (the default) applies no predicate.
/// </summary>
public enum BookingStatusFilter
{
    All,
    Upcoming,
    Completed,
    Cancelled,
}

/// <summary>
/// No `userId` or `totalPrice` fields by design - both are derived server-side from the JWT and
/// `Court.PricePerHour` (contracts/api.md, consilium security finding on mass assignment).
/// </summary>
public record CreateBookingRequest(Guid CourtId, DateTime StartTime, DateTime EndTime);

/// <summary>
/// The 001 booking fields plus (005) the human-readable venue/city/sport/court labels so a booking
/// is legible without a second lookup (005 data-model.md). Only display labels are added - no owner
/// id or other internal field (005 spec FR-011). `VenueName`/`City`/`Sport`/`CourtName` require the
/// `Court -> Venue -> City` chain to be loaded before mapping (enforced by the mapping's callers).
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
    string CourtName);

public record FreeSlot(DateTime Start, DateTime End);

public record AvailabilityResponse(Guid CourtId, DateOnly Date, IReadOnlyList<FreeSlot> FreeSlots);
