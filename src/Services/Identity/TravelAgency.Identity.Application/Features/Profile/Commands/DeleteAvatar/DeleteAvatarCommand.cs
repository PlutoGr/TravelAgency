using MediatR;
using TravelAgency.Identity.Application.DTOs;

namespace TravelAgency.Identity.Application.Features.Profile.Commands.DeleteAvatar;

public sealed record DeleteAvatarCommand(string? BearerToken) : IRequest<UserProfileDto>;
