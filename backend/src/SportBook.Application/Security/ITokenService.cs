using SportBook.Domain.Entities;

namespace SportBook.Application.Security;

/// <summary>Issues access tokens (JWT) and opaque refresh tokens.</summary>
public interface ITokenService
{
    /// <summary>Signed JWT carrying `sub` (user id) and `role` claims.</summary>
    string GenerateAccessToken(User user);

    DateTime GetAccessTokenExpiry();

    /// <summary>A random opaque value and its hash - only the hash is ever persisted (data-model.md RefreshToken).</summary>
    (string RawToken, string TokenHash) GenerateRefreshToken();

    DateTime GetRefreshTokenExpiry();

    string HashToken(string rawToken);
}
