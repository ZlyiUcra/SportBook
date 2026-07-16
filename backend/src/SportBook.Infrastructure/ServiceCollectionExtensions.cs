using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SportBook.Infrastructure;

/// <summary>
/// Single entry point for registering data-access infrastructure. Kept as one extension method
/// so test hosts (Sqlite in-memory, WebApplicationFactory) can swap the provider registration
/// without touching Api/Application/Domain (plan.md Storage constraint).
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSportBookInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<SportBookDbContext>(options =>
            options.UseSqlServer(connectionString));

        return services;
    }
}
