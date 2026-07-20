using System.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using SportBook.Application.Dtos;
using SportBook.Application.Exceptions;
using SportBook.Application.Services;
using SportBook.Domain.Entities;
using SportBook.Domain.Enums;
using SportBook.Infrastructure;

namespace SportBook.Application.Features.Bookings.CreateBooking;

/// <summary>
/// Books a court for a whole-hour slot; price and overlap safety are computed server-side.
/// No `userId` or `totalPrice` fields by design - both are derived server-side from the JWT and
/// `Court.PricePerHour` (contracts/api.md, consilium security finding on mass assignment).
/// </summary>
public sealed record CreateBookingCommand(Guid UserId, Guid CourtId, DateTime StartTime, DateTime EndTime) : IRequest<BookingResponse>;

/// <summary>
/// Overlap safety (spec FR-004) uses a serializable transaction with deadlock retry around
/// check-then-insert - the consilium-agreed default for SQL Server, which has no exclusion
/// constraints. This handler's retry loop is the one spot in the Bookings slice conversion
/// flagged for dedicated review (consilium 2026-07-20 MediatR adoption, nitpicker concern #4):
/// ct propagation, transaction lifetime, and retry-count semantics are copied verbatim from the
/// pre-conversion BookingService, not re-derived.
/// </summary>
public sealed class CreateBookingHandler(SportBookDbContext db, TimeProvider timeProvider) : IRequestHandler<CreateBookingCommand, BookingResponse>
{
    private const int MaxCreateAttempts = 3;

    public async ValueTask<BookingResponse> Handle(CreateBookingCommand request, CancellationToken ct)
    {
        var start = BookingHelpers.AsUtc(request.StartTime);
        var end = BookingHelpers.AsUtc(request.EndTime);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var court = await db.Courts.AsNoTracking()
            .Include(c => c.Venue!).ThenInclude(v => v.City)
            .SingleOrDefaultAsync(c => c.Id == request.CourtId && c.IsActive, ct)
            ?? throw new ApiException(404, "COURT_NOT_FOUND", "Court not found.");

        BookingHelpers.ValidateSlot(court, start, end, now);

        var hours = (int)(end - start).TotalHours;
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            CourtId = court.Id,
            UserId = request.UserId,
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
            catch (Exception ex) when (BookingHelpers.IsDeadlock(ex) && attempt < MaxCreateAttempts)
            {
                // Serializable range locks make concurrent check-then-insert deadlock-prone by
                // design; the losing transaction retries and then observes the winner's row.
                db.Entry(booking).State = EntityState.Detached;
            }
        }
    }
}
