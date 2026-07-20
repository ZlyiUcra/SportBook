using MediatR;
using SportBook.Application.Dtos;
using SportBook.Application.Services;
using SportBook.Domain.Entities;
using SportBook.Infrastructure;

namespace SportBook.Application.Features.Venues.CreateVenue;

/// <summary>
/// No `ownerId` field - the owner is always the authenticated caller (research.md Authorization
/// checklist). `Latitude`/`Longitude` are both-or-neither (contracts/api.md Venues section) -
/// enforced in VenueLocationValidator, not by the record shape, since "both or neither" is not expressible
/// as a type constraint here without over-complicating the DTO.
/// </summary>
public record CreateVenueRequest(string Name, int CityId, string Address, string? Description, decimal? Latitude = null, decimal? Longitude = null);

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
