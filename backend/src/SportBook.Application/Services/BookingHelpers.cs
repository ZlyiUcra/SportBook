using Microsoft.EntityFrameworkCore;
using SportBook.Application.Dtos;
using SportBook.Application.Exceptions;
using SportBook.Domain.Entities;
using SportBook.Domain.Enums;

namespace SportBook.Application.Services;

/// <summary>
/// Pure/stateless helpers shared across the Bookings feature slices (CreateBooking, CancelBooking,
/// GetBookingById, ListMyBookings, ListVenueBookingsForOwner, ConfirmBooking) - static, not an
/// injected collaborator, since none of them carry state or a dependency of their own (consilium
/// 2026-07-20: matches the original private-static shape, just made accessible across handler
/// classes instead of one service class).
/// </summary>
public static class BookingHelpers
{
    /// <summary>
    /// The `Court -> Venue -> City` chain every booking-response path must load so
    /// <see cref="Dtos.Mapping.ToResponse(Booking, DateTime)"/> can fill the venue/city/sport/court
    /// labels (005 data-model.md). Kept in one place so no response path forgets a level.
    /// </summary>
    public static IQueryable<Booking> IncludeDetail(IQueryable<Booking> query) =>
        query.Include(b => b.Court!).ThenInclude(c => c.Venue!).ThenInclude(v => v.City);

    /// <summary>
    /// Maps a <see cref="BookingStatusFilter"/> to a translatable predicate applied BEFORE paging
    /// (005 data-model.md), so the filter acts on the whole history, not a materialized page.
    /// `Completed` is encoded as Confirmed + past end time (never a stored status); a stale
    /// pending-past booking matches none of Upcoming/Completed/Cancelled and so only appears under
    /// `All`.
    /// </summary>
    public static IQueryable<Booking> ApplyStatusFilter(IQueryable<Booking> query, BookingStatusFilter status, DateTime now) =>
        status switch
        {
            BookingStatusFilter.Upcoming => query.Where(b => b.Status != BookingStatus.Cancelled && b.EndTime > now),
            BookingStatusFilter.Completed => query.Where(b => b.Status == BookingStatus.Confirmed && b.EndTime <= now),
            BookingStatusFilter.Cancelled => query.Where(b => b.Status == BookingStatus.Cancelled),
            _ => query,
        };

    /// <summary>Whole-hour, in the future, inside operating hours, same calendar day (data-model.md Booking rules).</summary>
    public static void ValidateSlot(Court court, DateTime start, DateTime end, DateTime now)
    {
        if (start.Minute != 0 || start.Second != 0 || start.Millisecond != 0
            || end.Minute != 0 || end.Second != 0 || end.Millisecond != 0)
        {
            throw new ApiException(400, "NOT_WHOLE_HOUR", "Bookings must start and end on whole-hour boundaries.");
        }

        if (end <= start)
        {
            throw new ApiException(400, "INVALID_RANGE", "End time must be after start time.");
        }

        if (start < now)
        {
            throw new ApiException(400, "START_IN_PAST", "Booking start time is in the past.");
        }

        if (end.AddTicks(-1).Date != start.Date)
        {
            throw new ApiException(400, "CROSSES_MIDNIGHT", "A booking must stay within one calendar day.");
        }

        var startTime = TimeOnly.FromDateTime(start);
        var endTime = TimeOnly.FromDateTime(end);
        var endsAtMidnight = endTime == TimeOnly.MinValue;
        if (startTime < court.OpeningTime || endsAtMidnight || endTime > court.ClosingTime)
        {
            throw new ApiException(409, "OUTSIDE_OPERATING_HOURS", "The requested time is outside the court's operating hours.");
        }
    }

    /// <summary>Timestamps are UTC by convention (plan.md); Unspecified is treated as UTC, Local converted.</summary>
    public static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };

    /// <summary>SQL Server deadlock victim (error 1205), surfaced through EF as DbUpdateException or directly.</summary>
    public static bool IsDeadlock(Exception ex)
    {
        var current = ex is DbUpdateException { InnerException: not null } dbEx ? dbEx.InnerException : ex;
        return current is Microsoft.Data.SqlClient.SqlException { Number: 1205 };
    }
}
