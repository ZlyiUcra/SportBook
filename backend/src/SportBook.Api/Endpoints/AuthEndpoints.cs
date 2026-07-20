using SportBook.Application.Dtos;
using SportBook.Application.Services;

namespace SportBook.Api.Endpoints;

/// <summary>Registration, login, refresh, and logout - the only endpoints reachable without a JWT (FR-014's exceptions).</summary>
public static class AuthEndpoints
{
    /// <summary>Registers the register/login/refresh/logout endpoints under `api/auth`.</summary>
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/auth");

        // <summary>Creates a new account and returns an access/refresh token pair, same as a login.</summary>
        group.MapPost("register", async (RegisterRequest request, AuthService authService, CancellationToken ct) =>
        {
            var result = await authService.RegisterAsync(request, ct);
            return Results.Json(result, statusCode: StatusCodes.Status201Created);
        }).AllowAnonymous();

        // <summary>Exchanges email/password for an access/refresh token pair.</summary>
        group.MapPost("login", async (LoginRequest request, AuthService authService, CancellationToken ct) =>
        {
            var result = await authService.LoginAsync(request, ct);
            return Results.Ok(result);
        }).AllowAnonymous();

        // <summary>Exchanges a still-valid refresh token for a new access/refresh token pair.</summary>
        group.MapPost("refresh", async (RefreshRequest request, AuthService authService, CancellationToken ct) =>
        {
            var result = await authService.RefreshAsync(request, ct);
            return Results.Ok(result);
        }).AllowAnonymous();

        // <summary>
        // Revokes the given refresh token so it can no longer be exchanged. Deliberately NOT
        // AllowAnonymous, unlike its three siblings above - stays behind the global fallback
        // auth policy (consilium 2026-07-20: an accidental AllowAnonymous here would let any
        // caller revoke an arbitrary refresh token).
        // </summary>
        group.MapPost("logout", async (LogoutRequest request, AuthService authService, CancellationToken ct) =>
        {
            await authService.LogoutAsync(request, ct);
            return Results.NoContent();
        });
    }
}
