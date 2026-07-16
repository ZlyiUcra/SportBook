namespace SportBook.Domain.Entities;

/// <summary>A rating and comment left by an authenticated user about a Venue (data-model.md Review).</summary>
public class Review
{
    public Guid Id { get; set; }

    public Guid VenueId { get; set; }

    public Venue? Venue { get; set; }

    public Guid UserId { get; set; }

    public User? User { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }
}
