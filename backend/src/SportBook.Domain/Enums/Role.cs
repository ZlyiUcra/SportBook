namespace SportBook.Domain.Enums;

/// <summary>
/// Account role. Registration always creates <see cref="Customer"/> - elevation to
/// <see cref="VenueOwner"/> or <see cref="Admin"/> is an out-of-scope admin process for this
/// iteration (research.md Authorization checklist).
/// </summary>
public enum Role
{
    Customer,
    VenueOwner,
    Admin,
}
