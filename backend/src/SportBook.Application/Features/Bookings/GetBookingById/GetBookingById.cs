using Mediator;
using Microsoft.EntityFrameworkCore;
using SportBook.Application.Authorization;
using SportBook.Application.Dtos;
using SportBook.Application.Exceptions;
using SportBook.Application.Services;
using SportBook.Infrastructure;

namespace SportBook.Application.Features.Bookings.GetBookingById;

/// <summary>A single booking; only the customer who made it may view it (403 otherwise).</summary>
public sealed record GetBookingByIdQuery(Guid UserId, Guid BookingId) : IRequest<BookingResponse>;

public sealed class GetBookingByIdHandler(SportBookDbContext db, TimeProvider timeProvider) : IRequestHandler<GetBookingByIdQuery, BookingResponse>
{
    public async ValueTask<BookingResponse> Handle(GetBookingByIdQuery request, CancellationToken ct)
    {
        var booking = await BookingHelpers.IncludeDetail(db.Bookings.AsNoTracking()).SingleOrDefaultAsync(b => b.Id == request.BookingId, ct)
            ?? throw new ApiException(404, "BOOKING_NOT_FOUND", "Booking not found.");

        OwnershipChecks.EnsureBookingCustomer(booking, request.UserId);
        return booking.ToResponse(timeProvider.GetUtcNow().UtcDateTime);
    }
}
