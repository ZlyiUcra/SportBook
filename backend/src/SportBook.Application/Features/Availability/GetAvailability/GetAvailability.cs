using Mediator;
using Microsoft.EntityFrameworkCore;
using SportBook.Application.Dtos;
using SportBook.Application.Exceptions;
using SportBook.Domain.Enums;
using SportBook.Infrastructure;

namespace SportBook.Application.Features.Availability.GetAvailability;

/// <summary>Free whole-hour slots for a court on a given date, within its operating hours (US1).</summary>
public sealed record GetAvailabilityQuery(Guid CourtId, DateOnly Date) : IRequest<AvailabilityResponse>;

/// <summary>
/// Computes free whole-hour slots on the fly from operating hours minus Pending/Confirmed
/// bookings (research.md availability decision - no materialized slot table).
/// </summary>
public sealed class GetAvailabilityHandler(SportBookDbContext db) : IRequestHandler<GetAvailabilityQuery, AvailabilityResponse>
{
    public async ValueTask<AvailabilityResponse> Handle(GetAvailabilityQuery request, CancellationToken ct)
    {
        var court = await db.Courts.AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == request.CourtId && c.IsActive, ct)
            ?? throw new ApiException(404, "COURT_NOT_FOUND", "Court not found.");

        var dayStart = request.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEnd = dayStart.AddDays(1);

        var bookings = await db.Bookings.AsNoTracking()
            .Where(b => b.CourtId == request.CourtId
                && b.Status != BookingStatus.Cancelled
                && b.StartTime < dayEnd && b.EndTime > dayStart)
            .Select(b => new { b.StartTime, b.EndTime })
            .ToListAsync(ct);

        var freeSlots = new List<FreeSlot>();
        for (var slotStart = request.Date.ToDateTime(court.OpeningTime, DateTimeKind.Utc);
             slotStart.AddHours(1) <= request.Date.ToDateTime(court.ClosingTime, DateTimeKind.Utc);
             slotStart = slotStart.AddHours(1))
        {
            var slotEnd = slotStart.AddHours(1);
            var taken = bookings.Any(b => b.StartTime < slotEnd && b.EndTime > slotStart);
            if (!taken)
            {
                freeSlots.Add(new FreeSlot(slotStart, slotEnd));
            }
        }

        return new AvailabilityResponse(request.CourtId, request.Date, freeSlots);
    }
}
