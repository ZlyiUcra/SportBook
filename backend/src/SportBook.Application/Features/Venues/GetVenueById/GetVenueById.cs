using Mediator;
using SportBook.Application.Dtos;
using SportBook.Application.Services;

namespace SportBook.Application.Features.Venues.GetVenueById;

/// <summary>A single venue with its courts and aggregate review rating.</summary>
public sealed record GetVenueByIdQuery(Guid Id) : IRequest<VenueDetailResponse>;

public sealed class GetVenueByIdHandler(VenueDetailReader reader) : IRequestHandler<GetVenueByIdQuery, VenueDetailResponse>
{
    public ValueTask<VenueDetailResponse> Handle(GetVenueByIdQuery request, CancellationToken ct) =>
        new(reader.GetByIdAsync(request.Id, ct));
}
