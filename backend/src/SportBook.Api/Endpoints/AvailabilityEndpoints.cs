using SportBook.Application.Dtos;
using SportBook.Application.Services;

namespace SportBook.Api.Endpoints;

/// <summary>Free-slot lookup for a single court and day, used by the booking form's time picker.</summary>
public static class AvailabilityEndpoints
{
    /// <summary>Registers the court availability endpoint under `api/courts/{id}/availability`.</summary>
    public static void MapAvailabilityEndpoints(this IEndpointRouteBuilder app)
    {
        // <summary>Whole-hour free slots for a court on a given date, within its operating hours.</summary>
        app.MapGet("api/courts/{id:guid}/availability", async (
            Guid id, DateOnly date, AvailabilityService availabilityService, CancellationToken ct) =>
        {
            var result = await availabilityService.GetForDateAsync(id, date, ct);
            return Results.Ok(result);
        });
    }
}
