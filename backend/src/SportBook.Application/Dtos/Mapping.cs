using SportBook.Domain.Entities;
using SportBook.Domain.Enums;

namespace SportBook.Application.Dtos;

/// <summary>
/// Hand-written entity-to-DTO mapping (research.md decision: no mapping library) so the response
/// whitelist stays visible in code review - `PasswordHash` can never leak through convention magic.
/// </summary>
public static class Mapping
{
    public static UserResponse ToResponse(this User user) =>
        new(user.Id, user.Name, user.Email, user.Role.ToString(), user.SubscriptionTier.ToString(), user.CreatedAt);

    public static CityResponse ToResponse(this City city) =>
        new(city.Id, city.NameEn, city.NameUk, city.NamePt, city.RegionEn, city.RegionUk, city.RegionPt,
            city.Latitude, city.Longitude);

    /// <summary><see cref="Venue.City"/> must already be loaded (Include) before calling this.</summary>
    public static VenueSummaryResponse ToSummaryResponse(this Venue venue) =>
        new(venue.Id, venue.Name, venue.City!.ToResponse(), venue.Address, venue.Description, venue.Latitude, venue.Longitude);

    /// <summary>
    /// <see cref="Venue.City"/> must already be loaded (Include) and `Latitude`/`Longitude` must
    /// be non-null before calling this - callers (VenueService.SearchNearbyAsync) only pass
    /// coordinate-bearing venues (003 data-model.md).
    /// </summary>
    public static NearbyVenueResponse ToNearbyResponse(this Venue venue, decimal distanceKm) =>
        new(venue.Id, venue.Name, venue.City!.ToResponse(), venue.Address, venue.Description,
            venue.Latitude!.Value, venue.Longitude!.Value, distanceKm);

    public static CourtResponse ToResponse(this Court court) =>
        new(court.Id, court.VenueId, court.Name, court.SportType.ToString(), court.PricePerHour,
            court.OpeningTime, court.ClosingTime, court.IsActive);

    /// <summary>
    /// `Completed` is derived on read (data-model.md state transitions): a Confirmed booking whose
    /// EndTime has passed is displayed as Completed without a stored transition or background job.
    /// Callers MUST load the `Court -> Venue -> City` chain (Include) before mapping - the venue/
    /// city/sport/court labels (005) are read from it; every booking-response path in
    /// <see cref="Services.BookingService"/> loads that chain.
    /// </summary>
    public static BookingResponse ToResponse(this Booking booking, DateTime utcNow)
    {
        var status = booking.Status == BookingStatus.Confirmed && booking.EndTime <= utcNow
            ? BookingStatus.Completed
            : booking.Status;

        var court = booking.Court!;
        var venue = court.Venue!;
        return new BookingResponse(booking.Id, booking.CourtId, booking.UserId, booking.StartTime,
            booking.EndTime, status.ToString(), booking.TotalPrice, booking.CreatedAt,
            venue.Name, venue.City!.ToResponse(), court.SportType.ToString(), court.Name, venue.Id);
    }
}
