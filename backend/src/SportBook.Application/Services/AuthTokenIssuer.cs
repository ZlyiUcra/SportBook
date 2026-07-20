using SportBook.Application.Dtos;
using SportBook.Application.Security;
using SportBook.Domain.Entities;
using SportBook.Infrastructure;

namespace SportBook.Application.Services;

/// <summary>
/// Shared access/refresh token issuance, used by the Register, Login, and Refresh handlers
/// (Features/Auth) - a plain injected collaborator, not a Command/Query, since it doesn't
/// correspond to an endpoint of its own (consilium 2026-07-20).
/// </summary>
public class AuthTokenIssuer(SportBookDbContext db, ITokenService tokenService)
{
    public async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken ct)
    {
        var accessToken = tokenService.GenerateAccessToken(user);
        var (rawRefreshToken, refreshTokenHash) = tokenService.GenerateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAt = tokenService.GetRefreshTokenExpiry(),
            CreatedAt = DateTime.UtcNow,
        });

        await db.SaveChangesAsync(ct);

        return new AuthResponse(
            accessToken,
            rawRefreshToken,
            new UserResponse(user.Id, user.Name, user.Email, user.Role.ToString(), user.SubscriptionTier.ToString(), user.CreatedAt));
    }
}
