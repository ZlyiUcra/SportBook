using Mediator;
using Microsoft.EntityFrameworkCore;
using SportBook.Application.Dtos;
using SportBook.Application.Exceptions;
using SportBook.Application.Security;
using SportBook.Application.Services;
using SportBook.Domain.Entities;
using SportBook.Domain.Enums;
using SportBook.Infrastructure;

namespace SportBook.Application.Features.Auth.Register;

/// <summary>Creates a new account and returns an access/refresh token pair, same as a login.</summary>
public sealed record RegisterCommand(string Name, string Email, string Password) : IRequest<AuthResponse>;

/// <summary>
/// Registration always creates a <see cref="Role.Customer"/> account - there is no `role` field
/// on <see cref="RegisterCommand"/> (research.md Authorization checklist).
/// </summary>
public sealed class RegisterHandler(SportBookDbContext db, IPasswordHasher passwordHasher, AuthTokenIssuer tokenIssuer)
    : IRequestHandler<RegisterCommand, AuthResponse>
{
    public async ValueTask<AuthResponse> Handle(RegisterCommand request, CancellationToken ct)
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

        return await tokenIssuer.IssueTokensAsync(user, ct);
    }
}
