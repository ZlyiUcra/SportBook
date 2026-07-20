using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SportBook.Application.Dtos;
using SportBook.Infrastructure;

namespace SportBook.Api.Endpoints;

/// <summary>The authenticated caller's own account data.</summary>
public static class UsersEndpoints
{
    /// <summary>Registers the caller's-own-profile endpoint under `api/users/me`.</summary>
    public static void MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/users");

        // <summary>
        // Returns the caller's own profile, resolved from the JWT - never another user's. Kept
        // as inline claim parsing (not `ClaimsPrincipalExtensions.GetUserId()`) to preserve this
        // endpoint's exact pre-existing behavior across the Minimal API conversion - not a
        // drive-by cleanup (consilium 2026-07-20).
        // </summary>
        group.MapGet("me", async (ClaimsPrincipal user, SportBookDbContext db, CancellationToken ct) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")!);
            var dbUser = await db.Users.SingleAsync(u => u.Id == userId, ct);

            return Results.Ok(new UserResponse(dbUser.Id, dbUser.Name, dbUser.Email, dbUser.Role.ToString(), dbUser.SubscriptionTier.ToString(), dbUser.CreatedAt));
        });
    }
}
