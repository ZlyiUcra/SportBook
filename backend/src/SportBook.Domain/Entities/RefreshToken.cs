namespace SportBook.Domain.Entities;

/// <summary>
/// Supports `/auth/refresh` and `/auth/logout` (research.md). Stores a hash of the token value,
/// never the raw token.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User? User { get; set; }

    public required string TokenHash { get; set; }

    public DateTime ExpiresAt { get; set; }

    /// <summary>Null while active; set on logout or rotation.</summary>
    public DateTime? RevokedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
