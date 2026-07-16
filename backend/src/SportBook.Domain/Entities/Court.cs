using SportBook.Domain.Enums;

namespace SportBook.Domain.Entities;

/// <summary>A single bookable playing surface within a Venue (data-model.md Court).</summary>
public class Court
{
    public Guid Id { get; set; }

    public Guid VenueId { get; set; }

    public Venue? Venue { get; set; }

    public required string Name { get; set; }

    public SportType SportType { get; set; }

    public decimal PricePerHour { get; set; }

    public TimeOnly OpeningTime { get; set; }

    public TimeOnly ClosingTime { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public List<Booking> Bookings { get; set; } = [];
}
