using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Enums;

namespace TravelAgency.Identity.Application.Features.Profile;

public static class ProfileMapper
{
    public const string AvatarPublicPathPrefix = "/api/v1/media/files/";
    public const string AvatarSizeCode = "a256";

    public static UserProfileDto ToDto(User user) =>
        new(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Phone,
            user.Role.ToRoleString(),
            user.CreatedAt,
            BuildAvatarUrl(user.AvatarMediaFileId));

    public static ManagerPublicCardDto ToPublicCard(User user) =>
        new(
            user.Id,
            user.DisplayName,
            BuildAvatarUrl(user.AvatarMediaFileId),
            user.Position,
            user.TourismSince,
            user.About,
            ManagerCardText.ReadTags(user.TagsJson));

    public static string? BuildAvatarUrl(Guid? mediaFileId) =>
        mediaFileId is null
            ? null
            : $"{AvatarPublicPathPrefix}{mediaFileId.Value:D}/{AvatarSizeCode}";
}
