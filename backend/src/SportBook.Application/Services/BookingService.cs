using System.Data;
using Microsoft.EntityFrameworkCore;
using SportBook.Application.Authorization;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Application.Exceptions;
using SportBook.Domain.Entities;
using SportBook.Domain.Enums;
using SportBook.Infrastructure;

namespace SportBook.Application.Services;

/// <summary>
/// Booking lifecycle for US1: create (overlap-safe), cancel (2h cutoff), reads. Overlap safety
/// (spec FR-004) uses a serializable transaction with deadlock retry around check-then-insert -
/// the consilium-agreed default for SQL Server, which has no exclusion constraints. TotalPrice is
/// always server-computed (FR-003); the request DTO cannot even carry a price.
/// </summary>
public class BookingService(SportBookDbContext db, TimeProvider timeProvider)
{
    private const int CancellationCutoffHours = 2;
    private const int MaxCreateAttempts = 3;

    /// <summary>
    /// The `Court -> Venue -> City` chain every booking-response path must load so
    /// <see cref="Dtos.Mapping.ToResponse(Booking, DateTime)"/> can fill the venue/city/sport/court
    /// labels (005 data-model.md). Kept in one place so no response path forgets a level.
    /// </summary>
    private static IQueryable<Booking> IncludeDetail(IQueryable<Booking> query) =>
        query.Include(b => b.Court!).ThenInclude(c => c.Venue!).ThenInclude(v => v.City);

    public async Task<BookingResponse> CreateAsync(Guid userId, CreateBookingRequest request, CancellationToken ct)
    {
        var start = AsUtc(request.StartTime);
        var end = AsUtc(request.EndTime);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var court = await db.Courts.AsNoTracking()
            .Include(c => c.Venue!).ThenInclude(v => v.City)
            .SingleOrDefaultAsync(c => c.Id == request.CourtId && c.IsActive, ct)
            ?? throw new ApiException(404, "COURT_NOT_FOUND", "Court not found.");

        ValidateSlot(court, start, end, now);

        var hours = (int)(end - start).TotalHours;
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            CourtId = court.Id,
            UserId = userId,
            StartTime = start,
            EndTime = end,
            Status = BookingStatus.Pending,
            TotalPrice = court.PricePerHour * hours,
            CreatedAt = now,
        };

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

                var overlapExists = await db.Bookings.AnyAsync(b =>
                    b.CourtId == court.Id
                    && b.Status != BookingStatus.Cancelled
                    && b.StartTime < end && b.EndTime > start, ct);

                if (overlapExists)
                {
                    throw new ApiException(409, "SLOT_TAKEN", "The requested time slot is no longer available.");
                }

                db.Bookings.Add(booking);
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                // Attach the already-loaded court graph for mapping only (after save, so EF never
                // tries to insert the detached court); the response needs venue/city/sport/court.
                booking.Court = court;
                return booking.ToResponse(now);
            }
            catch (Exception ex) when (IsDeadlock(ex) && attempt < MaxCreateAttempts)
            {
                // Serializable range locks make concurrent check-then-insert deadlock-prone by
                // design; the losing transaction retries and then observes the winner's row.
                db.Entry(booking).State = EntityState.Detached;
            }
        }
    }

    public async Task<BookingResponse> CancelAsync(Guid userId, Guid bookingId, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var booking = await IncludeDetail(db.Bookings).SingleOrDefaultAsync(b => b.Id == bookingId, ct)
            ?? throw new ApiException(404, "BOOKING_NOT_FOUND", "Booking not found.");

        OwnershipChecks.EnsureBookingCustomer(booking, userId);

        if (booking.Status == BookingStatus.Cancelled)
        {
            throw new ApiException(409, "ALREADY_CANCELLED", "Booking is already cancelled.");
        }

        if (now > booking.StartTime.AddHours(-CancellationCutoffHours))
        {
            throw new ApiException(409, "CANCEL_CUTOFF",
                $"Bookings can only be cancelled more than {CancellationCutoffHours} hours before start.");
        }

        booking.Status = BookingStatus.Cancelled;
        await db.SaveChangesAsync(ct);
        return booking.ToResponse(now);
    }

    public async Task<BookingResponse> GetByIdAsync(Guid userId, Guid bookingId, CancellationToken ct)
    {
        var booking = await IncludeDetail(db.Bookings.AsNoTracking()).SingleOrDefaultAsync(b => b.Id == bookingId, ct)
            ?? throw new ApiException(404, "BOOKING_NOT_FOUND", "Booking not found.");

        OwnershipChecks.EnsureBookingCustomer(booking, userId);
        return booking.ToResponse(timeProvider.GetUtcNow().UtcDateTime);
    }

    public async Task<PagedResponse<BookingResponse>> ListMineAsync(
        Guid userId, BookingStatusFilter status, PageRequest page, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var query = db.Bookings.AsNoTracking().Where(b => b.UserId == userId);
        query = ApplyStatusFilter(query, status, now);
        var totalCount = await query.CountAsync(ct);
        var items = await IncludeDetail(query)
            .OrderByDescending(b => b.StartTime)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(ct);

        return new PagedResponse<BookingResponse>(
            items.Select(b => b.ToResponse(now)).ToList(), page.Page, page.PageSize, totalCount);
    }

    /// <summary>
    /// Maps a <see cref="BookingStatusFilter"/> to a translatable predicate applied BEFORE paging
    /// (005 data-model.md), so the filter acts on the whole history, not a materialized page.
    /// `Completed` is encoded as Confirmed + past end time (never a stored status); a stale
    /// pending-past booking matches none of Upcoming/Completed/Cancelled and so only appears under
    /// `All`.
    /// </summary>
    private static IQueryable<Booking> ApplyStatusFilter(IQueryable<Booking> query, BookingStatusFilter status, DateTime now) =>
        status switch
        {
            BookingStatusFilter.Upcoming => query.Where(b => b.Status != BookingStatus.Cancelled && b.EndTime > now),
            BookingStatusFilter.Completed => query.Where(b => b.Status == BookingStatus.Confirmed && b.EndTime <= now),
            BookingStatusFilter.Cancelled => query.Where(b => b.Status == BookingStatus.Cancelled),
            _ => query,
        };

    /// <summary>Only bookings for the caller's own venue (spec FR-010); venue ownership is checked directly, not per row.</summary>
    public async Task<PagedResponse<BookingResponse>> ListByVenueForOwnerAsync(
        Guid ownerId, Guid venueId, PageRequest page, CancellationToken ct)
    {
        var venue = await db.Venues.AsNoTracking().SingleOrDefaultAsync(v => v.Id == venueId, ct)
            ?? throw new ApiException(404, "VENUE_NOT_FOUND", "Venue not found.");
        OwnershipChecks.EnsureVenueOwner(venue, ownerId);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var query = db.Bookings.AsNoTracking().Where(b => b.Court!.VenueId == venueId);
        var totalCount = await query.CountAsync(ct);
        var items = await IncludeDetail(query)
            .OrderByDescending(b => b.StartTime)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(ct);

        return new PagedResponse<BookingResponse>(
            items.Select(b => b.ToResponse(now)).ToList(), page.Page, page.PageSize, totalCount);
    }

    /// <summary>Pending -> Confirmed only (spec FR-011, data-model.md state transitions); owner via Court.Venue.OwnerId.</summary>
    public async Task<BookingResponse> ConfirmAsync(Guid ownerId, Guid bookingId, CancellationToken ct)
    {
        var booking = await IncludeDetail(db.Bookings)
            .SingleOrDefaultAsync(b => b.Id == bookingId, ct)
            ?? throw new ApiException(404, "BOOKING_NOT_FOUND", "Booking not found.");

        OwnershipChecks.EnsureBookingVenueOwner(booking, ownerId);

        if (booking.Status != BookingStatus.Pending)
        {
            throw new ApiException(409, "NOT_PENDING", "Only a pending booking can be confirmed.");
        }

        booking.Status = BookingStatus.Confirmed;
        await db.SaveChangesAsync(ct);
        return booking.ToResponse(timeProvider.GetUtcNow().UtcDateTime);
    }

    /// <summary>Whole-hour, in the future, inside operating hours, same calendar day (data-model.md Booking rules).</summary>
    private static void ValidateSlot(Court court, DateTime start, DateTime end, DateTime now)
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
    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };

    /// <summary>SQL Server deadlock victim (error 1205), surfaced through EF as DbUpdateException or directly.</summary>
    private static bool IsDeadlock(Exception ex)
    {
        var current = ex is DbUpdateException { InnerException: not null } dbEx ? dbEx.InnerException : ex;
        return current is Microsoft.Data.SqlClient.SqlException { Number: 1205 };
    }
}
