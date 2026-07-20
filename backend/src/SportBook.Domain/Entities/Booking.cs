using SportBook.Domain.Enums;

namespace SportBook.Domain.Entities;

/// <summary>
/// A reservation of a Court for a time range by a Customer (data-model.md Booking). Overlap
/// safety for [StartTime, EndTime) per court is enforced in <c>CreateBookingHandler</c> via a
/// serializable transaction with retry, not by a database constraint - SQL Server has no
/// exclusion constraints (plan.md Storage).
/// </summary>
public class Booking
{
    public Guid Id { get; set; }

    public Guid CourtId { get; set; }

    public Court? Court { get; set; }

    public Guid UserId { get; set; }

    public User? User { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public decimal TotalPrice { get; set; }

    public DateTime CreatedAt { get; set; }
}
