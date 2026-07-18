using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SportBook.Domain.Entities;
using SportBook.Infrastructure;

namespace SportBook.Application.Services;

/// <summary>
/// Process-lifetime cache of the City reference directory (research.md "Nearby-cities
/// computation shape": ~3-6k rows, changes only via migration, safe to cache indefinitely).
/// Registered as a singleton so the cache and the neighbor-set memo survive across requests;
/// it creates its own <see cref="SportBookDbContext"/> scope on first use rather than taking a
/// scoped DbContext as a constructor dependency, which would be a captive-dependency bug.
/// </summary>
public class CityDirectoryCache(IServiceScopeFactory scopeFactory)
{
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private readonly ConcurrentDictionary<int, IReadOnlyList<int>> _neighborCache = new();
    private IReadOnlyList<City>? _cities;

    public async Task<IReadOnlyList<City>> GetAllAsync(CancellationToken ct)
    {
        if (_cities is not null)
        {
            return _cities;
        }

        await _loadLock.WaitAsync(ct);
        try
        {
            if (_cities is not null)
            {
                return _cities;
            }

            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SportBookDbContext>();
            _cities = await db.Cities.AsNoTracking().ToListAsync(ct);
            return _cities;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    /// <summary>Neighbor-ID sets are pure functions of the (immutable) directory, so they memoize per city for the process lifetime.</summary>
    public IReadOnlyList<int> GetOrAddNeighborIds(int cityId, Func<int, IReadOnlyList<int>> compute) =>
        _neighborCache.GetOrAdd(cityId, compute);
}
