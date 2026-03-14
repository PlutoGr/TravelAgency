using MediatR;
using TravelAgency.Identity.Application.DTOs;

namespace TravelAgency.Identity.Application.Features.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand(RefreshTokenRequest Request) : IRequest<AuthTokensDto>;
