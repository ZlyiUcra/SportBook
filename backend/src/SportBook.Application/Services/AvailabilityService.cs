using Microsoft.EntityFrameworkCore;
using SportBook.Application.Dtos;
using SportBook.Application.Exceptions;
using SportBook.Domain.Enums;
using SportBook.Infrastructure;

namespace SportBook.Application.Services;

/// <summary>
/// Computes free whole-hour slots on the fly from operating hours minus Pending/Confirmed
/// bookings (research.md availability decision - no materialized slot table).
/// </summary>
public class AvailabilityService(SportBookDbContext db)
{
    public async Task<AvailabilityResponse> GetForDateAsync(Guid courtId, DateOnly date, CancellationToken ct)
    {
        var court = await db.Courts.AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == courtId && c.IsActive, ct)
            ?? throw new ApiException(404, "COURT_NOT_FOUND", "Court not found.");

        var dayStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEnd = dayStart.AddDays(1);

        var bookings = await db.Bookings.AsNoTracking()
            .Where(b => b.CourtId == courtId
                && b.Status != BookingStatus.Cancelled
                && b.StartTime < dayEnd && b.EndTime > dayStart)
            .Select(b => new { b.StartTime, b.EndTime })
            .ToListAsync(ct);

        var freeSlots = new List<FreeSlot>();
        for (var slotStart = date.ToDateTime(court.OpeningTime, DateTimeKind.Utc);
             slotStart.AddHours(1) <= date.ToDateTime(court.ClosingTime, DateTimeKind.Utc);
             slotStart = slotStart.AddHours(1))
        {
            var slotEnd = slotStart.AddHours(1);
            var taken = bookings.Any(b => b.StartTime < slotEnd && b.EndTime > slotStart);
            if (!taken)
            {
                freeSlots.Add(new FreeSlot(slotStart, slotEnd));
            }
        }

        return new AvailabilityResponse(courtId, date, freeSlots);
    }
}
