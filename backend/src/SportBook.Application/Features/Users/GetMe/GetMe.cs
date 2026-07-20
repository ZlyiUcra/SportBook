using MediatR;
using Microsoft.EntityFrameworkCore;
using SportBook.Application.Dtos;
using SportBook.Infrastructure;

namespace SportBook.Application.Features.Users.GetMe;

/// <summary>Returns the caller's own profile, resolved from the JWT - never another user's.</summary>
public sealed record GetMeQuery(Guid UserId) : IRequest<UserResponse>;

public sealed class GetMeHandler(SportBookDbContext db) : IRequestHandler<GetMeQuery, UserResponse>
{
    public async Task<UserResponse> Handle(GetMeQuery request, CancellationToken ct)
    {
        var user = await db.Users.SingleAsync(u => u.Id == request.UserId, ct);
        return new UserResponse(user.Id, user.Name, user.Email, user.Role.ToString(), user.SubscriptionTier.ToString(), user.CreatedAt);
    }
}
