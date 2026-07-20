using MediatR;
using Microsoft.EntityFrameworkCore;
using SportBook.Application.Authorization;
using SportBook.Application.Dtos;
using SportBook.Application.Exceptions;
using SportBook.Application.Services;
using SportBook.Infrastructure;

namespace SportBook.Application.Features.Venues.UpdateVenue;

/// <summary>Updates a venue; only its owner may call this (403 otherwise).</summary>
public sealed record UpdateVenueCommand(
    Guid OwnerId, Guid VenueId, string Name, int CityId, string Address, string? Description, decimal? Latitude, decimal? Longitude)
    : IRequest<VenueDetailResponse>;

public sealed class UpdateVenueHandler(SportBookDbContext db, VenueLocationValidator locationValidator, VenueDetailReader detailReader)
    : IRequestHandler<UpdateVenueCommand, VenueDetailResponse>
{
    public async Task<VenueDetailResponse> Handle(UpdateVenueCommand request, CancellationToken ct)
    {
        var venue = await db.Venues.SingleOrDefaultAsync(v => v.Id == request.VenueId, ct)
            ?? throw new ApiException(404, "VENUE_NOT_FOUND", "Venue not found.");
        OwnershipChecks.EnsureVenueOwner(venue, request.OwnerId);
        await locationValidator.ValidateAsync(request.CityId, request.Latitude, request.Longitude, ct);

        venue.Name = request.Name;
        venue.CityId = request.CityId;
        venue.Address = request.Address;
        venue.Description = request.Description;
        venue.Latitude = request.Latitude;
        venue.Longitude = request.Longitude;
        await db.SaveChangesAsync(ct);

        return await detailReader.GetByIdAsync(request.VenueId, ct);
    }
}
