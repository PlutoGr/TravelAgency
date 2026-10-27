using MediatR;
using TravelAgency.Identity.Application.Abstractions;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Exceptions;
using TravelAgency.Identity.Application.Features.Profile;
using TravelAgency.Identity.Domain.Enums;
using TravelAgency.Identity.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Abstractions;

namespace TravelAgency.Identity.Application.Features.Profile.Commands.UpdateProfile;

public sealed class UpdateProfileCommandHandler(
    ICurrentUserService currentUserService,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateProfileCommand, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated)
            throw new UnauthorizedException("User is not authenticated.");

        var userId = currentUserService.UserId;

        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var req = command.Request;
        var isStaff = user.Role is UserRole.Manager or UserRole.Admin;

        if (!isStaff && HasManagerOnlyFields(req))
        {
            var forbidden = ListManagerOnlyFields(req).ToArray();
            throw new AppValidationException(
                forbidden.ToDictionary(
                    field => field,
                    field => new[] { $"Поле «{field}» доступно только менеджеру или администратору." }));
        }

        var phone = req.Phone is null
            ? user.Phone
            : NormalizeOptionalEmpty(req.Phone);
        var city = req.City is null
            ? user.City
            : NormalizeOptionalEmpty(req.City);

        user.UpdateProfile(
            req.FirstName is null ? user.FirstName : req.FirstName.Trim(),
            req.LastName is null ? user.LastName : req.LastName.Trim(),
            phone,
            city);

        if (isStaff && HasManagerOnlyFields(req))
        {
            user.UpdateManagerProfile(
                req.About ?? user.About,
                req.TourismSince ?? user.TourismSince,
                req.Languages ?? user.GetLanguages(),
                req.Specialization ?? user.Specialization);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ProfileMapper.ToDto(user);
    }

    private static string? NormalizeOptionalEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool HasManagerOnlyFields(UpdateProfileRequest req) =>
        req.About is not null
        || req.TourismSince is not null
        || req.Languages is not null
        || req.Specialization is not null;

    private static IEnumerable<string> ListManagerOnlyFields(UpdateProfileRequest req)
    {
        if (req.About is not null) yield return "about";
        if (req.TourismSince is not null) yield return "tourismSince";
        if (req.Languages is not null) yield return "languages";
        if (req.Specialization is not null) yield return "specialization";
    }
}
