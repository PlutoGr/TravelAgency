using MediatR;
using TravelAgency.Catalog.Application.DTOs;

namespace TravelAgency.Catalog.Application.Features.Tours.Queries.GetTourCards;

public record GetTourCardsQuery(IReadOnlyList<string>? Ids) : IRequest<IReadOnlyList<PublicTourCardDto>>;
