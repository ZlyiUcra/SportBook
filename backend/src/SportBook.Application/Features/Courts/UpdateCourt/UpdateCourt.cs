using Mediator;
using Microsoft.EntityFrameworkCore;
using SportBook.Application.Authorization;
using SportBook.Application.Dtos;
using SportBook.Application.Exceptions;
using SportBook.Domain.Enums;
using SportBook.Infrastructure;

namespace SportBook.Application.Features.Courts.UpdateCourt;

/// <summary>Updates a court; only the owner of its venue may call this.</summary>
public sealed record UpdateCourtCommand(
    Guid OwnerId, Guid CourtId, string Name, SportType SportType, decimal PricePerHour,
    TimeOnly OpeningTime, TimeOnly ClosingTime, bool IsActive) : IRequest<CourtResponse>;

public sealed class UpdateCourtHandler(SportBookDbContext db) : IRequestHandler<UpdateCourtCommand, CourtResponse>
{
    public async ValueTask<CourtResponse> Handle(UpdateCourtCommand request, CancellationToken ct)
    {
        var court = await db.Courts.Include(c => c.Venue).SingleOrDefaultAsync(c => c.Id == request.CourtId, ct)
            ?? throw new ApiException(404, "COURT_NOT_FOUND", "Court not found.");
        OwnershipChecks.EnsureCourtOwner(court, request.OwnerId);

        court.Name = request.Name;
        court.SportType = request.SportType;
        court.PricePerHour = request.PricePerHour;
        court.OpeningTime = request.OpeningTime;
        court.ClosingTime = request.ClosingTime;
        court.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
        return court.ToResponse();
    }
}
