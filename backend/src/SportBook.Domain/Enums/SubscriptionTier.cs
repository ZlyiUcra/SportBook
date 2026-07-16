namespace SportBook.Domain.Enums;

/// <summary>
/// Future monetization placeholder (spec FR-015). Not used for feature gating this iteration -
/// every account defaults to <see cref="Free"/>.
/// </summary>
public enum SubscriptionTier
{
    Free,
    Premium,
}
