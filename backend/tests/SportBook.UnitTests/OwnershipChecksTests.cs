using SportBook.Application.Authorization;
using SportBook.Application.Exceptions;
using SportBook.Domain.Entities;

namespace SportBook.UnitTests;

/// <summary>T044: ownership-check helpers for the Venue/Court/Booking chains (research.md Authorization checklist).</summary>
public class OwnershipChecksTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid StrangerId = Guid.NewGuid();

    [Fact]
    public void EnsureVenueOwner_allows_the_owning_user()
    {
        var venue = new Venue { Id = Guid.NewGuid(), OwnerId = OwnerId, Name = "V", City = "C", Address = "A" };

        OwnershipChecks.EnsureVenueOwner(venue, OwnerId);
    }

    [Fact]
    public void EnsureVenueOwner_rejects_a_different_user()
    {
        var venue = new Venue { Id = Guid.NewGuid(), OwnerId = OwnerId, Name = "V", City = "C", Address = "A" };

        var ex = Assert.Throws<ApiException>(() => OwnershipChecks.EnsureVenueOwner(venue, StrangerId));
        Assert.Equal(403, ex.StatusCode);
    }

    [Fact]
    public void EnsureCourtOwner_allows_the_owner_of_the_courts_venue()
    {
        var venue = new Venue { Id = Guid.NewGuid(), OwnerId = OwnerId, Name = "V", City = "C", Address = "A" };
        var court = new Court { Id = Guid.NewGuid(), VenueId = venue.Id, Venue = venue, Name = "C" };

        OwnershipChecks.EnsureCourtOwner(court, OwnerId);
    }

    [Fact]
    public void EnsureCourtOwner_rejects_a_different_user()
    {
        var venue = new Venue { Id = Guid.NewGuid(), OwnerId = OwnerId, Name = "V", City = "C", Address = "A" };
        var court = new Court { Id = Guid.NewGuid(), VenueId = venue.Id, Venue = venue, Name = "C" };

        var ex = Assert.Throws<ApiException>(() => OwnershipChecks.EnsureCourtOwner(court, StrangerId));
        Assert.Equal(403, ex.StatusCode);
    }

    [Fact]
    public void EnsureCourtOwner_throws_if_venue_is_not_loaded()
    {
        var court = new Court { Id = Guid.NewGuid(), VenueId = Guid.NewGuid(), Name = "C" };

        Assert.Throws<InvalidOperationException>(() => OwnershipChecks.EnsureCourtOwner(court, OwnerId));
    }

    [Fact]
    public void EnsureBookingCustomer_allows_the_customer_who_made_it()
    {
        var booking = new Booking { Id = Guid.NewGuid(), UserId = OwnerId, CourtId = Guid.NewGuid() };

        OwnershipChecks.EnsureBookingCustomer(booking, OwnerId);
    }

    [Fact]
    public void EnsureBookingCustomer_rejects_a_different_customer()
    {
        var booking = new Booking { Id = Guid.NewGuid(), UserId = OwnerId, CourtId = Guid.NewGuid() };

        var ex = Assert.Throws<ApiException>(() => OwnershipChecks.EnsureBookingCustomer(booking, StrangerId));
        Assert.Equal(403, ex.StatusCode);
    }

    [Fact]
    public void EnsureBookingVenueOwner_allows_the_owner_of_the_bookings_venue()
    {
        var venue = new Venue { Id = Guid.NewGuid(), OwnerId = OwnerId, Name = "V", City = "C", Address = "A" };
        var court = new Court { Id = Guid.NewGuid(), VenueId = venue.Id, Venue = venue, Name = "C" };
        var booking = new Booking { Id = Guid.NewGuid(), CourtId = court.Id, Court = court, UserId = StrangerId };

        OwnershipChecks.EnsureBookingVenueOwner(booking, OwnerId);
    }

    [Fact]
    public void EnsureBookingVenueOwner_rejects_a_different_owner()
    {
        var venue = new Venue { Id = Guid.NewGuid(), OwnerId = OwnerId, Name = "V", City = "C", Address = "A" };
        var court = new Court { Id = Guid.NewGuid(), VenueId = venue.Id, Venue = venue, Name = "C" };
        var booking = new Booking { Id = Guid.NewGuid(), CourtId = court.Id, Court = court, UserId = StrangerId };

        var ex = Assert.Throws<ApiException>(() => OwnershipChecks.EnsureBookingVenueOwner(booking, StrangerId));
        Assert.Equal(403, ex.StatusCode);
    }

    [Fact]
    public void EnsureBookingVenueOwner_throws_if_court_or_venue_is_not_loaded()
    {
        var booking = new Booking { Id = Guid.NewGuid(), CourtId = Guid.NewGuid(), UserId = StrangerId };

        Assert.Throws<InvalidOperationException>(() => OwnershipChecks.EnsureBookingVenueOwner(booking, OwnerId));
    }
}
