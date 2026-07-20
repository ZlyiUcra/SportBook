using Mediator;
using Microsoft.EntityFrameworkCore;
using SportBook.Application.Dtos;
using SportBook.Application.Exceptions;
using SportBook.Application.Security;
using SportBook.Application.Services;
using SportBook.Infrastructure;

namespace SportBook.Application.Features.Auth.Refresh;

/// <summary>Exchanges a still-valid refresh token for a new access/refresh token pair.</summary>
public sealed record RefreshCommand(string RefreshToken) : IRequest<AuthResponse>;

public sealed class RefreshHandler(SportBookDbContext db, ITokenService tokenService, AuthTokenIssuer tokenIssuer)
    : IRequestHandler<RefreshCommand, AuthResponse>
{
    public async ValueTask<AuthResponse> Handle(RefreshCommand request, CancellationToken ct)
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
        return await tokenIssuer.IssueTokensAsync(existing.User, ct);
    }
}
