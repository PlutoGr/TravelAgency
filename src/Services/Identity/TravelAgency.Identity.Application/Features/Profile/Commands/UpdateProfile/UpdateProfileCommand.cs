using MediatR;
using TravelAgency.Identity.Application.DTOs;

namespace TravelAgency.Identity.Application.Features.Profile.Commands.UpdateProfile;

public sealed record UpdateProfileCommand(UpdateProfileRequest Request) : IRequest<UserProfileDto>;
