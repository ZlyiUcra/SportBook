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

    public async Task<BookingResponse> CreateAsync(Guid userId, CreateBookingRequest request, CancellationToken ct)
    {
        var start = AsUtc(request.StartTime);
        var end = AsUtc(request.EndTime);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var court = await db.Courts.AsNoTracking()
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
        var booking = await db.Bookings.SingleOrDefaultAsync(b => b.Id == bookingId, ct)
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
        var booking = await db.Bookings.AsNoTracking().SingleOrDefaultAsync(b => b.Id == bookingId, ct)
            ?? throw new ApiException(404, "BOOKING_NOT_FOUND", "Booking not found.");

        OwnershipChecks.EnsureBookingCustomer(booking, userId);
        return booking.ToResponse(timeProvider.GetUtcNow().UtcDateTime);
    }

    public async Task<PagedResponse<BookingResponse>> ListMineAsync(Guid userId, PageRequest page, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var query = db.Bookings.AsNoTracking().Where(b => b.UserId == userId);
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(b => b.StartTime)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(ct);

        return new PagedResponse<BookingResponse>(
            items.Select(b => b.ToResponse(now)).ToList(), page.Page, page.PageSize, totalCount);
    }

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
        var items = await query
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
        var booking = await db.Bookings.Include(b => b.Court).ThenInclude(c => c!.Venue)
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
