using MediatR;
using Microsoft.EntityFrameworkCore;
using SportBook.Application.Security;
using SportBook.Infrastructure;

namespace SportBook.Application.Features.Auth.Logout;

/// <summary>Revokes the given refresh token so it can no longer be exchanged.</summary>
public sealed record LogoutCommand(string RefreshToken) : IRequest;

public sealed class LogoutHandler(SportBookDbContext db, ITokenService tokenService) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken ct)
    {
        var tokenHash = tokenService.HashToken(request.RefreshToken);
        var existing = await db.RefreshTokens.SingleOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);
        if (existing is not null)
        {
            existing.RevokedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }
}
