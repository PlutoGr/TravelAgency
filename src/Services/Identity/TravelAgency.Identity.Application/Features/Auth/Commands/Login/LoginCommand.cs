using MediatR;
using TravelAgency.Identity.Application.DTOs;

namespace TravelAgency.Identity.Application.Features.Auth.Commands.Login;

public sealed record LoginCommand(LoginRequest Request) : IRequest<AuthTokensDto>;
