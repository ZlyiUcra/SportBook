using Microsoft.EntityFrameworkCore;
using SportBook.Application.Authorization;
using SportBook.Application.Common;
using SportBook.Application.Dtos;
using SportBook.Application.Exceptions;
using SportBook.Domain.Entities;
using SportBook.Domain.Enums;
using SportBook.Infrastructure;

namespace SportBook.Application.Services;

/// <summary>Court reads for US1 (list by venue) and owner-only writes for US2.</summary>
public class CourtService(SportBookDbContext db, TimeProvider timeProvider)
{
    public async Task<PagedResponse<CourtResponse>> ListByVenueAsync(Guid venueId, PageRequest page, CancellationToken ct)
    {
        if (!await db.Venues.AnyAsync(v => v.Id == venueId, ct))
        {
            throw new ApiException(404, "VENUE_NOT_FOUND", "Venue not found.");
        }

        var query = db.Courts.AsNoTracking().Where(c => c.VenueId == venueId);
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderBy(c => c.Name)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(c => new CourtResponse(c.Id, c.VenueId, c.Name, c.SportType.ToString(),
                c.PricePerHour, c.OpeningTime, c.ClosingTime, c.IsActive))
            .ToListAsync(ct);

        return new PagedResponse<CourtResponse>(items, page.Page, page.PageSize, totalCount);
    }

    public async Task<CourtResponse> CreateAsync(Guid ownerId, Guid venueId, CreateCourtRequest request, CancellationToken ct)
    {
        var venue = await db.Venues.SingleOrDefaultAsync(v => v.Id == venueId, ct)
            ?? throw new ApiException(404, "VENUE_NOT_FOUND", "Venue not found.");
        OwnershipChecks.EnsureVenueOwner(venue, ownerId);

        var court = new Court
        {
            Id = Guid.NewGuid(),
            VenueId = venueId,
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

    public async Task<CourtResponse> UpdateAsync(Guid ownerId, Guid courtId, UpdateCourtRequest request, CancellationToken ct)
    {
        var court = await LoadWithVenueAsync(courtId, ct);
        OwnershipChecks.EnsureCourtOwner(court, ownerId);

        court.Name = request.Name;
        court.SportType = request.SportType;
        court.PricePerHour = request.PricePerHour;
        court.OpeningTime = request.OpeningTime;
        court.ClosingTime = request.ClosingTime;
        court.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
        return court.ToResponse();
    }

    /// <summary>Blocked while the court has an upcoming, non-cancelled booking (spec FR-009).</summary>
    public async Task DeleteAsync(Guid ownerId, Guid courtId, CancellationToken ct)
    {
        var court = await LoadWithVenueAsync(courtId, ct);
        OwnershipChecks.EnsureCourtOwner(court, ownerId);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var hasUpcomingBookings = await db.Bookings.AnyAsync(b =>
            b.CourtId == courtId && b.Status != BookingStatus.Cancelled && b.StartTime > now, ct);
        if (hasUpcomingBookings)
        {
            throw new ApiException(409, "HAS_UPCOMING_BOOKINGS", "Cannot delete a court with upcoming, non-cancelled bookings.");
        }

        db.Courts.Remove(court);
        await db.SaveChangesAsync(ct);
    }

    private async Task<Court> LoadWithVenueAsync(Guid courtId, CancellationToken ct) =>
        await db.Courts.Include(c => c.Venue).SingleOrDefaultAsync(c => c.Id == courtId, ct)
            ?? throw new ApiException(404, "COURT_NOT_FOUND", "Court not found.");
}
