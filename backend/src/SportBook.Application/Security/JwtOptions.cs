namespace SportBook.Application.Security;

/// <summary>Bound from the `Jwt` configuration section. <see cref="Key"/> is a secret - never hardcoded.</summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public required string Key { get; set; }

    public required string Issuer { get; set; }

    public required string Audience { get; set; }

    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 30;
}
