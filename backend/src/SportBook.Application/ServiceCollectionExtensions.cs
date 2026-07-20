using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SportBook.Application.Security;
using SportBook.Application.Services;

namespace SportBook.Application;

/// <summary>Registers Application-layer services (security, business services) in one place.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSportBookApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddScoped<AuthTokenIssuer>();
        services.AddSingleton<CityDirectoryCache>();
        services.AddScoped<CityService>();
        services.AddScoped<VenueDetailReader>();
        services.AddScoped<VenueLocationValidator>();

        return services;
    }
}
