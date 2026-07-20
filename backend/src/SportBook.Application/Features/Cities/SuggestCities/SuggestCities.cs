using Mediator;
using SportBook.Application.Dtos;
using SportBook.Application.Exceptions;
using SportBook.Application.Services;
using SportBook.Domain.Entities;

namespace SportBook.Application.Features.Cities.SuggestCities;

/// <summary>Suggests up to 10 directory cities matching `Query` (min 2 chars) in any app language, ranked by population.</summary>
public sealed record SuggestCitiesQuery(string Query) : IRequest<IReadOnlyList<CityResponse>>;

public sealed class SuggestCitiesHandler(CityDirectoryCache cache) : IRequestHandler<SuggestCitiesQuery, IReadOnlyList<CityResponse>>
{
    /// <summary>Min 2 chars (else 400); matches any localized name column; TOP 10 ordered by population DESC (contracts/api.md Cities section).</summary>
    public async ValueTask<IReadOnlyList<CityResponse>> Handle(SuggestCitiesQuery request, CancellationToken ct)
    {
        if (request.Query.Length < 2)
        {
            throw new ApiException(400, "QUERY_TOO_SHORT", "query must be at least 2 characters.");
        }

        var cities = await cache.GetAllAsync(ct);
        return Rank(cities, request.Query);
    }

    /// <summary>Case-insensitive substring match against all three localized name columns - typing in any app language finds the city.</summary>
    private static bool MatchesQuery(City city, string query) =>
        city.NameEn.Contains(query, StringComparison.OrdinalIgnoreCase) ||
        city.NameUk.Contains(query, StringComparison.OrdinalIgnoreCase) ||
        city.NamePt.Contains(query, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Pure ranking step of <see cref="Handle"/>, split out so suggestion ranking and localized-name
    /// matching are unit-testable without a database (T014).
    /// </summary>
    public static IReadOnlyList<CityResponse> Rank(IEnumerable<City> cities, string query, int limit = 10) =>
        cities
            .Where(c => MatchesQuery(c, query))
            .OrderByDescending(c => c.Population)
            .Take(limit)
            .Select(c => c.ToResponse())
            .ToList();
}
