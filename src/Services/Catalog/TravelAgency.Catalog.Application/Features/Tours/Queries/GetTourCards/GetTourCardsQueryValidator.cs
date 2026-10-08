using FluentValidation;

namespace TravelAgency.Catalog.Application.Features.Tours.Queries.GetTourCards;

public class GetTourCardsQueryValidator : AbstractValidator<GetTourCardsQuery>
{
    public GetTourCardsQueryValidator()
    {
        RuleFor(query => query.Ids)
            .Must(ids => PublicTourCardIds.TryParse(ids, out _, out _))
            .WithMessage("ids must contain at most 50 guids.");
    }
}
