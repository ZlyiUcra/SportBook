namespace SportBook.Application.Dtos;

/// <summary>
/// No `userId` or `totalPrice` fields by design - both are derived server-side from the JWT and
/// `Court.PricePerHour` (contracts/api.md, consilium security finding on mass assignment).
/// </summary>
public record CreateBookingRequest(Guid CourtId, DateTime StartTime, DateTime EndTime);

public record BookingResponse(
    Guid Id,
    Guid CourtId,
    Guid UserId,
    DateTime StartTime,
    DateTime EndTime,
    string Status,
    decimal TotalPrice,
    DateTime CreatedAt);

public record FreeSlot(DateTime Start, DateTime End);

public record AvailabilityResponse(Guid CourtId, DateOnly Date, IReadOnlyList<FreeSlot> FreeSlots);
