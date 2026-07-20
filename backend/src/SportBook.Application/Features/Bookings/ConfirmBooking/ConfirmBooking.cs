using MediatR;
using Microsoft.EntityFrameworkCore;
using SportBook.Application.Authorization;
using SportBook.Application.Dtos;
using SportBook.Application.Exceptions;
using SportBook.Application.Services;
using SportBook.Domain.Enums;
using SportBook.Infrastructure;

namespace SportBook.Application.Features.Bookings.ConfirmBooking;

/// <summary>Confirms a pending booking; only the owner of the booked court's venue may call this (FR-011). Pending -> Confirmed only (data-model.md state transitions); owner via Court.Venue.OwnerId.</summary>
public sealed record ConfirmBookingCommand(Guid OwnerId, Guid BookingId) : IRequest<BookingResponse>;

public sealed class ConfirmBookingHandler(SportBookDbContext db, TimeProvider timeProvider) : IRequestHandler<ConfirmBookingCommand, BookingResponse>
{
    public async Task<BookingResponse> Handle(ConfirmBookingCommand request, CancellationToken ct)
    {
        var booking = await BookingHelpers.IncludeDetail(db.Bookings).SingleOrDefaultAsync(b => b.Id == request.BookingId, ct)
            ?? throw new ApiException(404, "BOOKING_NOT_FOUND", "Booking not found.");

        OwnershipChecks.EnsureBookingVenueOwner(booking, request.OwnerId);

        if (booking.Status != BookingStatus.Pending)
        {
            throw new ApiException(409, "NOT_PENDING", "Only a pending booking can be confirmed.");
        }

        booking.Status = BookingStatus.Confirmed;
        await db.SaveChangesAsync(ct);
        return booking.ToResponse(timeProvider.GetUtcNow().UtcDateTime);
    }
}
