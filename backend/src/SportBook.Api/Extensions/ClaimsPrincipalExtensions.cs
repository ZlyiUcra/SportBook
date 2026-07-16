using System.Security.Claims;

namespace SportBook.Api.Extensions;

/// <summary>
/// Identity always comes from JWT claims, never from request bodies (research.md Authorization
/// checklist) - this is the single place controllers read the caller's id from.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub")
            ?? throw new InvalidOperationException("Authenticated principal has no user id claim.");
        return Guid.Parse(value);
    }
}
