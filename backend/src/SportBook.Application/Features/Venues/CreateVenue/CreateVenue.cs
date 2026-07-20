using MediatR;
using SportBook.Application.Dtos;
using SportBook.Application.Services;
using SportBook.Domain.Entities;
using SportBook.Infrastructure;

namespace SportBook.Application.Features.Venues.CreateVenue;

/// <summary>Creates a venue owned by the caller. Owner is always the authenticated caller - there is no `ownerId` field on the request.</summary>
public sealed record CreateVenueCommand(
    Guid OwnerId, string Name, int CityId, string Address, string? Description, decimal? Latitude, decimal? Longitude)
    : IRequest<VenueDetailResponse>;

public sealed class CreateVenueHandler(
    SportBookDbContext db, TimeProvider timeProvider, VenueLocationValidator locationValidator, VenueDetailReader detailReader)
    : IRequestHandler<CreateVenueCommand, VenueDetailResponse>
{
    public async Task<VenueDetailResponse> Handle(CreateVenueCommand request, CancellationToken ct)
    {
        await locationValidator.ValidateAsync(request.CityId, request.Latitude, request.Longitude, ct);

        var venue = new Venue
        {
            Id = Guid.NewGuid(),
            OwnerId = request.OwnerId,
            Name = request.Name,
            CityId = request.CityId,
            Address = request.Address,
            Description = request.Description,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };

        db.Venues.Add(venue);
        await db.SaveChangesAsync(ct);

        return await detailReader.GetByIdAsync(venue.Id, ct);
    }
}
