using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportBook.Application.Dtos;
using SportBook.Infrastructure;

namespace SportBook.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController(SportBookDbContext db) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> GetMe(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        var user = await db.Users.SingleAsync(u => u.Id == userId, ct);

        return Ok(new UserResponse(user.Id, user.Name, user.Email, user.Role.ToString(), user.SubscriptionTier.ToString(), user.CreatedAt));
    }
}
