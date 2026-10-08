using FluentValidation;
using FluentValidation.Results;
using MediatR;
using TravelAgency.Catalog.Application.DTOs;
using TravelAgency.Catalog.Application.Interfaces;
using TravelAgency.Catalog.Application.Mappings;
using TravelAgency.Catalog.Domain.Enums;

namespace TravelAgency.Catalog.Application.Features.Tours.Queries.GetTourCards;

public sealed class GetTourCardsQueryHandler(IPublicTourCardQuery cards)
    : IRequestHandler<GetTourCardsQuery, IReadOnlyList<PublicTourCardDto>>
{
    public async Task<IReadOnlyList<PublicTourCardDto>> Handle(GetTourCardsQuery request, CancellationToken cancellationToken)
    {
        if (!PublicTourCardIds.TryParse(request.Ids, out var ids, out var error))
            throw new ValidationException([new ValidationFailure(nameof(GetTourCardsQuery.Ids), error ?? "ids")]);

        if (ids.Count == 0)
            return [];

        var found = await cards.GetPublishedOrUnpublishedAsync(ids, cancellationToken);
        var byId = found.ToDictionary(tour => tour.Id);
        var now = DateTime.UtcNow;
        var result = new List<PublicTourCardDto>(ids.Count);
        foreach (var id in ids)
        {
            if (!byId.TryGetValue(id, out var tour))
                continue;

            if (tour.Status is not (TourStatus.Published or TourStatus.Unpublished))
                continue;

            result.Add(PublicCatalogMapper.ToCard(tour, now));
        }

        return result;
    }
}
