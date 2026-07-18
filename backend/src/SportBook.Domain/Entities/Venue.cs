namespace SportBook.Domain.Entities;

/// <summary>A sports facility listed by a venue owner (data-model.md Venue).</summary>
public class Venue
{
    public Guid Id { get; set; }

    public Guid OwnerId { get; set; }

    public User? Owner { get; set; }

    public required string Name { get; set; }

    public int CityId { get; set; }

    public City? City { get; set; }

    public required string Address { get; set; }

    public string? Description { get; set; }

    /// <summary>Both-or-neither with <see cref="Longitude"/> (enforced in VenueService) - set only via the owner's map pin; no coordinate is ever derived from the city.</summary>
    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<Court> Courts { get; set; } = [];

    public List<Review> Reviews { get; set; } = [];
}
