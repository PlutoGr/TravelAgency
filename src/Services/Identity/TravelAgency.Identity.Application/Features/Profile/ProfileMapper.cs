using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Enums;

namespace TravelAgency.Identity.Application.Features.Profile;

public static class ProfileMapper
{
    public const string AvatarPublicPathPrefix = "/api/v1/media/files/";
    public const string AvatarSizeCode = "a256";

    public static UserProfileDto ToDto(User user)
    {
        var isStaff = user.Role is UserRole.Manager or UserRole.Admin;
        return new UserProfileDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Phone,
            user.Role.ToRoleString(),
            user.CreatedAt,
            user.City,
            BuildAvatarUrl(user.AvatarMediaFileId),
            isStaff ? user.About : null,
            isStaff ? user.TourismSince : null,
            isStaff ? user.GetLanguages() : null,
            isStaff ? user.Specialization : null);
    }

    public static ManagerPublicCardDto ToPublicCard(User user)
    {
        var about = string.IsNullOrWhiteSpace(user.About) ? null : user.About.Trim();
        var languages = user.GetLanguages();
        if (languages is null || languages.Count == 0)
            languages = null;

        return new ManagerPublicCardDto(
            user.Id,
            user.DisplayName,
            BuildAvatarUrl(user.AvatarMediaFileId),
            about,
            user.TourismSince,
            languages);
    }

    public static string? BuildAvatarUrl(Guid? mediaFileId) =>
        mediaFileId is null
            ? null
            : $"{AvatarPublicPathPrefix}{mediaFileId.Value:D}/{AvatarSizeCode}";
}
