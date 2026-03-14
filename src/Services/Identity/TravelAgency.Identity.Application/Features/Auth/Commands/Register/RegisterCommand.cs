using MediatR;
using TravelAgency.Identity.Application.DTOs;

namespace TravelAgency.Identity.Application.Features.Auth.Commands.Register;

public sealed record RegisterCommand(RegisterRequest Request) : IRequest<AuthTokensDto>;
