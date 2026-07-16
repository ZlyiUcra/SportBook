using SportBook.Application.Exceptions;
using SportBook.Domain.Entities;

namespace SportBook.Application.Authorization;

/// <summary>
/// The written ownership rules from research.md's Authorization checklist, in one place so every
/// service applies the same FK-chain checks (venue.OwnerId, court.Venue.OwnerId, booking.UserId)
/// instead of re-deriving them per endpoint. All identity comes from JWT claims - never from
/// request bodies.
/// </summary>
public static class OwnershipChecks
{
    /// <summary>Venue update/delete and owner-scoped venue reads: only the owning user.</summary>
    public static void EnsureVenueOwner(Venue venue, Guid currentUserId)
    {
        if (venue.OwnerId != currentUserId)
        {
            throw new ApiException(403, "NOT_VENUE_OWNER", "You do not own this venue.");
        }
    }

    /// <summary>Court create/update/delete: only the owner of the court's venue. Venue must be loaded.</summary>
    public static void EnsureCourtOwner(Court court, Guid currentUserId)
    {
        if (court.Venue is null)
        {
            throw new InvalidOperationException("Court.Venue must be loaded before an ownership check.");
        }

        if (court.Venue.OwnerId != currentUserId)
        {
            throw new ApiException(403, "NOT_VENUE_OWNER", "You do not own this court's venue.");
        }
    }

    /// <summary>Booking get/cancel: only the customer who made it (spec FR-006).</summary>
    public static void EnsureBookingCustomer(Booking booking, Guid currentUserId)
    {
        if (booking.UserId != currentUserId)
        {
            // 404 semantics are the controller's choice; the service treats it as forbidden.
            throw new ApiException(403, "NOT_BOOKING_OWNER", "This booking belongs to another customer.");
        }
    }

    /// <summary>Booking confirm: only the owner of the venue the booked court belongs to (spec FR-011). Court.Venue must be loaded.</summary>
    public static void EnsureBookingVenueOwner(Booking booking, Guid currentUserId)
    {
        if (booking.Court?.Venue is null)
        {
            throw new InvalidOperationException("Booking.Court.Venue must be loaded before an ownership check.");
        }

        if (booking.Court.Venue.OwnerId != currentUserId)
        {
            throw new ApiException(403, "NOT_VENUE_OWNER", "This booking is not for one of your venues.");
        }
    }
}
