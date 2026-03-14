using MediatR;

namespace TravelAgency.Identity.Application.Features.Auth.Commands.Logout;

public sealed record LogoutCommand(string RefreshToken, Guid UserId) : IRequest;
