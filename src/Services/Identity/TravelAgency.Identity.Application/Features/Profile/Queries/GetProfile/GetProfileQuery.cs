using MediatR;
using TravelAgency.Identity.Application.DTOs;

namespace TravelAgency.Identity.Application.Features.Profile.Queries.GetProfile;

public sealed record GetProfileQuery : IRequest<UserProfileDto>;
