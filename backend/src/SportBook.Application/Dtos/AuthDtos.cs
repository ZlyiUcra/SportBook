namespace SportBook.Application.Dtos;

public record RegisterRequest(string Name, string Email, string Password);

public record LoginRequest(string Email, string Password);

public record RefreshRequest(string RefreshToken);

public record LogoutRequest(string RefreshToken);

/// <summary>Explicit response whitelist - never an entity passthrough, so `PasswordHash` can never leak.</summary>
public record UserResponse(Guid Id, string Name, string Email, string Role, string SubscriptionTier, DateTime CreatedAt);

public record AuthResponse(string AccessToken, string RefreshToken, UserResponse User);
