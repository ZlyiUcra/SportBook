using MediatR;
using SportBook.Application.Features.Cities.FindNearestCity;
using SportBook.Application.Features.Cities.SuggestCities;

namespace SportBook.Api.Endpoints;

/// <summary>City directory reads: suggestion search (US1) and nearest-city resolution (US3).</summary>
public static class CitiesEndpoints
{
    /// <summary>Registers the city suggestion and nearest-city endpoints under `api/cities`.</summary>
    public static void MapCitiesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/cities");

        // <summary>Suggests up to 10 directory cities matching `query` (min 2 chars) in any app language, ranked by population.</summary>
        group.MapGet("", async (string query, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new SuggestCitiesQuery(query), ct);
            return Results.Ok(result);
        });

        // <summary>
        // Resolves the nearest directory city to a device position. Contract MUSTs (research.md
        // Geolocation privacy posture): `lat`/`lng` are range-validated and never persisted or
        // logged - this action must not gain request logging without excluding its query string.
        // </summary>
        group.MapGet("nearest", async (decimal lat, decimal lng, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new FindNearestCityQuery(lat, lng), ct);
            return Results.Ok(result);
        });
    }
}
