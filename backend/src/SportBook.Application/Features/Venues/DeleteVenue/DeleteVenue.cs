using Mediator;
using Microsoft.EntityFrameworkCore;
using SportBook.Application.Authorization;
using SportBook.Application.Exceptions;
using SportBook.Domain.Enums;
using SportBook.Infrastructure;

namespace SportBook.Application.Features.Venues.DeleteVenue;

/// <summary>
/// Deletes a venue; only its owner may call this, and only while none of its courts have an
/// upcoming, non-cancelled booking (spec FR-009). Once allowed, the delete cascades to the
/// venue's courts, their bookings, and its reviews - no separate cleanup step, since nothing
/// upcoming survives the guard below.
/// </summary>
public sealed record DeleteVenueCommand(Guid OwnerId, Guid VenueId) : IRequest;

public sealed class DeleteVenueHandler(SportBookDbContext db, TimeProvider timeProvider) : IRequestHandler<DeleteVenueCommand>
{
    public async ValueTask<Unit> Handle(DeleteVenueCommand request, CancellationToken ct)
    {
        var venue = await db.Venues.SingleOrDefaultAsync(v => v.Id == request.VenueId, ct)
            ?? throw new ApiException(404, "VENUE_NOT_FOUND", "Venue not found.");
        OwnershipChecks.EnsureVenueOwner(venue, request.OwnerId);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var hasUpcomingBookings = await db.Bookings.AnyAsync(b =>
            b.Court!.VenueId == request.VenueId && b.Status != BookingStatus.Cancelled && b.StartTime > now, ct);
        if (hasUpcomingBookings)
        {
            throw new ApiException(409, "HAS_UPCOMING_BOOKINGS", "Cannot delete a venue with upcoming, non-cancelled bookings.");
        }

        db.Venues.Remove(venue);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
