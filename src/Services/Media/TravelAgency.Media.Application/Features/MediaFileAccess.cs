using TravelAgency.Media.Domain;
using TravelAgency.Media.Domain.Entities;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Media.Application.Features;

/// <summary>
/// Who may read a media file. The same rule for the original download and presign.
/// Order: public tour-image, owner, Manager or Admin. Anyone else is denied.
/// </summary>
public static class MediaFileAccess
{
    public static bool CanRead(MediaFile file, Guid userId, IReadOnlyList<string>? roles)
    {
        if (file.IsPublic && MediaPurposes.IsTourImage(file.Purpose))
            return true;

        var isOwner = string.Equals(
            file.OwnerId,
            userId.ToString(),
            StringComparison.OrdinalIgnoreCase);
        if (isOwner)
            return true;

        if (HasManagerOrAdmin(roles))
            return true;

        return false;
    }

    private static bool HasManagerOrAdmin(IReadOnlyList<string>? roles)
    {
        if (roles is null)
            return false;

        foreach (var role in roles)
        {
            if (string.Equals(role, AppRoles.Manager, StringComparison.Ordinal)
                || string.Equals(role, AppRoles.Admin, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
