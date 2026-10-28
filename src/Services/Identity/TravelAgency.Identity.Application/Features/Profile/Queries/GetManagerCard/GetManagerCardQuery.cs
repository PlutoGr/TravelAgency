using MediatR;
using TravelAgency.Identity.Application.DTOs;

namespace TravelAgency.Identity.Application.Features.Profile.Queries.GetManagerCard;

public sealed record GetManagerCardQuery(Guid UserId) : IRequest<ManagerPublicCardDto>;
