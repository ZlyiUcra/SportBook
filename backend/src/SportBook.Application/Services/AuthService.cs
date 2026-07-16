using Microsoft.EntityFrameworkCore;
using SportBook.Application.Dtos;
using SportBook.Application.Exceptions;
using SportBook.Application.Security;
using SportBook.Domain.Entities;
using SportBook.Domain.Enums;
using SportBook.Infrastructure;

namespace SportBook.Application.Services;

/// <summary>
/// Registration always creates a <see cref="Role.Customer"/> account - there is no `role` field
/// on <see cref="RegisterRequest"/> (research.md Authorization checklist).
/// </summary>
public class AuthService(SportBookDbContext db, IPasswordHasher passwordHasher, ITokenService tokenService)
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == normalizedEmail, ct))
        {
            throw new ApiException(409, "EMAIL_TAKEN", "Email is already registered.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = normalizedEmail,
            PasswordHash = passwordHasher.Hash(request.Password),
            Role = Role.Customer,
            CreatedAt = DateTime.UtcNow,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == normalizedEmail, ct);
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new ApiException(401, "INVALID_CREDENTIALS", "Invalid email or password.");
        }

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct)
    {
        var tokenHash = tokenService.HashToken(request.RefreshToken);
        var existing = await db.RefreshTokens
            .Include(rt => rt.User)
            .SingleOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);

        if (existing?.User is null || existing.RevokedAt is not null || existing.ExpiresAt < DateTime.UtcNow)
        {
            throw new ApiException(401, "INVALID_REFRESH_TOKEN", "Refresh token is invalid or expired.");
        }

        existing.RevokedAt = DateTime.UtcNow;
        return await IssueTokensAsync(existing.User, ct);
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken ct)
    {
        var tokenHash = tokenService.HashToken(request.RefreshToken);
        var existing = await db.RefreshTokens.SingleOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);
        if (existing is not null)
        {
            existing.RevokedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken ct)
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
