using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SportBook.Infrastructure;

namespace SportBook.IntegrationTests.TestInfrastructure;

/// <summary>
/// Boots the real app against the docker-compose SQL Server with a dedicated `SportBookDb_Tests`
/// database (dropped and re-migrated once per run). Uses the compose SA credentials by default -
/// tests need create/drop-database rights and must work on any freshly started container;
/// override via the SPORTBOOK_TEST_CONNECTION environment variable if needed. The overlap
/// concurrency behavior (FR-004) is intentionally validated on the real engine, not Sqlite
/// (consilium 2026-07-16 decision).
/// </summary>
public class SportBookApiFactory : WebApplicationFactory<Program>
{
    private const string DefaultTestConnection =
        "Server=127.0.0.1,14330;Database=SportBookDb_Tests;User Id=sa;Password=SportBook_Dev_Passw0rd;TrustServerCertificate=True;Encrypt=True";

    public static string TestConnectionString =>
        Environment.GetEnvironmentVariable("SPORTBOOK_TEST_CONNECTION") ?? DefaultTestConnection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // UseSetting (not ConfigureAppConfiguration): with minimal hosting, Program.cs reads
        // Jwt config eagerly at startup, and only settings injected this way are visible at
        // that point - late-appended config sources would only reach lazy IOptions consumers,
        // leaving the bearer validator and the token signer with different keys.
        builder.UseSetting("ConnectionStrings:DefaultConnection", TestConnectionString);
        builder.UseSetting("Jwt:Key", "sportbook-integration-tests-signing-key-0123456789abcdef");
        builder.UseSetting("Jwt:Issuer", "SportBook");
        builder.UseSetting("Jwt:Audience", "SportBook");
    }

    /// <summary>Fresh schema once per test run; individual tests isolate by creating their own rows.</summary>
    public void ResetDatabase()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SportBookDbContext>();
        db.Database.EnsureDeleted();
        db.Database.Migrate();
    }

    /// <summary>Runs seed logic in a fresh DI scope against the test database.</summary>
    public async Task SeedAsync(Func<SportBookDbContext, Task> seed)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SportBookDbContext>();
        await seed(db);
    }
}

/// <summary>
/// Single shared factory + database for all integration test classes - xUnit runs classes in one
/// collection sequentially, which keeps EnsureDeleted/Migrate free of cross-class races.
/// </summary>
[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "api";
}

public sealed class ApiFixture : IDisposable
{
    public SportBookApiFactory Factory { get; } = new();

    public ApiFixture()
    {
        Factory.ResetDatabase();
    }

    public void Dispose() => Factory.Dispose();
}
