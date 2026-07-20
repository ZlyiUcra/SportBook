using System.Security.Claims;
using Mediator;
using SportBook.Api.Extensions;
using SportBook.Application.Features.Users.GetMe;

namespace SportBook.Api.Endpoints;

/// <summary>The authenticated caller's own account data.</summary>
public static class UsersEndpoints
{
    /// <summary>Registers the caller's-own-profile endpoint under `api/users/me`.</summary>
    public static void MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/users");

        // <summary>
        // Returns the caller's own profile, resolved from the JWT - never another user's. Now
        // reads identity via the shared `GetUserId()` extension, same as every other slice - the
        // Handler moved the DB lookup off the endpoint, and Handlers cannot see ClaimsPrincipal
        // (consilium 2026-07-20), so the endpoint-extracts-id-then-passes-into-request pattern
        // used everywhere else is no longer optional here either. Functionally identical to the
        // prior inline claim parsing (same NameIdentifier/"sub" claim), not a behavior change.
        // </summary>
        group.MapGet("me", async (ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetMeQuery(user.GetUserId()), ct);
            return Results.Ok(result);
        });
    }
}
