using System.Security.Cryptography;

namespace SportBook.Application.Security;

/// <summary>
/// PBKDF2-SHA256 password hashing via the .NET built-in <see cref="Rfc2898DeriveBytes"/> - no
/// extra NuGet dependency for a hashing algorithm (CLAUDE.md dependency sign-off rule). Format
/// is self-describing (`iterations.salt.hash`, both base64) so the iteration count can be raised
/// later without breaking existing hashes.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int Iterations = 210_000;
    private const int SaltSizeBytes = 16;
    private const int KeySizeBytes = 32;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySizeBytes);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }

    public bool Verify(string password, string hash)
    {
        var parts = hash.Split('.', 3);
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[1]);
        var expectedKey = Convert.FromBase64String(parts[2]);
        var actualKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedKey.Length);
        return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
    }
}
