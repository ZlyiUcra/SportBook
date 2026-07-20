using MediatR;
using Microsoft.EntityFrameworkCore;
using SportBook.Application.Authorization;
using SportBook.Application.Dtos;
using SportBook.Application.Exceptions;
using SportBook.Domain.Entities;
using SportBook.Domain.Enums;
using SportBook.Infrastructure;

namespace SportBook.Application.Features.Courts.CreateCourt;

public record CreateCourtRequest(string Name, SportType SportType, decimal PricePerHour, TimeOnly OpeningTime, TimeOnly ClosingTime);

/// <summary>Creates a court under a venue; only the venue's owner may call this.</summary>
public sealed record CreateCourtCommand(
    Guid OwnerId, Guid VenueId, string Name, SportType SportType, decimal PricePerHour, TimeOnly OpeningTime, TimeOnly ClosingTime)
    : IRequest<CourtResponse>;

public sealed class CreateCourtHandler(SportBookDbContext db, TimeProvider timeProvider) : IRequestHandler<CreateCourtCommand, CourtResponse>
{
    public async Task<CourtResponse> Handle(CreateCourtCommand request, CancellationToken ct)
    {
        var venue = await db.Venues.SingleOrDefaultAsync(v => v.Id == request.VenueId, ct)
            ?? throw new ApiException(404, "VENUE_NOT_FOUND", "Venue not found.");
        OwnershipChecks.EnsureVenueOwner(venue, request.OwnerId);

        var court = new Court
        {
            Id = Guid.NewGuid(),
            VenueId = request.VenueId,
            Name = request.Name,
            SportType = request.SportType,
            PricePerHour = request.PricePerHour,
            OpeningTime = request.OpeningTime,
            ClosingTime = request.ClosingTime,
            IsActive = true,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };

        db.Courts.Add(court);
        await db.SaveChangesAsync(ct);
        return court.ToResponse();
    }
}
