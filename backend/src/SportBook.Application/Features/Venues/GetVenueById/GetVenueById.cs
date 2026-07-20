using MediatR;
using SportBook.Application.Dtos;
using SportBook.Application.Services;

namespace SportBook.Application.Features.Venues.GetVenueById;

/// <summary>A single venue with its courts and aggregate review rating.</summary>
public sealed record GetVenueByIdQuery(Guid Id) : IRequest<VenueDetailResponse>;

public sealed class GetVenueByIdHandler(VenueDetailReader reader) : IRequestHandler<GetVenueByIdQuery, VenueDetailResponse>
{
    public Task<VenueDetailResponse> Handle(GetVenueByIdQuery request, CancellationToken ct) =>
        reader.GetByIdAsync(request.Id, ct);
}
