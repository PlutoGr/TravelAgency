using MediatR;
using TravelAgency.Identity.Application.DTOs;

namespace TravelAgency.Identity.Application.Features.Profile.Commands.UpdateManagerCard;

public sealed record UpdateManagerCardCommand(UpdateManagerCardRequest Request) : IRequest<ManagerPublicCardDto>;
