namespace SportBook.Domain.Enums;

/// <summary>
/// Booking lifecycle state (data-model.md Booking). <see cref="Completed"/> is never persisted -
/// it is derived on read from a Confirmed booking whose EndTime has passed.
/// </summary>
public enum BookingStatus
{
    Pending,
    Confirmed,
    Cancelled,
    Completed,
}
