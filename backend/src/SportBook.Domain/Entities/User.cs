using SportBook.Domain.Enums;

namespace SportBook.Domain.Entities;

/// <summary>A registered account (data-model.md User).</summary>
public class User
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Email { get; set; }

    /// <summary>Never exposed in any response DTO.</summary>
    public required string PasswordHash { get; set; }

    public Role Role { get; set; } = Role.Customer;

    public SubscriptionTier SubscriptionTier { get; set; } = SubscriptionTier.Free;

    public DateTime CreatedAt { get; set; }
}
