using Mediator;
using Microsoft.EntityFrameworkCore;
using SportBook.Application.Dtos;
using SportBook.Application.Exceptions;
using SportBook.Application.Security;
using SportBook.Application.Services;
using SportBook.Infrastructure;

namespace SportBook.Application.Features.Auth.Login;

/// <summary>Exchanges email/password for an access/refresh token pair.</summary>
public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;

public sealed class LoginHandler(SportBookDbContext db, IPasswordHasher passwordHasher, AuthTokenIssuer tokenIssuer)
    : IRequestHandler<LoginCommand, AuthResponse>
{
    public async ValueTask<AuthResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == normalizedEmail, ct);
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new ApiException(401, "INVALID_CREDENTIALS", "Invalid email or password.");
        }

        return await tokenIssuer.IssueTokensAsync(user, ct);
    }
}
