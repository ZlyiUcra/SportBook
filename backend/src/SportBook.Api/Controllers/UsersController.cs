using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportBook.Application.Dtos;
using SportBook.Infrastructure;

namespace SportBook.Api.Controllers;

/// <summary>The authenticated caller's own account data.</summary>
[ApiController]
[Route("api/users")]
public class UsersController(SportBookDbContext db) : ControllerBase
{
    /// <summary>Returns the caller's own profile, resolved from the JWT - never another user's.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> GetMe(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        var user = await db.Users.SingleAsync(u => u.Id == userId, ct);

        return Ok(new UserResponse(user.Id, user.Name, user.Email, user.Role.ToString(), user.SubscriptionTier.ToString(), user.CreatedAt));
    }
}
