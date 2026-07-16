namespace SportBook.Application.Security;

/// <summary>Hashes and verifies user passwords. Never stores or logs a plaintext password.</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}
