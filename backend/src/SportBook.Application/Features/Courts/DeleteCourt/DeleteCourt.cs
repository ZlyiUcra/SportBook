using Mediator;
using Microsoft.EntityFrameworkCore;
using SportBook.Application.Authorization;
using SportBook.Application.Exceptions;
using SportBook.Domain.Enums;
using SportBook.Infrastructure;

namespace SportBook.Application.Features.Courts.DeleteCourt;

/// <summary>Deletes a court; only the owner of its venue may call this, and only while it has no upcoming, non-cancelled booking (FR-009).</summary>
public sealed record DeleteCourtCommand(Guid OwnerId, Guid CourtId) : IRequest;

public sealed class DeleteCourtHandler(SportBookDbContext db, TimeProvider timeProvider) : IRequestHandler<DeleteCourtCommand>
{
    public async ValueTask<Unit> Handle(DeleteCourtCommand request, CancellationToken ct)
    {
        var court = await db.Courts.Include(c => c.Venue).SingleOrDefaultAsync(c => c.Id == request.CourtId, ct)
            ?? throw new ApiException(404, "COURT_NOT_FOUND", "Court not found.");
        OwnershipChecks.EnsureCourtOwner(court, request.OwnerId);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var hasUpcomingBookings = await db.Bookings.AnyAsync(b =>
            b.CourtId == request.CourtId && b.Status != BookingStatus.Cancelled && b.StartTime > now, ct);
        if (hasUpcomingBookings)
        {
            throw new ApiException(409, "HAS_UPCOMING_BOOKINGS", "Cannot delete a court with upcoming, non-cancelled bookings.");
        }

        db.Courts.Remove(court);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
