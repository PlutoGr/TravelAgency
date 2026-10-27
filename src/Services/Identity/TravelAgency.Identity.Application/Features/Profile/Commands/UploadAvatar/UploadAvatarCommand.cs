using MediatR;
using TravelAgency.Identity.Application.DTOs;

namespace TravelAgency.Identity.Application.Features.Profile.Commands.UploadAvatar;

public sealed record UploadAvatarCommand(
    Stream Content,
    string FileName,
    string ContentType,
    long SizeBytes,
    string? BearerToken) : IRequest<UserProfileDto>;
