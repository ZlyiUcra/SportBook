using System.Net;
using System.Net.Http.Json;
using SportBook.Application.Dtos;
using SportBook.IntegrationTests.TestInfrastructure;

namespace SportBook.IntegrationTests;

/// <summary>T020: register, login, and refresh flow (contracts/api.md Auth section).</summary>
[Collection(ApiCollection.Name)]
public class AuthTests(ApiFixture fixture)
{
    [Fact]
    public async Task Register_returns_201_with_tokens_and_customer_role()
    {
        var client = fixture.Factory.CreateClient();
        var email = $"reg-{Guid.NewGuid():N}@example.com";

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("New User", email, "Test1234!"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.NotEmpty(auth.AccessToken);
        Assert.NotEmpty(auth.RefreshToken);
        Assert.Equal("Customer", auth.User.Role);
        Assert.Equal(email.ToLowerInvariant(), auth.User.Email);
    }

    [Fact]
    public async Task Duplicate_email_registration_returns_409()
    {
        var client = fixture.Factory.CreateClient();
        var auth = await client.RegisterAsync();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("Dup", auth.User.Email, "Test1234!"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Login_returns_tokens_for_valid_credentials_and_401_for_wrong_password()
    {
        var client = fixture.Factory.CreateClient();
        var auth = await client.RegisterAsync();

        var ok = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(auth.User.Email, "Test1234!"));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        var wrong = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(auth.User.Email, "WrongPass1!"));
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
    }

    [Fact]
    public async Task Refresh_rotates_the_token_and_rejects_reuse_of_the_old_one()
    {
        var client = fixture.Factory.CreateClient();
        var auth = await client.RegisterAsync();

        var first = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(auth.RefreshToken));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var rotated = await first.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(rotated);
        Assert.NotEqual(auth.RefreshToken, rotated.RefreshToken);

        var reuse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(auth.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
    }

    [Fact]
    public async Task Protected_endpoint_requires_authentication_by_default()
    {
        var client = fixture.Factory.CreateClient();

        var anonymous = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        var auth = await client.RegisterAsync();
        client.UseBearer(auth.AccessToken);
        var authorized = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.OK, authorized.StatusCode);
    }
}
