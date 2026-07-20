using Mediator;
using Microsoft.EntityFrameworkCore;
using SportBook.Application.Authorization;
using SportBook.Application.Dtos;
using SportBook.Application.Exceptions;
using SportBook.Application.Services;
using SportBook.Domain.Enums;
using SportBook.Infrastructure;

namespace SportBook.Application.Features.Bookings.CancelBooking;

/// <summary>Cancels a booking; only its customer may call this, and only more than 2 hours before its start (FR-005).</summary>
public sealed record CancelBookingCommand(Guid UserId, Guid BookingId) : IRequest<BookingResponse>;

public sealed class CancelBookingHandler(SportBookDbContext db, TimeProvider timeProvider) : IRequestHandler<CancelBookingCommand, BookingResponse>
{
    private const int CancellationCutoffHours = 2;

    public async ValueTask<BookingResponse> Handle(CancelBookingCommand request, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var booking = await BookingHelpers.IncludeDetail(db.Bookings).SingleOrDefaultAsync(b => b.Id == request.BookingId, ct)
            ?? throw new ApiException(404, "BOOKING_NOT_FOUND", "Booking not found.");

        OwnershipChecks.EnsureBookingCustomer(booking, request.UserId);

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
}
